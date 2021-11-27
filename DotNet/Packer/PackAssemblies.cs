using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetJS.Packer
{
    public class PackUMD : Task
    {
        [Required, SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public string BaseDir { get; set; }
        [Required, SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public string OutDir { get; set; }
        [Required, SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public string JSDir { get; set; }
        [Required, SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public string WasmFile { get; set; }
        [Required, SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public string EntryAssemblyName { get; set; }
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public bool Clean { get; set; } = true;

        public override bool Execute ()
        {
            var libraryJS = GetDotNetJS() + GenerateModuleJS();
            if (Clean) CleanPublishDirectory();
            PublishLibrary(libraryJS);
            CopyDotNetMap();
            return true;
        }

        private string GetDotNetJS ()
        {
            var path = Path.Combine(JSDir, "dotnet.js");
            return File.ReadAllText(path);
        }

        private string GenerateModuleJS ()
        {
            var wasmBase64 = GetWasmBase64();
            var assemblies = CollectAssemblies();
            var initJS = File.ReadAllText("bin/codegen/init.txt");
            var bootJS = File.ReadAllText("bin/codegen/boot.txt");
            return UMD.GenerateJS(EntryAssemblyName, wasmBase64, assemblies, initJS, bootJS);
        }

        private void CleanPublishDirectory ()
        {
            Directory.Delete(BaseDir, true);
            Directory.CreateDirectory(BaseDir);
        }

        private void PublishLibrary (string libraryJS)
        {
            var path = Path.Combine(BaseDir, "dotnet.js");
            File.WriteAllText(path, libraryJS);
            Log.LogMessage(MessageImportance.High, $"JavaScript UMD library is published at {path}.");
        }

        private List<Assembly> CollectAssemblies ()
        {
            var dlls = new List<Assembly>();
            var publishDir = Path.Combine(OutDir, "publish/wwwroot/_framework");
            foreach (var path in Directory.GetFiles(publishDir, "*.dll"))
                dlls.Add(CreateAssembly(path));
            return dlls;
        }

        private Assembly CreateAssembly (string path)
        {
            var name = Path.GetFileName(path);
            var binary = File.ReadAllBytes(path);
            var base64 = Convert.ToBase64String(binary);
            return new Assembly { Name = name, Base64 = base64 };
        }

        private string GetWasmBase64 ()
        {
            var binary = File.ReadAllBytes(WasmFile);
            return Convert.ToBase64String(binary);
        }

        private void CopyDotNetMap ()
        {
            var source = Path.Combine(JSDir, "dotnet.js.map");
            var destination = Path.Combine(BaseDir, "dotnet.js.map");
            File.Copy(source, destination, true);
        }
    }
}
