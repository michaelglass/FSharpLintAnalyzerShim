# Changelog

## Unreleased

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
