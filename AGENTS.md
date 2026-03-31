# Requirements

- Keep the code lean and efficient, including the use of `unsafe` when it is justified.
- Use the latest available .NET and C# features when they improve the code and fit the existing style.
- Avoid defensive programming and compatibility overhead. Target only the modern 32-bit WASM runtime, current JS specs, and current browser capabilities.
- Follow the existing code style, architecture, project structure, naming, and formatting strictly.
- Do not stop at analysis or a partial fix. If the task requires code or verification, carry it through to the expected result.
- If clarification is required, use the question tool instead of guessing.
- Before finishing a task, always run C# tests and the `Packaging Bootsharp` instructions below.

# Packaging Bootsharp

Follow these steps exactly and sequentially whenever the Bootsharp package consumed by other projects must be actualized, or when running the JS end-to-end tests against a fresh package.

1. Build the JS package with `npm run build` under `src/js`.
2. Bump the Bootsharp library alpha version in `src/cs/Directory.Build.props`
	1. If the current version does not already use an `-alpha.X` suffix, add one.
	2. Example: `0.8.0` -> `0.8.0-alpha.0` -> `0.8.0-alpha.1`.
3. Package the C# library with `src/cs/.scripts/pack.ps1` under `src/cs`.
4. Compile the end-to-end C# test projects with `npm run compile-test` under `src/js`.
5. Run the end-to-end JS tests with `npm run test` under `src/js`.

Important:

- Always execute these steps in order.
- Do not parallelize them.

# Code Coverage

We have a strict 100% coverage policy for both the C# and JS codebases.

- Tests must be meaningful and cover real behavior.
- Do not add fake tests just to satisfy the numbers.
- No unreachable code is allowed, except in rare cases where testing is not practical. In those cases, `[ExcludeFromCodeCoverage]` may be used deliberately.
- Treat branch coverage as part of the requirement, not just line coverage.

To check C# coverage, use `reportgenerator` on merged coverlet output. Example workflow reference: `src/cs/.scripts/cover.ps1`. Do not run that script verbatim in automation; it is intended for interactive usage.

To check JS coverage, run `npm run cover` under `src/js`.
