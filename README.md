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
- **AllRulesCoverageTests** -- runs the shim against `benchmarks/SampleProject`, a compiling F# project that deliberately violates most FSharpLint rules. Asserts every rule in the covered set fires at least once and that `Clean.fs` (a file with no intentional violations) produces zero warnings.

## Rule coverage

`benchmarks/SampleProject` exercises the shim against as many FSharpLint rules as can be triggered from compileable F# source under an aggressive `fsharplint.json` (small `maxLines`, `maxItems`, `maxComplexity` thresholds).

Sixty distinct rule codes are triggered and asserted in-process. Rules that require full project type-resolution to fire (e.g. `FL0014 RedundantNewKeyword`, `FL0016-21` raise/failwith/nullArg/invalidOp/invalidArg/failwithf single-argument rules, `FL0034 ReimplementsFunction`, `FL0035 CanBeReplacedWithComposition`, `FL0086 FavourAsKeyword`, `FL0093 DiscourageStringInterpolationWithStringFormat`) fire when run via the `dotnet fsharplint lint` CLI but are not consistently surfaced by `Lint.lintProject` in the in-process test harness; the CLI path covers them. Rules that cannot be triggered at all inside a compileable project (e.g. `FL0064 NoTabCharacters` -- the F# compiler rejects tabs outright) are documented in `benchmarks/SampleProject/Rules/Typography.fs`.

## Benchmark

`benchmarks/run.sh` compares FSharpLint's native CLI (`dotnet fsharplint lint`) against an in-process runner that mirrors the shim's code path (loading a project via `Ionide.ProjInfo`, calling `Lint.lintProject` / `Lint.lintSolution`, mapping warnings). Run with:

```bash
mise run benchmark
```

Results below are from an M-series Mac, 5 runs + 1 warmup per scenario.

### SampleProject — 9 files, one fsproj

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 2.555 ± 0.010 | 2.540 | 2.565 | 1.00 |
| `shim (in-process runner)` | 3.052 ± 0.031 | 3.011 | 3.076 | 1.19 ± 0.01 |

### FsHotWatch core — one real project, ~40 files

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 5.238 ± 0.079 | 5.168 | 5.371 | 1.00 |
| `shim (in-process runner)` | 6.816 ± 0.101 | 6.681 | 6.913 | 1.30 ± 0.03 |

### FsHotWatch solution — 12 nested projects

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 479.963 ± 63.614 | 394.395 | 566.638 | 9.71 ± 2.43 |
| `shim (in-process runner)` | 49.446 ± 10.502 | 39.291 | 64.343 | 1.00 |

On a single project the CLI wins by ~1.2-1.3× -- the shim runner pays an extra `dotnet` exe start-up and an Ionide.ProjInfo load to match what the CLI already amortises. **On a nested solution the ordering flips: the shim path finishes in ~50 s versus ~8 minutes for the CLI (9.7× faster).** `dotnet fsharplint lint` on a `.slnx` appears to spin the MSBuild pipeline and FSharpChecker up per project; `Lint.lintSolution` called through the shim reuses both across all 12 projects.

### Host compatibility note

The shim binaries are built against FCS 43.12.202 to match [fshw](https://github.com/dawedawe/fshw) and other analyzer hosts on that FCS line. `fsharp-analyzers` 0.36.0 pins FCS 43.10.101, so loading the shim directly into that host raises an ABI mismatch. The in-process runner at `benchmarks/BenchmarkRunner/` exists to make the shim path measurable until the two hosts converge on a shared FCS version.
