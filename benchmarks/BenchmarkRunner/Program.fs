module FSharpLintAnalyzerShim.Benchmarks.Program

open System
open System.IO
open FSharpLint.Application

/// Walk up from a source file looking for fsharplint.json. Matches the shim's discovery semantics.
let private findConfig (startFile: string) : Lint.ConfigurationParam =
    let rec walkUp (d: string) =
        let candidate = Path.Combine(d, "fsharplint.json")
        if File.Exists candidate then
            Lint.ConfigurationParam.FromFile candidate
        else
            let parent = Directory.GetParent d
            if isNull parent then Lint.ConfigurationParam.Default else walkUp parent.FullName

    walkUp (Path.GetDirectoryName startFile)

[<EntryPoint>]
let main argv =
    match argv with
    | [| targetPath |] ->
        let fullPath = Path.GetFullPath targetPath
        let targetDir = Path.GetDirectoryName fullPath
        let toolsPath = Ionide.ProjInfo.Init.init (DirectoryInfo targetDir) None
        let anySource = Directory.GetFiles(targetDir, "*.fs", SearchOption.AllDirectories) |> Array.head
        let opts =
            { Lint.OptionalLintParameters.Default with Configuration = findConfig anySource }

        let result =
            match Path.GetExtension(fullPath).ToLowerInvariant() with
            | ".slnx"
            | ".sln"
            | ".slnf" -> Lint.lintSolution opts fullPath toolsPath
            | _ -> Lint.lintProject opts fullPath toolsPath

        match result with
        | LintResult.Success warnings ->
            printfn "Warnings: %d" (List.length warnings)
            0
        | LintResult.Failure failure ->
            eprintfn "Lint failed: %s" failure.Description
            1
    | _ ->
        eprintfn "usage: BenchmarkRunner <path-to-fsproj-or-slnx>"
        64
