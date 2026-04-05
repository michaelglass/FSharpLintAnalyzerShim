module FSharpLintAnalyzerShim.Tests.IntegrationTests

open Xunit
open Swensen.Unquote
open FSharpLint.Application
open FSharp.Analyzers.SDK
open FSharpLintAnalyzerShim.LintAnalyzer

let private lintMessages (source: string) : Message list =
    match Lint.lintSource Lint.OptionalLintParameters.Default source with
    | Lint.LintResult.Success warnings -> warnings |> List.map mapWarning
    | Lint.LintResult.Failure failure -> failwith $"Lint failed: {failure.Description}"

[<Fact>]
let ``lintSource with interface naming violation produces expected warning`` () =
    let messages =
        lintMessages "module Test\ntype iMyInterface =\n    abstract member DoStuff : unit -> unit\n"

    let interfaceWarnings = messages |> List.filter (fun m -> m.Code = "FL0036")
    test <@ interfaceWarnings.Length > 0 @>
    test <@ interfaceWarnings |> List.exists (fun m -> m.Message.Contains("I")) @>

[<Fact>]
let ``lintSource with clean code produces no warnings`` () =
    let messages = lintMessages "module Test\nlet add x y = x + y\n"
    test <@ messages |> List.isEmpty @>

[<Fact>]
let ``fsharplint:disable-next-line suppresses warning on following line`` () =
    let messages =
        lintMessages
            "module Test\n// fsharplint:disable-next-line\ntype iMyInterface =\n    abstract member DoStuff : unit -> unit\n"

    let interfaceWarnings = messages |> List.filter (fun m -> m.Code = "FL0036")
    test <@ interfaceWarnings |> List.isEmpty @>

[<Fact>]
let ``fsharplint:disable-next-line with specific rule only suppresses that rule`` () =
    let messages =
        lintMessages
            "module Test\n// fsharplint:disable-next-line InterfaceNames\ntype iMyInterface =\n    abstract member DoStuff : unit -> unit\n"

    let interfaceWarnings = messages |> List.filter (fun m -> m.Code = "FL0036")
    test <@ interfaceWarnings |> List.isEmpty @>

[<Fact>]
let ``fsharplint:disable suppresses warnings for rest of file`` () =
    let messages =
        lintMessages
            "module Test\n// fsharplint:disable InterfaceNames\ntype iFirst =\n    abstract member A : unit -> unit\ntype iSecond =\n    abstract member B : unit -> unit\n"

    let interfaceWarnings = messages |> List.filter (fun m -> m.Code = "FL0036")
    test <@ interfaceWarnings |> List.isEmpty @>

[<Fact>]
let ``fsharplint:enable re-enables a previously disabled rule`` () =
    let messages =
        lintMessages
            "module Test\n// fsharplint:disable InterfaceNames\ntype iSuppressed =\n    abstract member A : unit -> unit\n// fsharplint:enable InterfaceNames\ntype iNotSuppressed =\n    abstract member B : unit -> unit\n"

    let interfaceWarnings = messages |> List.filter (fun m -> m.Code = "FL0036")
    test <@ interfaceWarnings.Length > 0 @>
    test <@ interfaceWarnings |> List.forall (fun m -> m.Message.Contains("iNotSuppressed")) @>
