using Backend.Domain;
using DotNetJS;

[assembly: JSExport(new[] { typeof(IBackend) })]
[assembly: JSImport(new[] { typeof(IFrontend) })]
