Sample web application built with C# backend and [React](https://react.dev) frontend bundled with [Vite](https://vitejs.dev). Features generating JavaScript bindings for a standalone C# project and injecting them via [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection), multithreading, AOT-compiling, customizing various Bootsharp build options, side-loading binaries, mocking C# APIs in frontend unit tests, using events and type declarations.

How to test:
- Run `cd ./backend && dotnet workload restore`
- Run `npm run backend` to compile C# backend;
- Run `npm install` to install NPM dependencies;
- Run `npm run test` to run frontend unit tests;
- Run `npm run cover` to gather code coverage;
- Run `npm run dev` to run local dev server with hot reload;
- Run `npm run build` to build the app for production;
- Run `npm run preview` to run local server for the built app.
