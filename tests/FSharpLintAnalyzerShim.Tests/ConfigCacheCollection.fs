namespace FSharpLintAnalyzerShim.Tests

open Xunit

/// xunit collection serializing every test module that mutates the shim's
/// module-level configCache (Clear() + cached-entry assertions). xunit.v3 runs
/// distinct collections in parallel by default, so without this barrier a
/// Clear() in one module races the cache assertions in another.
[<CollectionDefinition("ConfigCache")>]
type ConfigCacheCollection = class end
