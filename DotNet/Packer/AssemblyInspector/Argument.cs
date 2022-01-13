namespace Packer;

internal record Argument
{
    public string Name { get; init; }
    public string Type { get; init; }

    public override string ToString () => $"{Name}: {Type}";
}
