# Requirements

- Keep the code lean and efficient, including the use of `unsafe` when it is justified.
- Use the latest available .NET and C# features when they improve the code and fit the existing style.
- Avoid defensive programming and compatibility overhead. Target only the modern 32-bit WASM runtime, current JS specs, and current browser capabilities.
- Follow the existing code style, architecture, project structure, naming, and formatting strictly.
- Do not stop at analysis or a partial fix. If the task requires code or verification, carry it through to the expected result.
- If clarification is required, use the question tool instead of guessing.

IMPORTANT: NEVER RUN ANY BUILD/PUBLISH COMMANDS IN PARALLEL.

# Packaging Bootsharp

Follow these steps exactly and sequentially whenever the Bootsharp package consumed by other projects must be actualized, or when running the JS end-to-end tests after updating the package's C# or JS code.

1. Build the JS package with `npm run build` under `src/js`.
2. Bump the Bootsharp library alpha version in `src/cs/Directory.Build.props`
    - If the current version does not already use an `-alpha.X` suffix, add one.
    - Example: `0.8.0` -> `0.8.0-alpha.0` -> `0.8.0-alpha.1`.
3. Package the C# library with `src/cs/.scripts/pack.sh` under `src/cs`.
4. Compile the end-to-end C# test projects with `npm run compile-test` under `src/js`.
5. Run the end-to-end JS tests with `npm run test` under `src/js`.

Important: Always execute these steps in order, do not parallelize them.

Note: Bumping the package version is only required after modifying the package's C# or JS sources. If you're only editing E2E tests, there is no need to follow the full repackaging procedure each time.

# Code Coverage

We have a strict 100% coverage policy for both the C# and JS codebases.

- Tests must be meaningful and cover real behavior.
- Do not add fake tests just to satisfy the numbers.
- No unreachable code is allowed, except in rare cases where testing is not practical. In those cases, `[ExcludeFromCodeCoverage]` may be used deliberately.
- Treat branch coverage as part of the requirement, not just line coverage.

To check C# coverage, use `reportgenerator` on merged coverlet output. Example workflow reference: `src/cs/.scripts/cover.sh`. Do not run that script verbatim in automation; it is intended for interactive usage.

To check JS coverage, run `npm run cover` under `src/js`.

# Inspecting Generated Output

C# tests under `Bootsharp.Publish.Test` generate files inside a temporary `MockProject` root, which is deleted when the test is disposed. When you need to inspect the generated content, write it to a scratch file outside the mock project, for example:

```csharp
AddAssembly(With("// fixture source code"));
Execute();
File.WriteAllText(Path.Combine(Path.GetTempPath(), "scratch.txt"), GeneratedDeclarations);
Contains("// asserted generated content");
```

Then run the focused test, read the scratch file and remove the probe before finalizing. Do not commit debug dumps or temporary file writes.

# Running Shell Scripts

Always run `.sh` scripts with the `bash` command, for example: `bash script.sh`.
