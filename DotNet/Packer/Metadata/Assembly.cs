namespace Packer
{
    public class Assembly
    {
        public string Name { get; }
        public string Base64 { get; }

        public Assembly (string name, string base64)
        {
            Name = name;
            Base64 = base64;
        }
    }
}
