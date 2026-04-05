module FSharpLintAnalyzerShim.LintAnalyzer

open System.Collections.Concurrent
open System.IO
open System.Runtime.CompilerServices
open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharpLint.Application
open FSharpLint.Framework.Suggestion

[<assembly: InternalsVisibleTo("FSharpLintAnalyzerShim.Tests")>]
do ()

/// Cache loaded configs by directory to avoid re-reading fsharplint.json per file.
let internal configCache = ConcurrentDictionary<string, ConfigurationParam>()

/// Walk up from filePath looking for fsharplint.json, cache parsed config per directory.
/// Parses eagerly so lintParsedFile doesn't re-read the JSON on every file.
let internal getConfigParam (filePath: string) : ConfigurationParam =
    let dir = Path.GetDirectoryName(Path.GetFullPath(filePath))

    configCache.GetOrAdd(
        dir,
        fun dir ->
            let rec walkUp (d: string) =
                let candidate = Path.Combine(d, "fsharplint.json")

                if File.Exists(candidate) then
                    match Lint.getConfig (ConfigurationParam.FromFile candidate) with
                    | Ok config -> ConfigurationParam.Configuration config
                    | Error _ -> ConfigurationParam.FromFile candidate
                else
                    let parent = Directory.GetParent(d)

                    if isNull parent then
                        match Lint.getConfig ConfigurationParam.Default with
                        | Ok config -> ConfigurationParam.Configuration config
                        | Error _ -> ConfigurationParam.Default
                    else
                        walkUp parent.FullName

            walkUp dir
    )

/// Map a FSharpLint SuggestedFix to an Analyzer SDK Fix.
let internal mapFix (suggestedFix: SuggestedFix) : Fix =
    { FromRange = suggestedFix.FromRange
      FromText = suggestedFix.FromText
      ToText = suggestedFix.ToText }

/// Map a FSharpLint LintWarning to an Analyzer SDK Message.
let internal mapWarning (warning: LintWarning) : Message =
    let fixes =
        match warning.Details.SuggestedFix with
        | Some lazySuggestion ->
            match lazySuggestion.Value with
            | Some fix -> [ mapFix fix ]
            | None -> []
        | None -> []

    { Type = warning.RuleName
      Message = warning.Details.Message
      Code = warning.RuleIdentifier
      Severity = Severity.Warning
      Range = warning.Details.Range
      Fixes = fixes }

let private makeErrorMessage (typeName: string) (message: string) : Message =
    { Type = typeName
      Message = message
      Code = "FL0000"
      Severity = Severity.Info
      Range = Range.range0
      Fixes = [] }

[<CliAnalyzer("FSharpLint", "All FSharpLint rules via FSharp.Analyzers.SDK")>]
let lintAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async {
            let configParam = getConfigParam context.FileName

            let parsedFileInfo: Lint.ParsedFileInformation =
                { Ast = context.ParseFileResults.ParseTree
                  Source = context.SourceText.ToString()
                  TypeCheckResults = Some context.CheckFileResults
                  ProjectCheckResults = Some context.CheckProjectResults }

            let mutable lintException: exn option = None

            let optionalParams =
                { Lint.OptionalLintParameters.Default with
                    Configuration = configParam
                    ReportLinterProgress =
                        Some(fun progress ->
                            match progress with
                            | Lint.ProjectProgress.Failed(_, ex) -> lintException <- Some ex
                            | _ -> ()) }

            match Lint.lintParsedFile optionalParams parsedFileInfo context.FileName with
            | LintResult.Success warnings ->
                match lintException with
                | Some ex ->
                    // lintParsedFile swallows exceptions via ReportLinterProgress(Failed);
                    // surface them so they're not silently lost.
                    return
                        (warnings |> List.map mapWarning)
                        @ [ makeErrorMessage
                                "FSharpLint.InternalError"
                                $"FSharpLint internal error: {ex.GetType().Name}: {ex.Message}" ]
                | None ->
                    return warnings |> List.map mapWarning
            | LintResult.Failure failure ->
                return [ makeErrorMessage "FSharpLint.Error" failure.Description ]
        }
