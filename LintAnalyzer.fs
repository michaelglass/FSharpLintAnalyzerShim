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

/// The FCS major.minor line this shim is compiled against (see fsproj FCS pin).
/// FSharpLint.Core's AST-typed surface is binary-coupled to this line, so an
/// analyzer host loading a different FCS minor version cannot run the shim.
let internal expectedFcsMajorMinor = (43, 12)

/// Pure check: given the FCS assembly version a host actually loaded, return the
/// diagnostics to surface when its major.minor differs from the line the shim was
/// built against (empty when compatible). A single `FL0000` Warning naming both
/// versions turns an otherwise-cryptic type-load / MissingMethod failure deep
/// inside linting into an actionable message. Kept pure so both branches are
/// unit-testable without loading a second FCS.
let internal fcsCompatibilityMessages (loaded: System.Version) : Message list =
    let expectedMajor, expectedMinor = expectedFcsMajorMinor

    if loaded.Major = expectedMajor && loaded.Minor = expectedMinor then
        []
    else
        let message =
            $"FSharpLintAnalyzerShim was built against FSharp.Compiler.Service "
            + $"{expectedMajor}.{expectedMinor}.x but the analyzer host loaded "
            + $"{loaded.Major}.{loaded.Minor}.{loaded.Build}. The shim's rule engine "
            + "is binary-coupled to its FCS line and cannot run under a different one. "
            + "Use an analyzer host on the FCS "
            + $"{expectedMajor}.{expectedMinor} line, or rebuild the shim against the host's FCS."

        [ { Type = "FSharpLint.HostIncompatible"
            Message = message
            Code = "FL0000"
            Severity = Severity.Warning
            Range = Range.range0
            Fixes = [] } ]

/// Cache loaded configs (plus any config-load diagnostics) by directory to avoid
/// re-reading fsharplint.json per file.
let internal configCache =
    ConcurrentDictionary<string, ConfigurationParam * Message list>()

/// Strip stack-trace frames from an exception string, keeping the type/message lines.
let private exceptionSummary (err: string) : string =
    err.Split('\n')
    |> Array.takeWhile (fun line -> not (line.TrimStart().StartsWith("at ")))
    |> Array.map (fun line -> line.Trim())
    |> String.concat " "

/// Diagnostic emitted when a discovered fsharplint.json cannot be parsed.
let private configErrorMessage (configPath: string) (err: string) : Message =
    { Type = "FSharpLint.ConfigError"
      Message =
        $"Failed to load lint configuration from {configPath}: {exceptionSummary err} "
        + "Linting continued with FSharpLint's default configuration."
      Code = "FL0000"
      Severity = Severity.Warning
      Range = Range.range0
      Fixes = [] }

/// FSharpLint's built-in default configuration, used when no fsharplint.json exists
/// in the hierarchy or the discovered one can't be parsed.
let private defaultConfigParam () : ConfigurationParam =
    match Lint.getConfig ConfigurationParam.Default with
    | Ok config -> ConfigurationParam.Configuration config
    | Error _ -> ConfigurationParam.Default

/// Walk up from filePath looking for fsharplint.json, cache parsed config per directory.
/// Parses eagerly so lintParsedFile doesn't re-read the JSON on every file.
/// Populates the cache for every directory along the walk path, so sibling files
/// at deeper levels don't each re-walk up to the same root.
/// A malformed config yields a clear FL0000 diagnostic naming the file (cached, so
/// every file under that directory surfaces it) plus the default configuration so
/// linting still proceeds.
let internal getConfigParam (filePath: string) : ConfigurationParam * Message list =
    let startDir = Path.GetDirectoryName(Path.GetFullPath(filePath))

    let rec walkUp (visited: string list) (d: string) : ConfigurationParam * Message list =
        match configCache.TryGetValue(d) with
        | true, cached ->
            for v in visited do
                configCache.TryAdd(v, cached) |> ignore

            cached
        | false, _ ->
            let candidate = Path.Combine(d, "fsharplint.json")

            let resolved =
                if File.Exists(candidate) then
                    match Lint.getConfig (ConfigurationParam.FromFile candidate) with
                    | Ok config -> Some(ConfigurationParam.Configuration config, [])
                    | Error err -> Some(defaultConfigParam (), [ configErrorMessage candidate err ])
                else
                    let parent = Directory.GetParent(d)

                    if isNull parent then
                        Some(defaultConfigParam (), [])
                    else
                        None

            match resolved with
            | Some r ->
                for v in d :: visited do
                    configCache.TryAdd(v, r) |> ignore

                r
            | None -> walkUp (d :: visited) (Directory.GetParent(d).FullName)

    walkUp [] startDir

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

/// Internal lint failures (LintResult.Failure and recovered exceptions) surface as
/// Warning, not Info: CI analyzer hosts suppress Info/Hint diagnostics, which would
/// silently hide a degraded lint run. Error is reserved by convention for
/// build-failing diagnostics, which a recoverable internal failure is not.
let private makeErrorMessage (typeName: string) (message: string) : Message =
    { Type = typeName
      Message = message
      Code = "FL0000"
      Severity = Severity.Warning
      Range = Range.range0
      Fixes = [] }

/// Analyzer body, parameterised over the FCS version the host actually loaded so
/// the host-incompatibility guard is unit-testable (a test can't load a second,
/// mismatching FCS into this process). `lintAnalyzer` supplies the live version.
let internal lintAnalyzerForFcs (loadedFcs: System.Version) : Analyzer<CliContext> =
    fun (context: CliContext) ->
        async {
            match fcsCompatibilityMessages loadedFcs with
            | (_ :: _) as hostIncompatible ->
                // The host loaded an FCS minor version the shim can't bind against.
                // Emit one clear diagnostic instead of crashing inside FSharpLint.
                return hostIncompatible
            | [] ->

                let configParam, configDiagnostics = getConfigParam context.FileName

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
                            configDiagnostics
                            @ (warnings |> List.map mapWarning)
                            @ [ makeErrorMessage
                                    "FSharpLint.InternalError"
                                    $"FSharpLint internal error: {ex.GetType().Name}: {ex.Message}" ]
                    | None -> return configDiagnostics @ (warnings |> List.map mapWarning)
                | LintResult.Failure failure ->
                    return configDiagnostics @ [ makeErrorMessage "FSharpLint.Error" failure.Description ]
        }

[<CliAnalyzer("FSharpLint", "All FSharpLint rules via FSharp.Analyzers.SDK")>]
let lintAnalyzer: Analyzer<CliContext> =
    lintAnalyzerForFcs (typeof<range>.Assembly.GetName().Version)
