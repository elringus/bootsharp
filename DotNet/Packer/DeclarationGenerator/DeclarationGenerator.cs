using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Packer.TextUtilities;

namespace Packer;

internal class DeclarationGenerator
{
    private readonly MethodDeclarationGenerator methodsGenerator = new();
    private readonly List<DeclarationFile> declarations = new();
    private readonly TypeDeclarationGenerator typesGenerator;
    private readonly bool embedded, worker;

    public DeclarationGenerator (NamespaceBuilder spaceBuilder, bool embedded, bool worker)
    {
        typesGenerator = new TypeDeclarationGenerator(spaceBuilder);
        this.embedded = embedded;
        this.worker = worker;
    }

    public void LoadDeclarations (string directory)
    {
        foreach (var path in Directory.GetFiles(directory, "*.d.ts"))
        {
            var fileName = Path.GetFileNameWithoutExtension(path)[..^2];
            var source = File.ReadAllText(path);
            declarations.Add(new DeclarationFile(fileName, source));
        }
    }

    public string Generate (AssemblyInspector inspector) => JoinLines(0,
        !embedded ? GenerateSideLoadDeclarations() : null,
        JoinLines(declarations.Select(GenerateForDeclaration), 0),
        typesGenerator.Generate(inspector.Types),
        methodsGenerator.Generate(inspector.Methods)
    ) + "\n";

    private string GenerateSideLoadDeclarations () => JoinLines(0,
        "export interface BootUris {", JoinLines(1, true,
            "wasm: string;",
            "assemblies: string[];",
            "entryAssembly: string;"),
        "}",
        "export declare function getBootUris(): BootUris;"
    );

    private string GenerateForDeclaration (DeclarationFile declaration)
    {
        if (!ShouldExportDeclaration(declaration)) return "";
        var source = declaration.Source;
        foreach (var line in SplitLines(source))
            if (line.StartsWith("import"))
                source = source.Replace(line, GetSourceForImportLine(line));
        return ModifyInternalDeclarations(source);
    }

    private static bool ShouldExportDeclaration (DeclarationFile declaration)
    {
        return declaration.FileName switch {
            "boot" or "interop" or "event" => true,
            _ => false
        };
    }

    private string GetSourceForImportLine (string line)
    {
        var importStart = line.LastIndexOf("\"./", StringComparison.Ordinal) + 3;
        var importLength = line.Length - importStart - 2;
        var import = line.Substring(importStart, importLength);
        foreach (var declaration in declarations)
            if (declaration.FileName == import)
                return declaration.Source;
        throw new PackerException($"Failed to find type import for '{import}'.");
    }

    private string ModifyInternalDeclarations (string source)
    {
        if (embedded) source = source.Replace("boot(bootData: BootData):", "boot():");
        if (worker)
        {
            ModifyMethodToReturnPromise("getBootStatus");
            ModifyMethodToReturnPromise("broadcast");
            ModifyMethodToReturnPromise("subscribe");
            ModifyMethodToReturnPromise("unsubscribe");
            RemoveLine("subscribeById");
            RemoveLine("unsubscribeById");
        }
        RemoveLine("function initializeInterop");
        RemoveLine("function initializeMono");
        RemoveLine("function callEntryPoint");
        return source;

        void RemoveLine (string lineFragment) =>
            source = Regex.Replace(source,
                $@"\n.*{lineFragment}.*", "");

        void ModifyMethodToReturnPromise (string methodName) =>
            source = Regex.Replace(source,
                $@"(^.* {methodName}[(|:].+[:|=>] )(.+);",
                "$1Promise<$2>;", RegexOptions.Multiline);
    }
}
