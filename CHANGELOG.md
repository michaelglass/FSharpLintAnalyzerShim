# Changelog

## Unreleased

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
