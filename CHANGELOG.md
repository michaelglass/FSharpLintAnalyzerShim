# Changelog

## Unreleased

- chore: bump local dev-tools — coverageratchet 0.15.0-alpha.8 → 0.15.0-alpha.10, syncdocs 0.13.0-alpha.2 → 0.13.0-alpha.3, fsprojlint 0.10.0-alpha.11 → 0.10.0-alpha.13, fsdocs-tool 21.0.0 → 22.1.0.
- deps: bump `Microsoft.Testing.Extensions.CodeCoverage` 18.8.0 → 18.9.0; forward-pin `System.Security.Cryptography.Xml` 10.0.9 → 10.0.10 via repo-root `Directory.Build.props` (10.0.9 gained new High-severity advisories: GHSA-23rf-6693-g89p, -8q5v-6pqq-x66h, -cvvh-rhrc-wg4q, -g8r8-53c2-pm3f, -mmjf-rqrv-855v). Clears the advisory on the shim's own projects; the bundled `FSharpLint.Core` git-dep still pins 10.0.9 via its own CPM (needs an upstream bump).
- chore: adopt the RefStamp ref-stamp guard (AUTOMATION-123) via repo-root `Directory.Build.props` (`PrivateAssets="all"`; build/test-inert) to satisfy fsprojlint 0.10.0-alpha.13's "local packs are ref-stamped" check.

## 0.3.0-alpha.7 - 2026-07-02

- fix: no more `FSharpLint.InternalError` ("ProjectOptions are not available" / NRE) when the analyzer host supplies **TransparentCompiler** check results (fshw does). Root cause was in the bundled `FSharpLint.Core`'s two-phase `ProjectOptions` lazy — `FSharpProjectContext.get_ProjectOptions()` throws by design under TransparentCompiler, and rules using the library heuristics (`AsynchronousFunctionNames` & co.) force that lazy. Fixed at source on `michaelglass/FSharpLint` `perf/two-phase-lint-api` (`0901c230`: degrade to `None` so heuristics fall back to `Unlikely`); the shim re-pins the Paket commit and adds a TransparentCompiler regression test. Notably this fired on files that fall back to FSharpLint's *default* config (which enables those rules) — e.g. NuGet-injected compile items like xunit.v3's `_content/DefaultRunnerReporters.fs`, where per-file config discovery finds no repo `fsharplint.json` (thellma/intelligence AUTOMATION-49).

## 0.3.0-alpha.6 - 2026-06-25

- security: bump the **bundled** `FSharpLint.Core`'s transitive `System.Security.Cryptography.Xml` 9.0.0 → 10.0.9 (GHSA-37gx-xxp4-5rgx, High). The shim's own projects already pinned 10.0.9 via repo-root `Directory.Build.props` (alpha.3/alpha.5), but `FSharpLint.Core` — pulled as a Paket git-dependency `ProjectReference` — sits under the fork's own `Directory.Build.props`, which shadows the repo-root override (MSBuild uses the nearest `Directory.Build.props` walking up, not a merge). So the bundled copy still resolved 9.0.0 and was merely NU1903-suppressed. Fixed at source on `michaelglass/FSharpLint` `perf/two-phase-lint-api` (CPM `PackageVersion` + net10-scoped `PackageReference`, dropping the NU1903 suppression); the shim re-pins the Paket commit so the bundled `FSharpLint.Core` now resolves 10.0.9 with zero 9.0.0 in the restore graph.

## 0.3.0-alpha.5 - 2026-06-24

- Bump `FSharp.Analyzers.SDK` 0.36.0 → 0.37.2 (now ships FCS 43.12.201, on the same 43.12 line the shim is built against — **resolves the `FL0000` host-incompatibility caveat** from 0.3.0-alpha.4), `FSharp.Core` 10.1.204 → 10.1.301, `System.Security.Cryptography.Xml` 10.0.8 → 10.0.9.

## 0.3.0-alpha.4 - 2026-06-12

