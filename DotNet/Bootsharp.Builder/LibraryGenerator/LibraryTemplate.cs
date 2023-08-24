namespace Bootsharp.Builder;

internal sealed class LibraryTemplate
{
    public required string RuntimeJS { get; init; }
    public required string InitJS { get; init; }

    public string Build () =>
        $$"""
          {{RuntimeJS}}
          (function (root, factory) {
              if (typeof exports === 'object' && typeof exports.nodeName !== 'string')
                  factory(module.exports, global);
              else factory(root.dotnet, root);
          }(typeof self !== 'undefined' ? self : this, function (exports, global) {
              {{InitJS}}
              global.dotnet = exports;
          }));
          """;
}
