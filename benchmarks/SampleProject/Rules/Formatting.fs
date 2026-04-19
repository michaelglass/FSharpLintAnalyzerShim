module SampleProject.Rules.Formatting

// --- TupleCommaSpacing (FL0001) ---
let f1 = (1,2) // EXPECT: FL0001
let f1good = (1, 2)

// --- PatternMatchClausesOnNewLine (FL0004) ---
let f4 x =
    match x with
    | 0 -> 0 | 1 -> 1 | _ -> -1 // EXPECT: FL0004

let f4good x =
    match x with
    | 0 -> 0
    | 1 -> 1
    | _ -> -1

// --- ModuleDeclSpacing (FL0008) ---
module M8 =
    let a = 1
    let b = 2 // EXPECT: FL0008
