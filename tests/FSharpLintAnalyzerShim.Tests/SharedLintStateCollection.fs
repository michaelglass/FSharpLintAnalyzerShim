namespace FSharpLintAnalyzerShim.Tests

open Xunit

/// xunit collection serializing every test module that touches shared
/// process-global lint state. xunit.v3 runs distinct collections in parallel by
/// default, so without this barrier:
/// - a configCache.Clear() in one module races the cache assertions in another
///   (ConfigDiscoveryTests / AllRulesCoverageTests), and
/// - concurrent FSharpChecker typechecks (IntegrationTests' lintSource scripts
///   vs AllRulesCoverageTests' project checks) contend hard enough under
///   coverage instrumentation that FSharpLint's type-check-gated naming rules
///   (e.g. FL0036) silently drop warnings when symbol resolution degrades.
[<CollectionDefinition("SharedLintState")>]
type SharedLintStateCollection = class end
