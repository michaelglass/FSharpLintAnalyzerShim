module FSharpLintAnalyzerShim.Tests.CoverageConfigurationTests

open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open Xunit
open Swensen.Unquote

[<Fact>]
let ``Coverage excludes vendored FSharpLint by module independently of source path`` () =
    let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
    let settings = XDocument.Load(Path.Combine(repositoryRoot, "coverage-settings.xml"))

    let excludedModulePatterns =
        settings
            .Descendants(XName.Get "ModulePaths")
            .Descendants(XName.Get "Exclude")
            .Descendants(XName.Get "ModulePath")
        |> Seq.map _.Value
        |> Seq.toList

    test
        <@
            excludedModulePatterns
            |> List.exists (fun pattern -> Regex.IsMatch("/tmp/FSharpLint.Core.dll", pattern))
        @>

    test
        <@
            excludedModulePatterns
            |> List.forall (fun pattern -> not (Regex.IsMatch("/tmp/FSharpLintAnalyzerShim.dll", pattern)))
        @>
