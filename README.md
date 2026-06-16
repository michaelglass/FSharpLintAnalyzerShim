# FSharpLintAnalyzerShim

A thin adapter that aims to expose all 97 [FSharpLint](https://github.com/fsprojects/FSharpLint)
rules as a single [FSharp.Analyzers.SDK](https://github.com/ionide/FSharp.Analyzers.SDK)
`[<CliAnalyzer>]`.

> **Status:** early alpha, and substantially AI-written. Behavior and APIs shift between
> versions, so your mileage may vary. Issues and PRs are very welcome.

## The problem

If you run both FSharpLint and your own F# analyzers, you load every project twice —
once for each tool — and pay for type-checking the whole solution twice. The goal here
is to make FSharpLint *be* an analyzer, so it can run alongside your custom analyzers in
one `fsharp-analyzers` invocation with a single project load.

All rule logic lives in FSharpLint.Core; this project contains no rules of its own.

## How it works

1. Discovers `fsharplint.json` by walking up from each file's directory (cached per directory).
2. Converts the Analyzer SDK's `CliContext` into FSharpLint's `ParsedFileInformation`.
3. Calls `FSharpLint.Application.Lint.lintParsedFile`.
4. Maps each `LintWarning` to an Analyzer SDK `Message`.

## Quick start

### Prerequisites

- .NET 10 SDK
- `fsharp-analyzers` CLI: `dotnet tool install -g fsharp-analyzers`
- [Paket](https://fsprojects.github.io/Paket/): `dotnet tool install -g paket`

> **Heads-up:** the published `fsharp-analyzers` 0.36.0 ships an FCS version this shim
> can't bind against, so you currently need an analyzer host on the FCS 43.12 line to
> run it. See [Host compatibility](docs/host-compatibility.md).

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

Pass `--analyzers-path` more than once to combine with your own analyzers:

```bash
fsharp-analyzers \
  --project path/to/YourProject.fsproj \
  --analyzers-path path/to/FSharpLintAnalyzerShim/bin/Release/net10.0/ \
  --analyzers-path path/to/YourOtherAnalyzers/bin/Release/net10.0/
```

## Configuration

Place a `fsharplint.json` anywhere in the file's directory hierarchy. The shim walks up
from each source file to find the nearest config, just like FSharpLint itself. If none
is found, FSharpLint's built-in default configuration is used.

See the [FSharpLint documentation](https://fsprojects.github.io/FSharpLint/) for the
config format.

## Rule suppression

FSharpLint's built-in suppression is meant to work through the shim with no extra
configuration.

**Inline comments** — disable rules per-line or per-section:

```fsharp
// fsharplint:disable-next-line RecordFieldNames
type Foo = { bar: int }

// fsharplint:disable MaxLinesInFunction
// ... long function ...
// fsharplint:enable MaxLinesInFunction
```

| Directive | Effect |
|---|---|
| `// fsharplint:disable RuleName` | Disable for rest of file |
| `// fsharplint:enable RuleName` | Re-enable |
| `// fsharplint:disable-line RuleName` | Disable for current line |
| `// fsharplint:disable-next-line RuleName` | Disable for next line |

Omit the rule name to apply to all rules.

**`fsharplint.json`** — disable rules globally by setting `"enabled": false` on any
rule.

## Diagnostics

FSharpLint rule diagnostics use the standard FSharpLint rule codes (`FL0001` through
`FL0097`), always at `Warning` severity. Suggested fixes are passed through as Analyzer
SDK `Fix` records.

The shim also emits its own `FL0000` diagnostics (all `Warning`, since CI hosts suppress
`Info`/`Hint`):

| Type | Meaning |
|---|---|
| `FSharpLint.HostIncompatible` | The host loaded an FCS minor version the shim can't bind against. See [Host compatibility](docs/host-compatibility.md). |
| `FSharpLint.ConfigError` | A discovered `fsharplint.json` couldn't be parsed; names the file and error, then lints with the default config. |
| `FSharpLint.InternalError` | FSharpLint hit an internal error it would otherwise swallow; the shim surfaces it. |
| `FSharpLint.Error` | FSharpLint reported a lint failure for the file (its description is passed through). |

## Development

```bash
mise run check    # build + test + lint
mise run build    # build only
mise run test     # tests only
```

## Dependencies

FSharpLint.Core is pulled from [michaelglass/FSharpLint](https://github.com/michaelglass/FSharpLint)
(`perf/two-phase-lint-api` branch) via a Paket git dependency. That branch merges
[Numpsy's `fcs10` branch](https://github.com/numpsy/FSharpLint/tree/fcs10), which
updated FSharpLint to FSharp.Compiler.Service 43.x — huge thanks to
[Numpsy (Richard Webb)](https://github.com/numpsy) for that work. It tracks the FCS
43.12 line and adds a two-phase lint API for analyzer integration. The shim pins FCS to
43.12.204; see [Host compatibility](docs/host-compatibility.md) for what that means for
the host you run it under.

## More

- [Host compatibility](docs/host-compatibility.md) — FCS binary coupling and the mismatch symptom.
- [Rule coverage](docs/rule-coverage.md) — which rules the test suite exercises, and the test layout.
- [Benchmarks](docs/benchmarks.md) — shim vs. the FSharpLint CLI on single projects and nested solutions.
