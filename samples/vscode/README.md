An example on using [Bootsharp](https://github.com/Elringus/Bootsharp)-compiled C# solution inside VS Code standalone (node) and [web](https://code.visualstudio.com/api/extension-guides/web-extensions) extensions.

Install [Hello, Bootsharp!](https://marketplace.visualstudio.com/items?itemName=Elringus.bootsharp) extension in VS Code, open command palette (`Ctrl+Shift+P` on standalone or `F1` on web) and execute `Hello, Bootsharp!` command. If successful, a welcome notification will appear.

![](https://i.gyazo.com/a3ec0ee51f14970a7eca24169d682274.png)

To test the extension locally:

- Run `npm run build` to build the sources;
- Run `npm run package` to bundle extension package;
- Drag-drop the produced `.vsix` file to the VS Code extension tab.
