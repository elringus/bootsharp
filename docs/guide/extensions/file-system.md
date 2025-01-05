# File System

::: danger SPONSORS
This extension is exclusive for sponsors: https://github.com/sponsors/elringus.
:::

With the new [File System Access](https://developer.mozilla.org/en-US/docs/Web/API/File_System_API) APIs it's possible to access local file system directly from web browser. Bootsharp.FileSystem extension provides C# bindings and JavaScript package to use the APIs directly from C#.

Install the NuGet package to C# project:

```xml

<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
        <PackageReference Include="Bootsharp.FileSystem" Version="*-*"/>
    </ItemGroup>

</Project>
```

And the NPM package to JavaScript project:

```json
{
    "dependencies": {
        "backend": "file:backed",
        "@rewaffle/bootsharp-file-system": "latest"
    }
}
```

Before booting C# solution in JavaScript, initialize the file system extension:

```ts
import bootsharp, { Bootsharp } from "backend";
import * as fs from "@rewaffle/bootsharp-file-system";

fs.init(Bootsharp.FileSystem.FileMounter);
await bootsharp.boot();
```

Then proceed to C# where `IFileMounter` interface will be automatically injected by the extension importing following APIs from JavaScript:

```csharp
interface IFileMounter
{
    Task<string?> PickRoot (PickOptions? options = null);
    Task<IFileSystem> Mount (string root, IFileWatcher watcher);
    Task Unmount (string root);
}
```

Invoking `PickRoot` method will prompt user to select root directory to mount. It will return unique root directory identifier to be used with `Mount` and `Unmount` methods or `null` in case user cancelled pick dialogue. Optional `PickOptions` argument allows specifying which directory pick dialogue should start in, whether write access should be requested, etc.

After user picked a directory and you get the root ID, invoke `Mount`, which will return `IFileSystem` instance, providing common IO interface over the contents of the mapped directory:

```csharp
interface IFileSystem
{
    Task CreateDirectory (string uri);
    Task RemoveDirectory (string uri);
    Task WriteFile (string uri, byte[] content);
    Task DeleteFile (string uri);
    Task<byte[]> ReadFile (string uri);
    Task<FileInfo> GetFileInfo (string uri);
}
```

File watcher instance specified when invoking `Mount` allows handling file changes under the mapped directory:

```csharp
interface IFileWatcher
{
    Task HandleFileChanges (FileChange[] changes);
}
```

— until the directory is un-mounted, the watcher will be notified when an entry (directory or file) is added, removed or modified.

::: tip EXAMPLE
Find sample application built with `Bootsharp.FileSystem` in the [sponsors repository](https://github.com/rewaffle/extra).
:::
