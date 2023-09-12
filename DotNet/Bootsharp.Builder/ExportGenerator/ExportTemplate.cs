namespace Bootsharp.Builder;

internal sealed class ExportTemplate()
{
    public string Build () =>
        $$"""
          using System.Runtime.InteropServices.JavaScript;
          using static Bootsharp.Serializer;

          namespace Bootsharp;

          public static partial class InteropExports
          {

          }
          """;
}
