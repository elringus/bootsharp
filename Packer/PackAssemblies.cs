using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetJS.Packer
{
    public class PackAssemblies : Task
    {
        [Required]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public string PublishUrl { get; set; }

        public override bool Execute ()
        {
            var dlls = CollectAssemblies();
            CleanPublishDirectory();
            foreach (var dll in dlls)
                File.WriteAllBytes(dll.Name, dll.Bytes);
            return true;
        }

        private List<Assembly> CollectAssemblies ()
        {
            var dlls = new List<Assembly>();
            foreach (var path in Directory.GetFiles(PublishUrl, "*.dll"))
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
            Directory.Delete(PublishUrl, true);
            Directory.CreateDirectory(PublishUrl);
        }
    }
}
