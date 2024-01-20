/// <summary>
/// Customizes Bootsharp behaviour at build time.
/// </summary>
public class Preferences : Bootsharp.Preferences
{
    // Group all generated JavaScript artifacts under 'Computer' namespace.
    public override string ResolveSpace (Type type, string @default)
    {
        var dotIdx = @default.IndexOf('.');
        return dotIdx >= 0 ? "Computer" + @default[dotIdx..] : @default;
    }
}
