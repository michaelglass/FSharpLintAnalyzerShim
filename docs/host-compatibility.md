# Host compatibility

The shim's rule engine (FSharpLint.Core) is compiled against a specific
FSharp.Compiler.Service (FCS) line and is **binary-coupled** to it: it consumes FCS's
typed-AST surface directly, so it can only run inside an analyzer host loading the
**same FCS major.minor**.

| | FCS version |
|---|---|
| Shim is built against | **43.12.204** (the `FSharp.Compiler.Service` pin in `FSharpLintAnalyzerShim.fsproj`) |
| Required host FCS line | **43.12.x** |
| `fsharp-analyzers` 0.36.0 (the only published release) ships | 43.10.101 |

Because `fsharp-analyzers` 0.36.0 ships FCS 43.10.101, **loading the shim directly into
that host is not supported** — the FCS lines differ (43.10 vs 43.12). A host on the
43.12 line (e.g. a build of [fshw](https://github.com/michaelglass/FsHotWatch) pinned
there) can load it.

## Mismatch symptom

Rather than crash with a cryptic `TypeLoadException` / `MissingMethodException` deep
inside linting, the shim runs a startup version check and emits a single `FL0000`
**Warning** diagnostic (`FSharpLint.HostIncompatible`) naming both the version it was
built against and the version the host loaded, and produces no lint results for that
run. If you see that diagnostic, run the shim under an analyzer host on the FCS 43.12
line, or rebuild the shim against your host's FCS.
