using System.Text.RegularExpressions;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Bootsharp.Publish.Test;

public abstract class TaskTest : IDisposable
{
    protected MockProject Project { get; } = new();
    protected BuildEngine Engine { get; } = BuildEngine.Create();
    protected string LastAddedAssemblyName { get; private set; }
    protected virtual string TestedContent { get; set; } = "";
    protected virtual string TestedDirectory => "";

    public abstract void Execute ();

    public void Dispose ()
    {
        Project.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void AddAssembly (string assemblyName, params MockSource[] sources)
    {
        LastAddedAssemblyName = assemblyName;
        Project.AddAssembly(new(assemblyName, sources));
    }

    protected void AddAssembly (params MockSource[] sources)
    {
        AddAssembly($"MockAssembly{Guid.NewGuid():N}.dll", sources);
    }

    protected MockSource WithClass (string @namespace, string body)
    {
        return new(@namespace, body, true);
    }

    protected MockSource WithClass (string body)
    {
        return WithClass(null, body);
    }

    protected MockSource With (string @namespace, string code)
    {
        return new(@namespace, code, false);
    }

    protected MockSource With (string code)
    {
        return With(null, code);
    }

    protected void Contains (string content) => Contains(null, content);
    protected void Contains (string path, string content)
    {
        try { Assert.Contains(content, GetTestedContent(path)); }
        catch (Exception ex) { DumpAndThrow(ex, GetTestedContent(path)); }
    }

    protected void DoesNotContain (string content) => DoesNotContain(null, content);
    protected void DoesNotContain (string path, string content)
    {
        try { Assert.DoesNotContain(content, GetTestedContent(path), StringComparison.OrdinalIgnoreCase); }
        catch (Exception ex) { DumpAndThrow(ex, GetTestedContent(path)); }
    }

    protected MatchCollection Matches (string pattern) => Matches(null, pattern);
    protected MatchCollection Matches (string path, string pattern)
    {
        var tested = GetTestedContent(path);
        try { Assert.Matches(pattern, tested); }
        catch (Exception ex) { DumpAndThrow(ex, tested); }
        return Regex.Matches(tested, pattern);
    }

    protected void Once (string pattern) => Once(null, pattern);
    protected void Once (string path, string pattern)
    {
        try { Assert.Single(Matches(path, pattern)); }
        catch (Exception ex) { DumpAndThrow(ex, GetTestedContent(path)); }
    }

    private string GetTestedContent (string path)
    {
        if (path == null) return TestedContent;
        return ReadProjectFile(Path.Combine(TestedDirectory, path));
    }

    protected string ReadProjectFile (string fileName)
    {
        var filePath = Path.Combine(Project.Root, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }

    private void DumpAndThrow (Exception ex, string content)
    {
        var path = Path.Combine(Project.Root, "..", "..", "..", "..", "last-failed-test-dump.txt");
        File.WriteAllText(path, content);
        throw ex;
    }
}
