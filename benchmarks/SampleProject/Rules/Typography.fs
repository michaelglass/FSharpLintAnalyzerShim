module SampleProject.Rules.Typography

// --- TypedItemSpacing (FL0010): "SpaceAfter" config ---
let f10 (x:int) = x + 1 // EXPECT: FL0010

let f10good (x: int) = x + 1

// --- TypePrefixing (FL0011): Hybrid mode prefers T[] over T array for built-ins? ---
let f11: list<int> = [ 1; 2; 3 ] // EXPECT: FL0011

// --- MaxCharactersOnLine (FL0060): maxCharactersOnLine=100 ---
let f60 = "This line is intentionally way too long to exceed the configured limit of one hundred characters per line for sure yep" // EXPECT: FL0060

// --- NoTabCharacters (FL0064): the F# compiler itself rejects tabs, so triggering
// this rule inside a compileable project is not possible without a custom build
// setup. Covered by FSharpLint's own test suite; not asserted here. ---
