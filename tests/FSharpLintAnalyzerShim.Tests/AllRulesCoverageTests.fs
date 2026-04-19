module FSharpLintAnalyzerShim.Tests.AllRulesCoverageTests

open System.IO
open Xunit
open Swensen.Unquote
open FSharpLint.Application
open FSharp.Analyzers.SDK
open FSharpLintAnalyzerShim.LintAnalyzer

/// Walk up from the test binary to find the repository root (where README.md lives).
let private repoRoot =
    let rec walkUp (dir: string) =
        if File.Exists(Path.Combine(dir, "README.md")) && File.Exists(Path.Combine(dir, "LintAnalyzer.fs")) then
            dir
        else
            let parent = Directory.GetParent(dir)

            if isNull parent then
                failwith "Could not locate repo root"
            else
                walkUp parent.FullName

    walkUp System.AppContext.BaseDirectory

let private sampleRoot = Path.Combine(repoRoot, "benchmarks", "SampleProject")
let private rulesDir = Path.Combine(sampleRoot, "Rules")
let private sampleConfig = Path.Combine(sampleRoot, "fsharplint.json")
let private sampleProject = Path.Combine(sampleRoot, "SampleProject.fsproj")

let private toolsPath =
    lazy (Ionide.ProjInfo.Init.init (System.IO.DirectoryInfo sampleRoot) None)

/// Lint the whole sample project in-process with the shim's config-discovery applied
/// to a representative source file. Returns Messages (shim-mapped from LintWarning).
let private lintSampleProjectViaShim () : Message list =
    let configParam = getConfigParam (Path.Combine(rulesDir, "Naming.fs"))

    match
        Lint.lintProject
            { Lint.OptionalLintParameters.Default with Configuration = configParam }
            sampleProject
            toolsPath.Value
    with
    | LintResult.Success warnings -> warnings |> List.map mapWarning
    | LintResult.Failure failure -> failwith $"Lint failed on project: {failure.Description}"

let private lintFileViaShim (filePath: string) : Message list =
    let configParam = getConfigParam filePath

    match Lint.lintFile { Lint.OptionalLintParameters.Default with Configuration = configParam } filePath with
    | LintResult.Success warnings -> warnings |> List.map mapWarning
    | LintResult.Failure failure -> failwith $"Lint failed on {filePath}: {failure.Description}"

/// The set of FL codes we expect the sample project to trigger (in aggregate).
/// This is the contract: if the shim drops one of these on the floor, the test fails.
let private expectedCodes =
    [ "FL0001" // TupleCommaSpacing
      "FL0004" // PatternMatchClausesOnNewLine
      "FL0006" // PatternMatchClauseIndentation
      "FL0008" // ModuleDeclSpacing
      "FL0010" // TypedItemSpacing
      "FL0011" // TypePrefixing
      "FL0013" // RecursiveAsyncFunction
      // Note: FL0014, FL0016-21, FL0034, FL0035, FL0086, FL0093 require full project
      // type-check resolution beyond what Lint.lintProject surfaces in this in-process
      // test (they all fire when run via the fsharp-analyzers CLI). See README rule-coverage
      // notes for the full split.
      "FL0022" // MaxLinesInLambdaFunction
      "FL0023" // MaxLinesInMatchLambdaFunction
      "FL0024" // MaxLinesInValue
      "FL0025" // MaxLinesInFunction
      "FL0026" // MaxLinesInMember
      "FL0030" // MaxLinesInRecord
      "FL0031" // MaxLinesInEnum
      "FL0032" // MaxLinesInUnion
      "FL0036" // InterfaceNames
      "FL0037" // ExceptionNames
      "FL0038" // TypeNames
      "FL0039" // RecordFieldNames
      "FL0040" // EnumCasesNames
      "FL0041" // UnionCasesNames
      "FL0043" // LiteralNames
      "FL0045" // MemberNames
      "FL0046" // ParameterNames
      "FL0047" // MeasureTypeNames
      "FL0048" // ActivePatternNames
      "FL0049" // PublicValuesNames
      "FL0051" // MaxNumberOfItemsInTuple
      "FL0052" // MaxNumberOfFunctionParameters
      "FL0053" // MaxNumberOfMembers
      "FL0054" // MaxNumberOfBooleanOperatorsInCondition
      "FL0055" // FavourIgnoreOverLetWild
      "FL0056" // WildcardNamedWithAsPattern
      "FL0057" // UselessBinding
      "FL0058" // TupleOfWildcards
      "FL0060" // MaxCharactersOnLine
      "FL0063" // TrailingNewLineInFile
      "FL0065" // Hints
      "FL0066" // NoPartialFunctions
      "FL0067" // PrivateValuesNames
      "FL0069" // GenericTypesNames
      "FL0070" // FavourTypedIgnore
      "FL0071" // CyclomaticComplexity
      "FL0072" // FailwithBadUsage
      "FL0073" // FavourReRaise
      "FL0075" // AvoidTooShortNames
      "FL0076" // FavourStaticEmptyFields
      "FL0077" // AvoidSinglePipeOperator
      "FL0079" // SuggestUseAutoProperty
      "FL0080" // UnnestedFunctionNames
      "FL0081" // NestedFunctionNames
      "FL0082" // UsedUnderscorePrefixedElements
      "FL0083" // UnneededRecKeyword
      "FL0085" // EnsureTailCallDiagnosticsInRecursiveFunctions
      "FL0087" // InterpolatedStringWithNoSubstitution
      "FL0088" // IndexerAccessorStyleConsistency
      "FL0089" // FavourSingleton
      "FL0092" ] // DisallowShadowing

[<Fact>]
let ``sample project exercises the documented rule coverage`` () =
    let messages = lintSampleProjectViaShim ()
    let allCodes = messages |> List.map (fun m -> m.Code) |> Set.ofList

    let missing = Set.difference (Set.ofList expectedCodes) allCodes

    if not (Set.isEmpty missing) then
        failwithf
            "Missing %d codes: %A\nObserved %d distinct codes (%d total warnings): %A"
            (Set.count missing)
            (missing |> Set.toList)
            (Set.count allCodes)
            (List.length messages)
            (allCodes |> Set.toList)

[<Fact>]
let ``Clean.fs produces zero warnings`` () =
    let cleanFile = Path.Combine(rulesDir, "Clean.fs")
    let messages = lintFileViaShim cleanFile
    test <@ messages |> List.isEmpty @>

[<Fact>]
let ``every sample file is loadable and lintable without Lint.Failure`` () =
    let ruleFiles = Directory.GetFiles(rulesDir, "*.fs")

    for file in ruleFiles do
        let configParam = getConfigParam file

        match Lint.lintFile { Lint.OptionalLintParameters.Default with Configuration = configParam } file with
        | LintResult.Success _ -> ()
        | LintResult.Failure failure -> failwithf "lint failed on %s: %s" file failure.Description

[<Fact>]
let ``shim config discovery finds sample fsharplint.json`` () =
    let sampleFile = Path.Combine(rulesDir, "Naming.fs")
    let _ = getConfigParam sampleFile
    // The discovered config is the one next to the file (sampleRoot).
    // We verify indirectly: linting with discovered config produces aggressive warnings
    // that wouldn't appear under the root repo's fsharplint.json.
    let messages = lintFileViaShim sampleFile
    let codes = messages |> List.map (fun m -> m.Code) |> Set.ofList
    // FL0075 (AvoidTooShortNames) is disabled in the repo root config but enabled in sample.
    test <@ codes.Contains "FL0075" @>
    // Keep sampleConfig referenced to avoid unused binding.
    test <@ File.Exists sampleConfig @>
