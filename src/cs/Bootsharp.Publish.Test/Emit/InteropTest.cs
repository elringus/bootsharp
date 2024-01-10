namespace Bootsharp.Publish.Test;

public class InteropTest : EmitTest
{
    protected override string TestedContent => GeneratedInterop;

    [Fact]
    public void WhenNothingInspectedGeneratesEmptyClass ()
    {
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Generated;

            public static partial class Interop
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void RegisterProxies ()
            """);
    }

    [Fact]
    public void CanGenerateForMethodsInGlobalSpace ()
    {
        AddAssembly(With(
            """
            public class Class
            {
                [JSInvokable] public static void Inv () {}
                [JSFunction] public static void Fun () {}
                [JSEvent] public static void Evt () {}
            }
            """));
        Execute();
        Contains("""Proxies.Set("Global.Class.fun", Class_Fun);""");
        Contains("""Proxies.Set("Global.Class.evt", Class_Evt);""");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSExport] internal static void Class_Inv () => global::Class.Inv();""");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSImport("Global.Class.funSerialized", "Bootsharp")] internal static partial void Class_Fun ();""");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSImport("Global.Class.evtSerialized", "Bootsharp")] internal static partial void Class_Evt ();""");
    }

    [Fact]
    public void CanGenerateForMethodsInCustomSpaces ()
    {
        AddAssembly(With(
            """
            namespace SpaceA
            {
                public class Class
                {
                    [JSInvokable] public static void Inv () {}
                    [JSFunction] public static void Fun () {}
                    [JSEvent] public static void Evt () {}
                }
            }
            namespace SpaceA.SpaceB
            {
                public class Class
                {
                    [JSInvokable] public static void Inv () {}
                    [JSFunction] public static void Fun () {}
                    [JSEvent] public static void Evt () {}
                }
            }
            """));
        Execute();
        Contains("""Proxies.Set("SpaceA.Class.fun", SpaceA_Class_Fun);""");
        Contains("""Proxies.Set("SpaceA.Class.evt", SpaceA_Class_Evt);""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.fun", SpaceA_SpaceB_Class_Fun);""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.evt", SpaceA_SpaceB_Class_Evt);""");
        Contains("""JSExport] internal static void SpaceA_Class_Inv () => global::SpaceA.Class.Inv();""");
        Contains("""JSImport("SpaceA.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Fun ();""");
        Contains("""JSImport("SpaceA.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Evt ();""");
        Contains("""JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();""");
        Contains("""JSImport("SpaceA.SpaceB.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Fun ();""");
        Contains("""JSImport("SpaceA.SpaceB.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Evt ();""");
    }
}
