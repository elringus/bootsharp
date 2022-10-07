namespace Packer;

internal class LibraryTemplate
{
    public string RuntimeJS { get; init; } = null!;
    public string InitJS { get; init; } = null!;

    public string Build () => $@"{RuntimeJS}
(function (root, factory) {{
    if (typeof exports === 'object' && typeof exports.nodeName !== 'string')
        factory(module.exports, global);
    else factory(root.dotnet, root);
}}(typeof self !== 'undefined' ? self : this, function (exports, global) {{
    {InitJS}
    global.dotnet = exports;
}}));";
}
