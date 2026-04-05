# FSharpLintAnalyzerShim

A thin adapter that exposes all 97 [FSharpLint](https://github.com/fsprojects/FSharpLint) rules as a single [FSharp.Analyzers.SDK](https://github.com/ionide/FSharp.Analyzers.SDK) `[<CliAnalyzer>]`.

This lets you run FSharpLint alongside custom project analyzers in one `fsharp-analyzers` invocation, eliminating duplicate project loading.

## How it works

The shim:

1. Discovers `fsharplint.json` by walking up from each file's directory (cached per directory)
2. Converts the Analyzer SDK's `CliContext` into FSharpLint's `ParsedFileInformation`
3. Calls `FSharpLint.Application.Lint.lintParsedFile`
4. Maps each `LintWarning` to an Analyzer SDK `Message`

All rule logic lives in FSharpLint.Core -- this project contains no rules of its own.

## Setup

### Prerequisites

- .NET 10 SDK
- `fsharp-analyzers` CLI tool: `dotnet tool install -g fsharp-analyzers`
- [Paket](https://fsprojects.github.io/Paket/) for dependency management: `dotnet tool install -g paket`

### Build

```bash
paket install
dotnet build -c Release
```

### Run

```bash
fsharp-analyzers \
  --project path/to/YourProject.fsproj \
  --analyzers-path path/to/FSharpLintAnalyzerShim/bin/Release/net10.0/
```

You can combine with other analyzer paths:

```bash
fsharp-analyzers \
  --project path/to/YourProject.fsproj \
  --analyzers-path path/to/FSharpLintAnalyzerShim/bin/Release/net10.0/ \
  --analyzers-path path/to/YourOtherAnalyzers/bin/Release/net10.0/
```

## Configuration

Place a `fsharplint.json` anywhere in the file's directory hierarchy. The shim walks up from each source file to find the nearest config, just like FSharpLint itself.

If no config file is found, FSharpLint's built-in default configuration is used.

See the [FSharpLint documentation](https://fsprojects.github.io/FSharpLint/) for config format.

## Diagnostics

All diagnostics use the standard FSharpLint rule codes (`FL0001` through `FL0097`). Severity is always `Warning`. Suggested fixes from FSharpLint are passed through as Analyzer SDK `Fix` records.

If FSharpLint encounters an internal error (which it normally swallows silently), the shim surfaces it as an `FL0000` Info diagnostic.

## Dependencies

FSharpLint.Core is pulled from [michaelglass/FSharpLint](https://github.com/michaelglass/FSharpLint) (`perf/two-phase-lint-api` branch) via Paket git dependency. This fork includes FCS 43.10.101 compatibility required by the `fsharp-analyzers` CLI v0.36.0.

## Rule coverage

All 97 FSharpLint rules work, including:

- **~75 rules** using only the untyped AST (formatting, source length, number-of-items, most smells)
- **~21 rules** using file-level type checking (naming resolution, partial functions, shadowing, etc.)
- **1 rule** using project-level type checking (NoAsyncRunSynchronouslyInLibrary)

The `CliContext` provides all three: `ParseFileResults`, `CheckFileResults`, and `CheckProjectResults`.

## Development

```bash
mise run check    # build + test
mise run test     # tests only
mise run build    # build only
```

## Tests

- **ConfigDiscoveryTests** -- config file discovery, directory walking, caching
- **WarningMappingTests** -- LintWarning-to-Message field mapping, fix mapping, severity
- **IntegrationTests** -- end-to-end: lint source with violations, verify mapped output
