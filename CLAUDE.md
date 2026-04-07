# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A thin adapter that exposes FSharpLint rules as a single `[<CliAnalyzer>]` for the FSharp.Analyzers.SDK. The entire shim is one file (`LintAnalyzer.fs`) — it discovers `fsharplint.json`, converts `CliContext` to FSharpLint's `ParsedFileInformation`, calls `lintParsedFile`, and maps results back to Analyzer SDK `Message` records. No lint rules live here.

## Commands

```bash
# Build + test + lint (full check)
mise run check

# Build only
mise run build
# or: dotnet build FSharpLintAnalyzerShim.slnx

# Test only
mise run test
# or: dotnet test --solution FSharpLintAnalyzerShim.slnx

# Run a single test by name
dotnet test tests/FSharpLintAnalyzerShim.Tests --filter "FullyQualifiedName~TestMethodName"

# Format (fantomas)
mise run fmt

# Check formatting
mise run fmt:check
```

## Dependencies

- .NET 10 SDK (managed via mise)
- **Paket** for dependency management — run `paket install` after cloning
- FSharpLint.Core is pulled via Paket git dependency from `michaelglass/FSharpLint` (`perf/two-phase-lint-api` branch) into `paket-files/`
- Tests use xunit v3 + Unquote

## Architecture

- **`LintAnalyzer.fs`** — the entire shim. Key internals exposed via `InternalsVisibleTo` for testing:
  - `getConfigParam`: walks up directories to find `fsharplint.json`, caches parsed configs per directory in a `ConcurrentDictionary`
  - `mapWarning` / `mapFix`: convert FSharpLint types to Analyzer SDK types
  - `lintAnalyzer`: the `[<CliAnalyzer>]` entry point
- **`tests/FSharpLintAnalyzerShim.Tests/`** — three test files:
  - `ConfigDiscoveryTests.fs` — config file discovery, directory walking, caching
  - `WarningMappingTests.fs` — LintWarning→Message field mapping, fix mapping, severity
  - `IntegrationTests.fs` — end-to-end lint with real source containing violations

## Conventions

- Format with **fantomas** (`mise run fmt`); check with `mise run fmt:check`
- FSharpLint config is in `fsharplint.json` at the repo root
- 4-space indentation, 120 char line limit
