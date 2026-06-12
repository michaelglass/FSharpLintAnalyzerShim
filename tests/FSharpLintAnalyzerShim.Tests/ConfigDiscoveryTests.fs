/// Shares the "ConfigCache" collection with AllRulesCoverageTests: both mutate
/// the shim's module-level configCache, so they must not run in parallel.
[<Xunit.Collection("ConfigCache")>]
module FSharpLintAnalyzerShim.Tests.ConfigDiscoveryTests

open System
open System.IO
open Xunit
open Swensen.Unquote
open FSharpLint.Application
open FSharpLintAnalyzerShim.LintAnalyzer

[<Fact>]
let ``When no fsharplint.json exists in hierarchy, returns Default config`` () =
    // Use a temp directory with no fsharplint.json anywhere in its hierarchy
    let tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(tempDir) |> ignore

    try
        let fakeFile = Path.Combine(tempDir, "Test.fs")
        File.WriteAllText(fakeFile, "module Test")

        // Clear cache to ensure fresh lookup
        configCache.Clear()

        let result, diagnostics = getConfigParam fakeFile

        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>

        test <@ diagnostics |> List.isEmpty @>
    finally
        Directory.Delete(tempDir, true)

[<Fact>]
let ``When fsharplint.json exists in same directory, returns FromFile`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(tempDir) |> ignore

    try
        let configPath = Path.Combine(tempDir, "fsharplint.json")
        File.WriteAllText(configPath, "{}")

        let fakeFile = Path.Combine(tempDir, "Test.fs")
        File.WriteAllText(fakeFile, "module Test")

        configCache.Clear()

        let result, diagnostics = getConfigParam fakeFile

        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>

        test <@ diagnostics |> List.isEmpty @>
    finally
        Directory.Delete(tempDir, true)

[<Fact>]
let ``When fsharplint.json exists in parent directory, walks up and finds it`` () =
    let parentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    let childDir = Path.Combine(parentDir, "src")
    Directory.CreateDirectory(childDir) |> ignore

    try
        let configPath = Path.Combine(parentDir, "fsharplint.json")
        File.WriteAllText(configPath, "{}")

        let fakeFile = Path.Combine(childDir, "Test.fs")
        File.WriteAllText(fakeFile, "module Test")

        configCache.Clear()

        let result, diagnostics = getConfigParam fakeFile

        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>

        test <@ diagnostics |> List.isEmpty @>
    finally
        Directory.Delete(parentDir, true)

[<Fact>]
let ``Config is cached per directory`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(tempDir) |> ignore

    try
        let configPath = Path.Combine(tempDir, "fsharplint.json")
        File.WriteAllText(configPath, "{}")

        let fakeFile = Path.Combine(tempDir, "Test.fs")
        File.WriteAllText(fakeFile, "module Test")

        configCache.Clear()

        let result1, _ = getConfigParam fakeFile
        let result2, _ = getConfigParam fakeFile

        test
            <@
                match result1 with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>

        test <@ obj.ReferenceEquals(result1, result2) @>
    finally
        Directory.Delete(tempDir, true)

[<Fact>]
let ``Malformed fsharplint.json falls back to defaults with a diagnostic naming the file`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(tempDir) |> ignore

    try
        let configPath = Path.Combine(tempDir, "fsharplint.json")
        File.WriteAllText(configPath, "{ this is not valid json ")

        let fakeFile = Path.Combine(tempDir, "Test.fs")
        File.WriteAllText(fakeFile, "module Test")

        configCache.Clear()

        let result, diagnostics = getConfigParam fakeFile

        // Falls back to a usable configuration so linting proceeds.
        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>

        match diagnostics with
        | [ diagnostic ] ->
            test <@ diagnostic.Code = "FL0000" @>
            test <@ diagnostic.Type = "FSharpLint.ConfigError" @>
            test <@ diagnostic.Severity = FSharp.Analyzers.SDK.Severity.Warning @>
            test <@ diagnostic.Message.Contains(configPath) @>
            test <@ diagnostic.Message.Contains("Couldn't parse config") @>
            // Stack-trace frames are stripped from the surfaced error.
            test <@ not (diagnostic.Message.Contains("   at ")) @>
        | other -> failwith $"expected exactly one config diagnostic, got %A{other}"

        // The diagnostic is cached with the config: a second file in the same
        // directory surfaces it too.
        let _, diagnostics2 = getConfigParam (Path.Combine(tempDir, "Other.fs"))
        test <@ diagnostics2 |> List.isEmpty |> not @>
    finally
        Directory.Delete(tempDir, true)

[<Fact>]
let ``Cache hit at an ancestor directory back-fills the walked subdirectories`` () =
    let parentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    let childDir = Path.Combine(parentDir, "src")
    Directory.CreateDirectory(childDir) |> ignore

    try
        let configPath = Path.Combine(parentDir, "fsharplint.json")
        File.WriteAllText(configPath, "{}")

        let parentFile = Path.Combine(parentDir, "Test.fs")
        File.WriteAllText(parentFile, "module Test")

        let childFile = Path.Combine(childDir, "Child.fs")
        File.WriteAllText(childFile, "module Child")

        configCache.Clear()

        // First lookup caches the parent directory only.
        let parentResult, _ = getConfigParam parentFile
        test <@ configCache.ContainsKey(parentDir) @>
        test <@ not (configCache.ContainsKey(childDir)) @>

        // Second lookup walks up from the child, hits the parent's cache entry,
        // and back-fills the child directory with the same instance.
        let childResult, _ = getConfigParam childFile
        test <@ configCache.ContainsKey(childDir) @>
        test <@ obj.ReferenceEquals(parentResult, childResult) @>
    finally
        Directory.Delete(parentDir, true)
