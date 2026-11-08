/// In the SharedLintState collection: mutates the shim's module-level configCache,
/// so it must not run in parallel with the other cache-mutating module.
[<Xunit.Collection("SharedLintState")>]
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

[<Fact>]
let ``With no fsharplint.json in the hierarchy, a second lookup is a cache hit with no recorded origin`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(tempDir) |> ignore

    try
        let fakeFile = Path.Combine(tempDir, "Test.fs")
        File.WriteAllText(fakeFile, "module Test")

        configCache.Clear()
        configOrigins.Clear()

        let result1, _ = getConfigParam fakeFile
        let result2, _ = getConfigParam fakeFile

        // Nothing to re-read: the default config is served from the cache as-is.
        test <@ not (configOrigins.ContainsKey tempDir) @>
        test <@ obj.ReferenceEquals(result1, result2) @>
    finally
        Directory.Delete(tempDir, true)

[<Fact>]
let ``A config with no ignoreFiles key ignores nothing`` () =
    let config =
        Lint.ConfigurationParam.Configuration(FSharpLint.Framework.Configuration.parseConfig "{}")

    test <@ not (isIgnoredFile config (Path.Combine(Path.GetTempPath(), "Any.fs"))) @>

[<Fact>]
let ``positive control: a config whose ignoreFiles names the file ignores it`` () =
    let config =
        Lint.ConfigurationParam.Configuration(
            FSharpLint.Framework.Configuration.parseConfig """{ "ignoreFiles": [ "Generated/Any.fs" ] }"""
        )

    test <@ isIgnoredFile config (Path.Combine(Path.GetTempPath(), "Generated", "Any.fs")) @>

[<Fact>]
let ``An ignoreFiles ** segment matches any number of directories`` () =
    let config =
        Lint.ConfigurationParam.Configuration(
            FSharpLint.Framework.Configuration.parseConfig """{ "ignoreFiles": [ "**/obj/**/*.fs" ] }"""
        )

    let underObj (segments: string list) =
        Path.Combine(Array.ofList (Path.GetTempPath() :: "repo" :: "obj" :: segments))

    test <@ isIgnoredFile config (underObj [ "X.fs" ]) @>
    test <@ isIgnoredFile config (underObj [ "Debug"; "X.fs" ]) @>
    test <@ isIgnoredFile config (underObj [ "Debug"; "net10.0"; "X.fs" ]) @>
    test <@ not (isIgnoredFile config (Path.Combine(Path.GetTempPath(), "repo", "src", "X.fs"))) @>

[<Fact>]
let ``Editing one fsharplint.json re-reads only the directories parsed from it`` () =
    let rootA = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    let rootB = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(rootA) |> ignore
    Directory.CreateDirectory(rootB) |> ignore

    try
        File.WriteAllText(Path.Combine(rootA, "fsharplint.json"), "{}")
        File.WriteAllText(Path.Combine(rootB, "fsharplint.json"), "{}")
        let fileA = Path.Combine(rootA, "A.fs")
        let fileB = Path.Combine(rootB, "B.fs")

        configCache.Clear()
        configOrigins.Clear()

        let a1, _ = getConfigParam fileA
        let b1, _ = getConfigParam fileB

        File.WriteAllText(Path.Combine(rootA, "fsharplint.json"), """{ "ignoreFiles": [] }""")

        let a2, _ = getConfigParam fileA
        let b2, _ = getConfigParam fileB

        // The edited config is re-parsed; the untouched one is still the cached instance.
        test <@ not (obj.ReferenceEquals(a1, a2)) @>
        test <@ obj.ReferenceEquals(b1, b2) @>
    finally
        configCache.Clear()
        configOrigins.Clear()
        Directory.Delete(rootA, true)
        Directory.Delete(rootB, true)

[<Fact>]
let ``With no fsharplint.json in the hierarchy, an ancestor cache hit back-fills subdirectories without an origin`` () =
    // Covers the `None` arm of the back-fill `origin |> Option.iter` in getConfigParam:
    // a cache hit at an ancestor whose config did not come from any fsharplint.json.
    let parentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    let childDir = Path.Combine(parentDir, "src")
    Directory.CreateDirectory(childDir) |> ignore

    try
        configCache.Clear()
        configOrigins.Clear()

        let parentResult, _ = getConfigParam (Path.Combine(parentDir, "Test.fs"))
        test <@ configCache.ContainsKey parentDir @>
        test <@ not (configCache.ContainsKey childDir) @>

        let childResult, childDiagnostics =
            getConfigParam (Path.Combine(childDir, "Child.fs"))

        test <@ configCache.ContainsKey childDir @>
        test <@ not (configOrigins.ContainsKey childDir) @>
        test <@ obj.ReferenceEquals(parentResult, childResult) @>
        test <@ childDiagnostics |> List.isEmpty @>
    finally
        configCache.Clear()
        configOrigins.Clear()
        Directory.Delete(parentDir, true)

[<Fact>]
let ``An explicit null ignoreFiles parses as absent and ignores nothing`` () =
    // isIgnoredFile maps over the `Some globs` array; this pins that FSharpLint's parser
    // never yields `Some null`, so a JSON null cannot reach that map as a null array.
    let parsed =
        FSharpLint.Framework.Configuration.parseConfig """{ "ignoreFiles": null }"""

    test <@ parsed.ignoreFiles = None @>

    test
        <@
            not (
                isIgnoredFile
                    (Lint.ConfigurationParam.Configuration parsed)
                    (Path.Combine(Path.GetTempPath(), "Any.fs"))
            )
        @>
