<!-- sync:intro -->
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
<!-- sync:intro:end -->

<!-- sync:build-and-run -->
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
<!-- sync:build-and-run:end -->

<!-- sync:configuration -->
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
<!-- sync:configuration:end -->

<!-- sync:development -->
## Development

```bash
mise run check    # build + test + lint
mise run build    # build only
mise run test     # tests only
```
<!-- sync:development:end -->

## More

- [Host compatibility](host-compatibility.md) — FCS binary coupling, the mismatch symptom, and which analyzer hosts can load the shim.
- [Rule coverage](rule-coverage.md) — which rules the test suite exercises, and the test layout.
- [Benchmarks](benchmarks.md) — shim vs. the FSharpLint CLI on single projects and nested solutions.
