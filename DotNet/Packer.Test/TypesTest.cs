using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Packer.Test;

public sealed class TypesTest : BuildTest
{
    public TypesTest () => Task.EmitTypes = true;

    [Fact]
    public void WhenTypeResolveFailsExceptionIsThrown ()
    {
        File.Delete(Path.Combine(Data.JSDir, "interop.d.ts"));
        Assert.Throws<PackerException>(() => Task.Execute());
    }

    [Fact]
    public void TypesContainInteropAndBootContentWithoutImport ()
    {
        Task.Execute();
        Assert.Contains(MockData.InteropTypeContent, Data.GeneratedTypes);
        Assert.Contains(MockData.BootTypeContent.Split('\n')[1], Data.GeneratedTypes);
    }

    [Fact]
    public void TypesDontContainOtherContent ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "other.d.ts"), "other");
        Task.Execute();
        Assert.DoesNotContain("other", Data.GeneratedTypes);
    }

    [Fact]
    public void LibraryExportsAssemblyObject ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Assert.Contains("export declare const foo: {", Data.GeneratedTypes);
    }

    [Fact]
    public void WhenAssemblyNameContainDotsObjectCreateForEachPart ()
    {
        Data.AddAssemblyWithName("foo.bar.nya.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Assert.Contains("export declare const foo: { bar: { nya: {", Data.GeneratedTypes);
    }

    [Fact]
    public void NumericsTranslatedToNumberType ()
    {
        var numerics = new[] { "byte", "sbyte", "ushort", "uint", "ulong", "short", "int", "long", "decimal", "double", "float" };
        var csArgs = string.Join(", ", numerics.Select(n => n + " v" + Array.IndexOf(numerics, n)));
        var tsArgs = string.Join(", ", numerics.Select(n => "v" + Array.IndexOf(numerics, n) + ": number"));
        Data.AddAssembly($"[JSInvokable] public static void Num ({csArgs}) {{}}");
        Task.Execute();
        Assert.Contains($"Num: ({tsArgs})", Data.GeneratedTypes);
    }

    [Fact]
    public void AsyncDotNetMethodsReturnPromiseInJS ()
    {
        Data.AddAssembly(
            "[JSInvokable] public static Task<bool> AsyBool () => default;",
            "[JSInvokable] public static ValueTask AsyVoid () => default;"
        );
        Task.Execute();
        Assert.Contains("AsyBool: () => Promise<boolean>", Data.GeneratedTypes);
        Assert.Contains("AsyVoid: () => Promise<void>", Data.GeneratedTypes);
    }

    [Fact]
    public void CharAndStringTranslatedToString ()
    {
        Data.AddAssembly("[JSInvokable] public static void Cha (char c, string s) {}");
        Task.Execute();
        Assert.Contains("Cha: (c: string, s: string) => void", Data.GeneratedTypes);
    }

    [Fact]
    public void BoolTranslatedToBoolean ()
    {
        Data.AddAssembly("[JSInvokable] public static void Boo (bool b) {}");
        Task.Execute();
        Assert.Contains("Boo: (b: boolean) => void", Data.GeneratedTypes);
    }

    [Fact]
    public void DateTimeTranslatedToDate ()
    {
        Data.AddAssembly("[JSInvokable] public static void Doo (DateTime time) {}");
        Task.Execute();
        Assert.Contains("Doo: (time: Date) => void", Data.GeneratedTypes);
    }

    [Fact]
    public void OtherTypesTranslatedToAny ()
    {
        Data.AddAssembly("[JSInvokable] public static Type Method (Type t) => default;");
        Task.Execute();
        Assert.Contains("Method: (t: any) => any", Data.GeneratedTypes);
    }
}
