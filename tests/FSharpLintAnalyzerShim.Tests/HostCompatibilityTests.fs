module FSharpLintAnalyzerShim.Tests.HostCompatibilityTests

open System
open Xunit
open Swensen.Unquote
open FSharp.Analyzers.SDK
open FSharpLintAnalyzerShim.LintAnalyzer

let private expectedMajor, private expectedMinor = expectedFcsMajorMinor

[<Fact>]
let ``fcsCompatibilityMessages is empty when major.minor match the build pin`` () =
    let matching = Version(expectedMajor, expectedMinor, 999, 0)
    test <@ fcsCompatibilityMessages matching |> List.isEmpty @>

[<Fact>]
let ``fcsCompatibilityMessages names both versions on the 43.10-host mismatch`` () =
    // The exact published mismatch this guard exists for: fsharp-analyzers 0.36.0
    // hosts FCS 43.10.101 while the shim is built against a later line.
    let mismatched = Version(43, 10, 101, 0)

    match fcsCompatibilityMessages mismatched with
    | [ message ] ->
        test <@ message.Type = "FSharpLint.HostIncompatible" @>
        test <@ message.Code = "FL0000" @>
        test <@ message.Severity = Severity.Warning @>
        test <@ message.Message.Contains($"{expectedMajor}.{expectedMinor}.x") @>
        test <@ message.Message.Contains("43.10.101") @>
    | other -> failwith $"expected exactly one mismatch message, got %A{other}"

[<Fact>]
let ``fcsCompatibilityMessages refuses a host on the previous FCS line`` () =
    // A host still on the .NET 10 band (FCS 43.12) cannot run a shim built on 43.13:
    // FCS 43.13 renamed record-field members (record spreads) the rule engine binds to.
    match fcsCompatibilityMessages (Version(43, 12, 402, 0)) with
    | [ message ] -> test <@ message.Message.Contains("43.12.402") @>
    | other -> failwith $"expected exactly one mismatch message, got %A{other}"

[<Fact>]
let ``fcsCompatibilityMessages flags a different major version`` () =
    let mismatched = Version(99, expectedMinor, 0, 0)
    test <@ fcsCompatibilityMessages mismatched <> [] @>

[<Fact>]
let ``test host's own loaded FCS passes the guard`` () =
    // The test process loads the shim's pinned FCS line, so the live check is empty.
    let liveFcs = typeof<FSharp.Compiler.Text.range>.Assembly.GetName().Version
    test <@ fcsCompatibilityMessages liveFcs |> List.isEmpty @>

[<Fact>]
let ``lintAnalyzerForFcs short-circuits with the host-incompatible diagnostic on mismatch`` () =
    let mismatched = Version(expectedMajor, expectedMinor - 2, 101, 0)

    // The guard must fire before the context is touched (a real mismatched host
    // would crash on any FCS-typed member), so a null context proves the ordering.
    let messages =
        lintAnalyzerForFcs mismatched Unchecked.defaultof<CliContext>
        |> Async.RunSynchronously

    match messages with
    | [ message ] ->
        test <@ message.Type = "FSharpLint.HostIncompatible" @>
        test <@ message.Severity = Severity.Warning @>
    | other -> failwith $"expected exactly one host-incompatible message, got %A{other}"
