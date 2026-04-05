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

## Rule suppression

FSharpLint's built-in suppression mechanisms work through the shim with no extra configuration:

**Inline comments** -- disable rules per-line or per-section:

```fsharp
// fsharplint:disable-next-line RecordFieldNames
type Foo = { bar: int }

// fsharplint:disable MaxLinesInFunction
// ... long function ...
// fsharplint:enable MaxLinesInFunction
```

Supported directives:

| Directive | Effect |
|---|---|
| `// fsharplint:disable RuleName` | Disable for rest of file |
| `// fsharplint:enable RuleName` | Re-enable |
| `// fsharplint:disable-line RuleName` | Disable for current line |
| `// fsharplint:disable-next-line RuleName` | Disable for next line |

Omit the rule name to apply to all rules.

**`fsharplint.json`** -- disable rules globally by setting `"enabled": false` on any rule. See the [FSharpLint documentation](https://fsprojects.github.io/FSharpLint/) for the full config format.

## Dependencies

FSharpLint.Core is pulled from [michaelglass/FSharpLint](https://github.com/michaelglass/FSharpLint) (`perf/two-phase-lint-api` branch) via Paket git dependency. This branch merges [Numpsy's `fcs10` branch](https://github.com/numpsy/FSharpLint/tree/fcs10), which updated FSharpLint to FSharp.Compiler.Service 43.x -- huge thanks to [Numpsy (Richard Webb)](https://github.com/numpsy) for that work. The `perf/two-phase-lint-api` branch pins FCS to 43.10.101 for compatibility with the `fsharp-analyzers` CLI v0.36.0 and adds a two-phase lint API for analyzer integration.

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
