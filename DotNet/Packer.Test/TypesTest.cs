using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Packer.Test;

public class TypesTest : ContentTest
{
    protected override string TestedContent => Data.GeneratedTypes;

    public TypesTest () => Task.EmitTypes = true;

    [Fact]
    public void TypesContainInteropAndBootContentWithoutImport ()
    {
        Task.Execute();
        Contains(MockData.InteropTypeContent);
        Contains(MockData.BootTypeContent.Split('\n')[1]);
    }

    [Fact]
    public void TypesDontContainOtherContent ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "other.d.ts"), "other");
        Task.Execute();
        Assert.DoesNotContain("other", Data.GeneratedTypes);
    }

    [Fact]
    public void WhenTypeResolveFailsExceptionIsThrown ()
    {
        File.Delete(Path.Combine(Data.JSDir, "interop.d.ts"));
        Assert.Throws<PackerException>(() => Task.Execute());
    }

    [Fact]
    public void TypesExportAssemblyNamespace ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("export namespace foo {");
    }

    [Fact]
    public void WhenAssemblyNameContainDotsNamespaceAlsoContainDots ()
    {
        Data.AddAssemblyWithName("foo.bar.nya.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("export namespace foo.bar.nya {");
    }

    [Fact]
    public void FunctionDeclarationIsExportedForInvokableMethod ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Foo () { }");
        Task.Execute();
        Contains("export namespace foo {\n    export function Foo(): void;\n}");
    }

    [Fact]
    public void AssignableVariableIsExportedForFunctionCallback ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSFunction] public static void OnFoo () { }");
        Task.Execute();
        Contains("export namespace foo.nya {\n    export let OnFoo: () => void;\n}");
    }

    [Fact]
    public void MembersFromSameAssemblyWrappedUnderSameNamespace ()
    {
        Data.AddAssemblyWithName("foo.dll",
            "public class Foo { }" +
            "[JSInvokable] public static Foo GetFoo () => default;"
        );
        Task.Execute();
        Contains("export namespace foo {\n    export class Foo {\n    \n}\n    export function GetFoo(): Foo;\n}");
    }

    [Fact]
    public void MembersFromDifferentAssembliesWrappedUnderRespectiveNamespaces ()
    {
        Data.AddAssemblyWithName("foo.dll", "public class Foo { }");
        Data.AddAssemblyWithName("bar.dll", "[JSInvokable] public static Foo GetFoo () => default;");
        Task.Execute();
        Contains("export namespace foo {\n    export class Foo {\n    \n}\n}");
        Contains("export namespace bar {\n    export function GetFoo(): foo.Foo;\n}");
    }

    [Fact]
    public void MultipleAssemblyNamespacesDeclaredFromNewLine ()
    {
        Data.AddAssemblyWithName("a.dll", "[JSInvokable] public static void Foo () { }");
        Data.AddAssemblyWithName("b.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("\nexport namespace b");
    }

    [Fact]
    public void DifferentAssembliesWithSameRootAssignedToDifferentNamespaces ()
    {
        Data.AddAssemblyWithName("nya.bar.dll", "[JSFunction] public static void Fun () { }");
        Data.AddAssemblyWithName("nya.foo.dll", "[JSFunction] public static void Foo () { }");
        Task.Execute();
        Contains("export namespace nya.bar {\n    export function Fun(): void;\n}");
        Contains("export namespace nya.foo {\n    export function Foo(): void;\n}");
    }

    [Fact]
    public void NumericsTranslatedToNumber ()
    {
        var nums = new[] { "byte", "sbyte", "ushort", "uint", "ulong", "short", "int", "long", "decimal", "double", "float" };
        var csArgs = string.Join(", ", nums.Select(n => $"{n} v{Array.IndexOf(nums, n)}"));
        var tsArgs = string.Join(", ", nums.Select(n => $"v{Array.IndexOf(nums, n)}: number"));
        Data.AddAssembly($"[JSInvokable] public static void Num ({csArgs}) {{}}");
        Task.Execute();
        Contains($"Num: ({tsArgs})");
    }

    [Fact]
    public void TaskTranslatedToPromise ()
    {
        Data.AddAssembly(
            "[JSInvokable] public static Task<bool> AsyBool () => default;",
            "[JSInvokable] public static ValueTask AsyVoid () => default;"
        );
        Task.Execute();
        Contains("AsyBool(): Promise<boolean>");
        Contains("AsyVoid(): Promise<void>");
    }

    [Fact]
    public void CharAndStringTranslatedToString ()
    {
        Data.AddAssembly("[JSInvokable] public static void Cha (char c, string s) {}");
        Task.Execute();
        Contains("Cha(c: string, s: string): void");
    }

    [Fact]
    public void BoolTranslatedToBoolean ()
    {
        Data.AddAssembly("[JSInvokable] public static void Boo (bool b) {}");
        Task.Execute();
        Contains("Boo(b: boolean): void");
    }

    [Fact]
    public void DateTimeTranslatedToDate ()
    {
        Data.AddAssembly("[JSInvokable] public static void Doo (DateTime time) {}");
        Task.Execute();
        Contains("Doo(time: Date): void");
    }

    [Fact]
    public void ListAndArrayTranslatedToArray ()
    {
        Data.AddAssembly("[JSInvokable] public static List<string> Goo (DateTime[] d) => default;");
        Task.Execute();
        Contains("Goo(d: Array<Date>): Array<string>");
    }

    [Fact]
    public void DefinitionIsGeneratedForObjectType ()
    {
        Data.AddAssembly(
            "public class Foo { public string Str { get; set; } public int Int { get; set; } }" +
            "[JSInvokable] public static Foo Method (Foo t) => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*str: string;\s*int: number;\s*}");
        Contains("Method(t: Foo): Foo");
    }

    [Fact]
    public void DefinitionIsGeneratedForInterfaceAndImplementation ()
    {
        Data.AddAssembly(
            "public interface Base { Base Foo { get; } void Bar (Base b); }" +
            "public class Derived : Base { public Base Foo { get; } public void Bar (Base b) {} }" +
            "[JSInvokable] public static Derived Method (Base b) => default;"
        );
        Task.Execute();
        Matches(@"export interface Base {\s*foo: Base;\s*}");
        Matches(@"export class Derived implements Base {\s*foo: Base;\s*}");
        Contains("Method(b: Base): Derived");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithListProperty ()
    {
        Data.AddAssembly(
            "public interface Item { }" +
            "public class Container { public List<Item> Items { get; } }" +
            "[JSInvokable] public static Container Combine (List<Item> items) => default;"
        );
        Task.Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export class Container {\s*items: Array<Item>;\s*}");
        Contains("Combine(items: Array<Item>): Container");
    }

    [Fact]
    public void CanCrawlCustomTypes ()
    {
        Data.AddAssembly(
            "public enum Nyam { A, B }" +
            "public class Foo { public Nyam Nyam { get; } }" +
            "public class Bar : Foo { }" +
            "public class Barrel { public List<Bar> Bars { get; } }" +
            "[JSInvokable] public static Barrel GetBarrel () => default;"
        );
        Task.Execute();
        Matches(@"export enum Nyam {\s*A,\s*B\s*}");
        Matches(@"export class Foo {\s*nyam: Nyam;\s*}");
        Matches(@"export class Bar extends Foo {\s*}");
    }

    [Fact]
    public void OtherTypesAreTranslatedToAny ()
    {
        Data.AddAssembly("[JSInvokable] public static DBNull Method (DBNull t) => default;");
        Task.Execute();
        Contains("Method(t: any): any");
    }

    [Fact]
    public void StaticPropertiesAreNotIncluded ()
    {
        Data.AddAssembly(
            "public class Foo { public static string Soo { get; } }" +
            "[JSInvokable] public static Foo Bar () => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*}");
    }

    [Fact]
    public void ExpressionPropertiesAreNotIncluded ()
    {
        Data.AddAssembly(
            "public class Foo { public bool Boo => true; }" +
            "[JSInvokable] public static Foo Bar () => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*}");
    }

    [Fact]
    public void NullablePropertiesHaveOptionalModificator ()
    {
        Data.AddAssembly(
            "public class Foo { public bool? Bool { get; } }" +
            "public class Bar { public Foo? Foo { get; } }" +
            "[JSInvokable] public static Foo FooBar (Bar bar) => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*bool\?: boolean;\s*}");
        Matches(@"export class Bar {\s*foo\?: Foo;\s*}");
    }

    [Fact]
    public void NullableEnumsAreCrawled ()
    {
        Data.AddAssembly(
            "public enum Foo { A, B }" +
            "public class Bar { public Foo? Foo { get; } }" +
            "[JSInvokable] public static Bar GetBar () => default;"
        );
        Task.Execute();
        Matches(@"export enum Foo {\s*A,\s*B\s*}");
        Matches(@"export class Bar {\s*foo\?: Foo;\s*}");
    }
}
