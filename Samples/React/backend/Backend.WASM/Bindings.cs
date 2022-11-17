using Backend;
using DotNetJS;

[assembly: JSExport(new[] { typeof(IBackend) })]
[assembly: JSImport(new[] { typeof(IFrontend) })]
