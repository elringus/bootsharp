namespace Bootsharp.Common.Test;

public class PreferencesTest
{
    [Fact]
    public void ReturnDefaultsByDefault ()
    {
        var prefs = new Preferences();
        Assert.Equal("foo", prefs.ResolveSpace(default, "foo"));
    }
}
