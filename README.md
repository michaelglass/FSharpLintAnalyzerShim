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

## Host compatibility

The shim's rule engine (FSharpLint.Core) is compiled against a specific FSharp.Compiler.Service line and is **binary-coupled** to it: it consumes FCS's typed-AST surface directly, so it can only run inside an analyzer host loading the **same FCS major.minor**.

| | FCS version |
|---|---|
| Shim is built against | **43.12.204** (see the `FSharp.Compiler.Service` pin in `FSharpLintAnalyzerShim.fsproj`) |
| Required host FCS line | **43.12.x** |
| `fsharp-analyzers` 0.36.0 (latest as of writing) ships | 43.10.101 |

Because `fsharp-analyzers` 0.36.0 — the only published `fsharp-analyzers` release — ships FCS 43.10.101, **loading the shim directly into that host is not supported**: the FCS lines differ (43.10 vs 43.12). Hosts on the 43.12 line (e.g. a build of [fshw](https://github.com/dawedawe/fshw) pinned there) can load it.

**Symptom of a mismatch:** rather than crash with a cryptic `TypeLoadException` / `MissingMethodException` deep inside linting, the shim performs a startup version check and emits a single `FL0000` **Warning** diagnostic (`FSharpLint.HostIncompatible`) naming both the version it was built against and the version the host loaded, and produces no lint results for that run. If you see that diagnostic, run the shim under an analyzer host on the FCS 43.12 line, or rebuild the shim against your host's FCS.

## Diagnostics

All FSharpLint rule diagnostics use the standard FSharpLint rule codes (`FL0001` through `FL0097`). Severity is always `Warning`. Suggested fixes from FSharpLint are passed through as Analyzer SDK `Fix` records.

The shim also emits `FL0000` diagnostics for its own conditions:

- **`FSharpLint.HostIncompatible`** (`Warning`) — the analyzer host loaded an FCS minor version the shim can't bind against (see [Host compatibility](#host-compatibility)).
- **`FSharpLint.InternalError`** — FSharpLint hit an internal error it would otherwise swallow silently; the shim surfaces it instead of dropping it.

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

FSharpLint.Core is pulled from [michaelglass/FSharpLint](https://github.com/michaelglass/FSharpLint) (`perf/two-phase-lint-api` branch) via Paket git dependency. This branch merges [Numpsy's `fcs10` branch](https://github.com/numpsy/FSharpLint/tree/fcs10), which updated FSharpLint to FSharp.Compiler.Service 43.x -- huge thanks to [Numpsy (Richard Webb)](https://github.com/numpsy) for that work. The branch tracks the FCS 43.12 line and adds a two-phase lint API for analyzer integration. The shim pins FCS to 43.12.204; see [Host compatibility](#host-compatibility) for what that means for the analyzer host you run it under.

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

Results below are from an M-series Mac, 5 runs + 1 warmup per scenario, against published `dotnet-fsharplint` 0.26.10.

### SampleProject — 9 files, one fsproj

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 2.728 ± 0.041 | 2.686 | 2.776 | 1.00 |
| `shim (in-process runner)` | 3.195 ± 0.036 | 3.142 | 3.245 | 1.17 ± 0.02 |

### FsHotWatch core — one real project, ~40 files

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 5.704 ± 0.379 | 5.512 | 6.381 | 1.00 |
| `shim (in-process runner)` | 7.458 ± 0.156 | 7.259 | 7.622 | 1.31 ± 0.09 |

### FsHotWatch solution — 12 nested projects

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` (published 0.26.10) | 515.114 ± 120.826 | 381.536 | 641.692 | 15.20 ± 3.57 |
| `shim (in-process runner)` | 33.884 ± 0.253 | 33.472 | 34.149 | 1.00 |

On a single project the CLI wins by ~1.2–1.3× — the shim runner pays an extra `dotnet` exe start-up and an Ionide.ProjInfo load to match what the CLI already amortises. **On a nested solution the ordering flips hard: the shim finishes in ~34 s versus ~8½ minutes for the published CLI (≈15×).** The CLI's solution path builds a fresh `WorkspaceLoader` and `FSharpChecker` per project; the shim reuses both across all 12.

### Why the gap is structural, not algorithmic

The slow CLI path is fixable upstream, not a property of FSharpLint's rules. A small patch to `Lint.asyncLintSolution` (share one `WorkspaceLoader` + one `FSharpChecker` across the whole solution, and map each project's options with a singleton known-set so FCS resolves P2P references as DLLs) closes most of the gap. Measured on the same box, `perf/two-phase-lint-api` with that patch runs the FsHotWatch solution in **19.9 s ± 0.24 s** — faster than the shim, because the CLI has no analyzer-host orchestration to pay for. The shim's structural win is that it *always* shared those resources; once the CLI does too, its overhead is lower.

### Host compatibility note

See the [Host compatibility](#host-compatibility) section below for the exact FCS versions and the mismatch symptom. The in-process runner at `benchmarks/BenchmarkRunner/` exists to make the shim path measurable until an analyzer-host release ships the matching FCS line.
