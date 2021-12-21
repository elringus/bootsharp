using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Packer.Test;

[ExcludeFromCodeCoverage]
public class Diverse : MockSource
{
    [JSInvokable]
    public static void Foo () { }

    [JSInvokable]
    public static byte Num (sbyte a, ushort b, uint c, ulong d, short e, int f, long g, decimal h, double m, float n) => 0;

    [JSInvokable]
    public static Task Asy (string function) => default;

    [JSFunction]
    public static MockSource Fun () => default;

    [JSFunction]
    public static ValueTask<bool> Nya (DateTime time) => default;

    public override string[] GetExpectedInitLines (string assembly) => new[] {
        $"exports.{assembly} = {{}};",
        $"exports.{assembly}.Foo = () => exports.invoke('{assembly}', 'Foo');",
        $"exports.{assembly}.Num = (a, b, c, d, e, f, g, h, m, n) => exports.invoke('{assembly}', 'Num', a, b, c, d, e, f, g, h, m, n);",
        $"exports.{assembly}.Asy = (fn) => exports.invokeAsync('{assembly}', 'Asy', fn);",
        $"exports.{assembly}.Fun = undefined;",
        $"exports.{assembly}.Nya = undefined;"
    };

    public override string[] GetExpectedBootLines (string assembly) => new[] {
        BuildFunctionAssignmentLine(assembly, nameof(Fun)),
        BuildFunctionAssignmentLine(assembly, nameof(Nya))
    };

    public override string[] GetExpectedTypeLines (string assembly) => new[] {
        $"export declare const {assembly}: {{",
        "Foo: () => void,",
        "Num: (a: number, b: number, c: number, d: number, e: number, f: number, g: number, h: number, m: number, n: number) => number,",
        "Asy: (fn: string) => Promise<void>,",
        "Fun: () => any,",
        "Nya: (time: Date) => Promise<boolean>,",
        "};"
    };
}
