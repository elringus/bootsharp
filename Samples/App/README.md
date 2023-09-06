Sample web application built with C# backend and [React](https://react.dev) frontend bundled with [Vite](https://vitejs.dev). Features generating JavaScript bindings for a standalone C# project and injecting them via [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection), AOT-compiling, customizing various Bootsharp build options, side-loading binaries, mocking C# APIs in frontend unit tests and using type definitions.

How to test:
- Build C# backend with `dotnet publish` under "backend" directory (or run `scripts/build-backend.sh`);
- Run `npm run test` to run frontend unit tests;
- Run `npm run dev` to run local dev server;
- Run `npm run build` to build the app.
