using System.Text.RegularExpressions;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Bootsharp.Publish.Test;

public abstract class TaskTest : IDisposable
{
    protected MockProject Project { get; } = new();
    protected BuildEngine Engine { get; } = BuildEngine.Create();
    protected string LastAddedAssemblyName { get; private set; }
    protected virtual string TestedContent { get; } = "";

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

    protected void Contains (string content)
    {
        try { Assert.Contains(content, TestedContent); }
        catch (Exception ex) { DumpAndThrow(ex); }
    }

    protected void DoesNotContain (string content)
    {
        try { Assert.DoesNotContain(content, TestedContent, StringComparison.OrdinalIgnoreCase); }
        catch (Exception ex) { DumpAndThrow(ex); }
    }

    protected MatchCollection Matches (string pattern)
    {
        try { Assert.Matches(pattern, TestedContent); }
        catch (Exception ex) { DumpAndThrow(ex); }
        return Regex.Matches(TestedContent, pattern);
    }

    protected void Once (string pattern)
    {
        try { Assert.Single(Matches(pattern)); }
        catch (Exception ex) { DumpAndThrow(ex); }
    }

    protected string ReadProjectFile (string fileName)
    {
        var filePath = Path.Combine(Project.Root, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }

    private void DumpAndThrow (Exception ex)
    {
        var path = Path.Combine(Project.Root, "..", "..", "..", "..", "last-failed-test-dump.txt");
        File.WriteAllText(path, TestedContent);
        throw ex;
    }
}