- feat: emit a single `FL0000` Warning (`FSharpLint.HostIncompatible`) naming both
  FCS versions when the analyzer host's FCS minor version differs from the line the
  shim is built against, instead of crashing with a `TypeLoadException` /
  `MissingMethodException` deep inside linting. `fsharp-analyzers` 0.36.0 ships FCS
  43.10.101 while the shim is built against 43.12.204, so loading the shim directly
  into that host surfaces this diagnostic and produces no lint results for the run;
  run it under an analyzer host on the FCS 43.12 line. Documented in the README
  "Host compatibility" section.
- feat: surface a malformed `fsharplint.json` as an `FL0000` Warning
  (`FSharpLint.ConfigError`) naming the file and the parse error, then lint with
  FSharpLint's default configuration instead of failing the whole file with an
  Info-severity message that buried the raw exception and never named the config.
  The diagnostic is cached per directory.
- fix: report internal lint failures and recovered lint exceptions as `Warning`
  rather than `Info` so CI analyzer hosts (which suppress `Info`/`Hint`) no longer
  silently hide a degraded or failed lint run.
- chore: bump local tools — coverageratchet 0.15.0-alpha.8, fssemantictagger
  0.13.0-alpha.13, fsprojlint 0.10.0-alpha.9.

## 0.3.0-alpha.3 - 2026-05-28

- security: pin transitive `System.Security.Cryptography.Xml` to 10.0.8 (9.0.0 has GHSA-37gx-xxp4-5rgx, High severity) via repo-root `Directory.Build.props`.
- Bump `FSharp.Compiler.Service` 43.12.202 → 43.12.204 and the coupled `FSharp.Core` 10.1.202 → 10.1.204 (both stay inside the 10.1.* pin).

## 0.3.0-alpha.2 - 2026-05-28

- Bump `Microsoft.Testing.Extensions.CodeCoverage` 18.6.2 → 18.7.0.
  Other external deps held: FSharp.Compiler.Service stays at 43.12.202 and
  FSharp.Core at 10.1.202 to remain binary-compatible with analyzer hosts
  (fshw pins FCS 43.12.202) and FSharpLint.Core's central pin; FSharp.Analyzers.SDK
  stays at 0.36.0. No YamlDotNet dependency exists (this FSharpLint branch uses
  JSON config).
- Config lookup: cache the resolved `ConfigurationParam` on every directory
  along the walk path, not just the starting directory. With N source
  directories sharing one root `fsharplint.json`, each lookup now short-
  circuits on the first cache hit rather than re-walking to the root.

## v0.2.0-alpha.2

- **Migrate to FSharp.Compiler.Service 43.12.202** (was 43.10.101).
  Required to avoid `MissingMethodException: LetOrUse.get_isBang()` when
  hosted by analyzer runners built against FCS 43.12 (the current Ionide /
  fshw stack). Shim fsproj now pins FCS 43.12.202 explicitly, overriding
  the SDK 0.36.0 transitive pin at 43.10.101. FSharp.Core bumped to 10.1.202
  to match.
- Add LICENSE file (MIT)
- Wire up fsprojlint linting in mise.toml
- Update NuGet dependencies to latest versions
- Bump internal tool versions (coverageratchet, syncdocs, fssemantictagger, fsprojlint) across multiple rounds

## v0.2.0-alpha.1

- Make package publishable to NuGet with bundled dependencies and release workflow
- Add coverage support: CodeCoverage package, coverage-ratchet config, updated mise.toml
- Fix release workflow: set `no-build-pack=false` for BundleDeps target

## v0.1.0-alpha.1

- Initial implementation: all 97 FSharpLint rules exposed as an FSharp.Analyzers.SDK analyzer
- Add fantomas formatting, linting, and CI
- Document rule suppression and add Numpsy attribution
- Add integration tests for fsharplint suppression directives
- Add paket to CI and suppress NU1608 warnings in test project
- Adopt reusable build workflow from MichaelsWackyFsPackageTools
- Add NuGet Trusted Publishing setup
- Enable paket-restore in CI workflow
- Fix: lint FL0065 in tests (`x = []` -> `List.isEmpty`)
- Fix mise.toml release tasks to match fssemantictagger CLI syntax
