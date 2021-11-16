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

        public override bool Execute ()
        {
            var dlls = CollectAssemblies();
            CleanPublishDirectory();
            foreach (var dll in dlls)
                Log.LogMessage(MessageImportance.High, dll.Name);
            foreach (var path in Directory.GetFiles(JSDir))
                Log.LogMessage(MessageImportance.High, path);
            return true;
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
            var bytes = File.ReadAllBytes(path);
            return new Assembly { Name = name, Bytes = bytes };
        }

        private void CleanPublishDirectory ()
        {
            Directory.Delete(BaseDir, true);
            Directory.CreateDirectory(BaseDir);
        }
    }
}
