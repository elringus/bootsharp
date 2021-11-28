using System.Reflection;

namespace DotNetJS.Packer
{
    public class Argument
    {
        public string Name { get; }
        public string Type { get; }

        public Argument (ParameterInfo info)
        {
            Name = GetJavaScriptName(info.Name);
            Type = TypeConversion.ToTypeScript(info.ParameterType);
        }

        private string GetJavaScriptName (string dotnetName)
        {
            if (dotnetName == "function") return "fn";
            return dotnetName;
        }
    }
}
