module FSharpLintAnalyzerShim.Tests.IntegrationTests

open Xunit
open Swensen.Unquote
open FSharpLint.Application
open FSharpLintAnalyzerShim.LintAnalyzer

[<Fact>]
let ``lintSource with interface naming violation produces expected warning`` () =
    let source =
        "module Test\ntype iMyInterface =\n    abstract member DoStuff : unit -> unit\n"

    let result = Lint.lintSource Lint.OptionalLintParameters.Default source

    match result with
    | Lint.LintResult.Success warnings ->
        let messages = warnings |> List.map mapWarning

        test <@ messages.Length > 0 @>

        let interfaceWarning = messages |> List.tryFind (fun m -> m.Code = "FL0036")

        test <@ interfaceWarning.IsSome @>
        test <@ interfaceWarning.Value.Message.Contains("I") @>
    | Lint.LintResult.Failure failure -> failwith $"Lint failed: {failure.Description}"

[<Fact>]
let ``lintSource with clean code produces no warnings`` () =
    let source = "module Test\nlet add x y = x + y\n"

    let result = Lint.lintSource Lint.OptionalLintParameters.Default source

    match result with
    | Lint.LintResult.Success warnings ->
        let messages = warnings |> List.map mapWarning
        test <@ messages |> List.isEmpty @>
    | Lint.LintResult.Failure failure -> failwith $"Lint failed: {failure.Description}"
