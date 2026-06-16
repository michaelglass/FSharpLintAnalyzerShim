# Benchmarks

`benchmarks/run.sh` compares FSharpLint's native CLI (`dotnet fsharplint lint`)
against an in-process runner that mirrors the shim's code path (loading a project via
`Ionide.ProjInfo`, calling `Lint.lintProject` / `Lint.lintSolution`, mapping warnings).

```bash
mise run benchmark
```

The FsHotWatch scenarios assume a [FsHotWatch](https://github.com/michaelglass/FsHotWatch)
checkout as a sibling directory of this repo; point `FSHW_ROOT` at a checkout
elsewhere to override (relative paths resolve from this repo's root), or they are
skipped when absent.

Results below are from an M-series Mac, 5 runs + 1 warmup per scenario, against
published `dotnet-fsharplint` 0.26.10.

## SampleProject — 9 files, one fsproj

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 2.728 ± 0.041 | 2.686 | 2.776 | 1.00 |
| `shim (in-process runner)` | 3.195 ± 0.036 | 3.142 | 3.245 | 1.17 ± 0.02 |

## FsHotWatch core — one real project, ~40 files

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` | 5.704 ± 0.379 | 5.512 | 6.381 | 1.00 |
| `shim (in-process runner)` | 7.458 ± 0.156 | 7.259 | 7.622 | 1.31 ± 0.09 |

## FsHotWatch solution — 12 nested projects

| Command | Mean [s] | Min [s] | Max [s] | Relative |
|:---|---:|---:|---:|---:|
| `fsharplint CLI` (published 0.26.10) | 515.114 ± 120.826 | 381.536 | 641.692 | 15.20 ± 3.57 |
| `shim (in-process runner)` | 33.884 ± 0.253 | 33.472 | 34.149 | 1.00 |

On a single project the CLI wins by ~1.2–1.3× — the shim runner pays an extra `dotnet`
exe start-up and an Ionide.ProjInfo load to match what the CLI already amortises. **On
a nested solution the ordering flips hard: the shim finishes in ~34 s versus ~8½
minutes for the published CLI (≈15×).** The CLI's solution path builds a fresh
`WorkspaceLoader` and `FSharpChecker` per project; the shim reuses both across all 12.

## Why the gap is structural, not algorithmic

The slow CLI path is fixable upstream, not a property of FSharpLint's rules. A small
patch to `Lint.asyncLintSolution` (share one `WorkspaceLoader` + one `FSharpChecker`
across the whole solution, and map each project's options with a singleton known-set so
FCS resolves P2P references as DLLs) closes most of the gap. Measured on the same box,
`perf/two-phase-lint-api` with that patch runs the FsHotWatch solution in **19.9 s ±
0.24 s** — faster than the shim, because the CLI has no analyzer-host orchestration to
pay for. The shim's structural win is that it *always* shared those resources; once the
CLI does too, its overhead is lower.

The in-process runner at `benchmarks/BenchmarkRunner/` exists to make the shim path
measurable until an analyzer-host release ships the matching FCS line (see
[Host compatibility](host-compatibility.md)).
