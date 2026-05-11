# Requirements

- Follow the existing code style, architecture, naming and formatting strictly.
- Use the latest C# features when they fit the existing style.
- Avoid defensive programming and compatibility overhead.
- If clarification is required, use the question tool instead of guessing.

# Export-Import Model

We have "export" and "import" concepts used throughout the codebase. The model is always C#-centric and means the same thing on both the C# and JavaScript sides:

- Export: something in C# being exported to JavaScript
- Import: something in JavaScript being imported to C#

For example, an exported method means a C# method exposed to JavaScript, and we refer to it as exported in both the C# and JavaScript code. An imported method means the opposite: a JavaScript function bound to a partial C# method, referred to as imported in both C# and JS code.

Make sure to follow this convention strictly.

# Packaging Bootsharp

Follow these steps exactly and sequentially whenever the Bootsharp package consumed by other projects must be actualized, or when running the JS end-to-end tests after modifying the package's C# or JS code.

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
- No unreachable code is allowed, except in rare cases where testing is not practical.
- Treat branch coverage as part of the requirement, not just line coverage.

To check C# coverage, use `reportgenerator` on merged coverlet output. Example workflow reference: `src/cs/.scripts/cover.sh`. Do not run that script verbatim in automation; it is intended for interactive usage.

To check JS coverage, run `npm run cover` under `src/js`.

# Inspecting Generated Output

C# tests under `Bootsharp.Publish.Test` generate files inside a temporary `MockProject` root, which is deleted when the test is disposed. When you need to inspect the generated content of the last failed test, read the `src/cs/Bootsharp.Publish.Test/last-failed-test-dump.txt` file.

# Running Shell Scripts

Always run `.sh` scripts with the `bash` command, for example: `bash script.sh`.
