module FSharpLintAnalyzerShim.Tests.WarningMappingTests

open Xunit
open Swensen.Unquote
open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharpLint.Framework.Suggestion
open FSharpLintAnalyzerShim.LintAnalyzer

let private testRange = Range.range0

let private makeWarning ruleName ruleId message suggestedFix : LintWarning =
    { RuleIdentifier = ruleId
      RuleName = ruleName
      FilePath = "Test.fs"
      ErrorText = ""
      Details =
        { Range = testRange
          Message = message
          SuggestedFix = suggestedFix
          TypeChecks = [] } }

[<Fact>]
let ``mapFix correctly maps SuggestedFix fields to Fix fields`` () =
    let suggestedFix: SuggestedFix =
        { FromText = "old"
          FromRange = testRange
          ToText = "new" }

    let result = mapFix suggestedFix

    test <@ result.FromText = "old" @>
    test <@ result.FromRange = testRange @>
    test <@ result.ToText = "new" @>

[<Fact>]
let ``mapWarning maps RuleName to Type and RuleIdentifier to Code`` () =
    let warning = makeWarning "InterfaceNames" "FL0036" "test message" None
    let result = mapWarning warning

    test <@ result.Type = "InterfaceNames" @>
    test <@ result.Code = "FL0036" @>

[<Fact>]
let ``mapWarning maps Message correctly`` () =
    let warning = makeWarning "SomeRule" "FL0001" "Expected message text" None
    let result = mapWarning warning

    test <@ result.Message = "Expected message text" @>

[<Fact>]
let ``mapWarning sets Severity to Warning`` () =
    let warning = makeWarning "SomeRule" "FL0001" "msg" None
    let result = mapWarning warning

    test <@ result.Severity = Severity.Warning @>

[<Fact>]
let ``mapWarning maps SuggestedFix to Fixes list when present`` () =
    let suggestedFix: SuggestedFix =
        { FromText = "old"
          FromRange = testRange
          ToText = "new" }

    let warning =
        makeWarning "SomeRule" "FL0001" "msg" (Some(lazy (Some suggestedFix)))

    let result = mapWarning warning

    test <@ result.Fixes.Length = 1 @>
    test <@ result.Fixes.[0].FromText = "old" @>
    test <@ result.Fixes.[0].ToText = "new" @>

[<Fact>]
let ``mapWarning returns empty Fixes when no SuggestedFix`` () =
    let warning = makeWarning "SomeRule" "FL0001" "msg" None
    let result = mapWarning warning

    test <@ result.Fixes = [] @>

[<Fact>]
let ``mapWarning returns empty Fixes when SuggestedFix lazy evaluates to None`` () =
    let warning = makeWarning "SomeRule" "FL0001" "msg" (Some(lazy None))
    let result = mapWarning warning

    test <@ result.Fixes = [] @>
