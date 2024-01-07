namespace Bootsharp.Common.Test;

public class ExceptionTest
{
    [Fact]
    public void NotImplementedIncludesMethodName ()
    {
        Assert.Contains("$Func", Assert.Throws<NotIntercepted>(Func).Message);
    }

    private static void Func () => throw new NotIntercepted();
}
