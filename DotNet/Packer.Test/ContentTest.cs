using System.Text.RegularExpressions;
using Xunit;

namespace Packer.Test;

public abstract class ContentTest : BuildTest
{
    protected abstract string TestedContent { get; }

    protected void Contains (string content)
    {
        Assert.Contains(content, TestedContent);
    }

    protected MatchCollection Matches (string pattern)
    {
        Assert.Matches(pattern, TestedContent);
        return Regex.Matches(TestedContent, pattern);
    }
}
