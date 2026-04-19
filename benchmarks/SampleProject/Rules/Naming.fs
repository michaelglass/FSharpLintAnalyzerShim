module SampleProject.Rules.Naming

// --- Interface names (FL0036): must PascalCase + start with I ---
type iMyInterface = // EXPECT: FL0036
    abstract member DoThing: unit -> unit

type IGoodInterface =
    abstract member DoThing: unit -> unit

// --- Exception names (FL0037): must PascalCase + end with Exception ---
exception BadName of string // EXPECT: FL0037
exception GoodException of string

// --- Type names (FL0038): must PascalCase ---
type badTypeName() = // EXPECT: FL0038
    member _.X = 1

type GoodTypeName() =
    member _.X = 1

// --- Record field names (FL0039): must PascalCase per config ---
type AllBadRecord =
    { badField: int // EXPECT: FL0039
      anotherBad: string } // EXPECT: FL0039

type GoodRecord = { GoodField: int; Another: string }

// --- Enum cases (FL0040): must PascalCase ---
type MyEnum =
    | lowerCaseCase = 1 // EXPECT: FL0040
    | AnotherCase = 2

// --- Union cases (FL0041): must PascalCase (no underscores per config) ---
type MyUnion =
    | Bad_Variant // EXPECT: FL0041
    | GoodVariant of int

// --- Module names (FL0042): must PascalCase ---
module badModule = // EXPECT: FL0042
    let x = 1

module GoodModule =
    let x = 1

// --- Literal names (FL0043): must PascalCase ---
[<Literal>]
let badLiteralName = 42 // EXPECT: FL0043

[<Literal>]
let GoodLiteralName = 42

// --- Namespace names (FL0044): tested in file opener via rawIdent below ---

// --- Member names (FL0045): must PascalCase (AllowPrefix) ---
type ClassWithMembers() =
    member _.badMemberName() = () // EXPECT: FL0045
    member _.GoodMemberName() = ()

// --- Parameter names (FL0046): must CamelCase ---
let funcWithBadParam BadParam = BadParam + 1 // EXPECT: FL0046
let funcWithGoodParam goodParam = goodParam + 1

// --- Measure type names (FL0047): no underscores ---
[<Measure>]
type bad_measure // EXPECT: FL0047

[<Measure>]
type GoodMeasure

// --- Active pattern names (FL0048): must PascalCase ---
let (|Has_Underscore|_|) input = if input > 0 then Some input else None // EXPECT: FL0048
let (|GoodPat|_|) input = if input > 0 then Some input else None

// --- Public values names (FL0049): must CamelCase ---
let BadPublicName = 1 // EXPECT: FL0049
let goodPublicName = 1

// --- Private values (FL0067): CamelCase ---
module private Private =
    let BadPrivateName = 1 // EXPECT: FL0067
    let goodPrivateName = 1

// --- Internal values (FL0068): CamelCase ---
module internal Internal =
    let BadInternalName = 1 // EXPECT: FL0068
    let goodInternalName = 1

// --- Generic types names (FL0069): must PascalCase (type parameter starts with upper) ---
let genericFn<'badTypeParam> (x: 'badTypeParam) = x // EXPECT: FL0069
let genericFnGood<'T> (x: 'T) = x

// --- AvoidTooShortNames (FL0075): single-char binding names ---
let z = 5 // EXPECT: FL0075

// --- Unnested function names (FL0080): CamelCase top-level functions ---
let BadTopLevelFunc x = x + 1 // EXPECT: FL0080
let goodTopLevelFunc x = x + 1

// --- Nested function names (FL0081): CamelCase nested functions ---
let outer1 () =
    let NestedBad x = x + 1 // EXPECT: FL0081
    let nestedGood x = x + 1
    NestedBad 0 + nestedGood 0
