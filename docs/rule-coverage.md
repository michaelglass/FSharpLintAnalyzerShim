# Rule coverage

`benchmarks/SampleProject` exercises the shim against as many FSharpLint rules as can
be triggered from compileable F# source under an aggressive `fsharplint.json` (small
`maxLines`, `maxItems`, `maxComplexity` thresholds).

Sixty distinct rule codes are triggered and asserted in-process. Some rules need full
project type-resolution to fire (e.g. `FL0014 RedundantNewKeyword`; `FL0016-21`
raise/failwith/nullArg/invalidOp/invalidArg/failwithf single-argument rules; `FL0034
ReimplementsFunction`; `FL0035 CanBeReplacedWithComposition`; `FL0086 FavourAsKeyword`;
`FL0093 DiscourageStringInterpolationWithStringFormat`). These fire under `dotnet
fsharplint lint` but are not consistently surfaced by `Lint.lintProject` in the
in-process test harness; the CLI path covers them.

Rules that cannot be triggered at all inside a compileable project (e.g. `FL0064
NoTabCharacters` — the F# compiler rejects tabs outright) are documented in
`benchmarks/SampleProject/Rules/Typography.fs`.

## Test suite

- **ConfigDiscoveryTests** — config file discovery, directory walking, caching.
- **WarningMappingTests** — `LintWarning`-to-`Message` field mapping, fix mapping, severity.
- **HostCompatibilityTests** — the FCS version guard (see [Host compatibility](host-compatibility.md)).
- **IntegrationTests** — end-to-end: lint source with violations, verify mapped output.
- **AllRulesCoverageTests** — runs the shim against `benchmarks/SampleProject`, a
  compiling F# project that deliberately violates most FSharpLint rules. Asserts every
  rule in the covered set fires at least once, and that `Clean.fs` (no intentional
  violations) produces zero warnings.
