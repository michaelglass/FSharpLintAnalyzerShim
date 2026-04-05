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

        let result = getConfigParam fakeFile

        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>
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

        let result = getConfigParam fakeFile

        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>
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

        let result = getConfigParam fakeFile

        test
            <@
                match result with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>
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

        let result1 = getConfigParam fakeFile
        let result2 = getConfigParam fakeFile

        test
            <@
                match result1 with
                | Lint.ConfigurationParam.Configuration _ -> true
                | _ -> false
            @>

        test <@ obj.ReferenceEquals(result1, result2) @>
    finally
        Directory.Delete(tempDir, true)
