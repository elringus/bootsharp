using System.Reflection;

namespace Packer
{
    internal class Argument
    {
        public string Name { get; }
        public string Type { get; }

        public Argument (ParameterInfo info)
        {
            Name = GetJavaScriptName(info.Name);
            Type = TypeConversion.ToTypeScript(info.ParameterType);
        }

        public override string ToString () => $"{Name}: {Type}";

        private string GetJavaScriptName (string dotnetName)
        {
            if (dotnetName == "function") return "fn";
            return dotnetName;
        }
    }
}
