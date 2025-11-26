namespace Bootsharp.Publish.Test;

public class SolutionInspectionTest : PackTest
{
    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("foo.dll",
            WithClass("[JSInvokable] public static void Inv () {}")
        );
        Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("foo"));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        AddAssembly("foo.dll",
            WithClass("[JSInvokable] public static void InvFoo () {}")
        );
        AddAssembly("bar.dll",
            WithClass("[JSInvokable] public static void InvBar () {}")
        );
        File.WriteAllText(Path.Combine(Project.Root, "foo.dll"), "corrupted");
        Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }

    [Fact]
    public void IgnoresAssembliesNotPresentInBuildDirectory ()
    {
        var buildDir = $"{Project.Root}/build";
        Task.BuildDirectory = buildDir;
        Directory.CreateDirectory(buildDir);
        File.WriteAllText($"{buildDir}/foo.wasm", "");

        foreach (var file in Directory.EnumerateFiles(Project.Root))
            File.WriteAllText($"{buildDir}/{Path.GetFileName(file)}", File.ReadAllText(file));

        AddAssembly("foo.dll",
            WithClass("[JSInvokable] public static void InvFoo () {}")
        );
        AddAssembly("bar.dll",
            WithClass("[JSInvokable] public static void InvBar () {}")
        );
        Execute();

        Assert.Contains(Engine.Messages, w => w.Contains("foo"));
        Assert.DoesNotContain(Engine.Messages, w => w.Contains("bar"));
    }

    [Fact]
    public void DoesntIgnoreAssembliesWhenLLVM ()
    {
        Task.LLVM = true;

        var buildDir = $"{Project.Root}/build";
        Task.BuildDirectory = buildDir;
        Directory.CreateDirectory(buildDir);
        File.WriteAllText($"{buildDir}/foo.wasm", "");

        foreach (var file in Directory.EnumerateFiles(Project.Root))
            File.WriteAllText($"{buildDir}/{Path.GetFileName(file)}", File.ReadAllText(file));

        AddAssembly("foo.dll",
            WithClass("[JSInvokable] public static void InvFoo () {}")
        );
        AddAssembly("bar.dll",
            WithClass("[JSInvokable] public static void InvBar () {}")
        );
        Execute();

        Assert.Contains(Engine.Messages, w => w.Contains("foo"));
        Assert.Contains(Engine.Messages, w => w.Contains("bar"));
    }
}
