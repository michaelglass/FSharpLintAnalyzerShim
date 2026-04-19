module SampleProject.Rules.SizeLimits

// --- MaxLinesInLambdaFunction (FL0022): maxLines=5 ---
let f22 =
    fun x -> // EXPECT: FL0022
        let a = x + 1
        let b = a + 1
        let c = b + 1
        let d = c + 1
        let e = d + 1
        e

// --- MaxLinesInMatchLambdaFunction (FL0023): maxLines=10 ---
let f23 =
    function // EXPECT: FL0023
    | 0 -> 0
    | 1 -> 1
    | 2 -> 2
    | 3 -> 3
    | 4 -> 4
    | 5 -> 5
    | 6 -> 6
    | 7 -> 7
    | 8 -> 8
    | _ -> -1

// --- MaxLinesInValue (FL0024): maxLines=10 ---
let f24 = // EXPECT: FL0024
    let a = 1
    let b = 2
    let c = 3
    let d = 4
    let e = 5
    let f = 6
    let g = 7
    let h = 8
    let i = 9
    let j = 10
    a + b + c + d + e + f + g + h + i + j

// --- MaxLinesInFunction (FL0025): maxLines=15 ---
let f25 x = // EXPECT: FL0025
    let a = x + 1
    let b = a + 1
    let c = b + 1
    let d = c + 1
    let e = d + 1
    let f = e + 1
    let g = f + 1
    let h = g + 1
    let i = h + 1
    let j = i + 1
    let k = j + 1
    let l = k + 1
    let m = l + 1
    let n = m + 1
    n

// --- MaxLinesInConstructor (FL0027) + MaxLinesInMember (FL0026) ---
type T26() = // EXPECT: FL0027
    do
        let a = 1
        let b = 2
        let c = 3
        let d = 4
        let e = 5
        let f = 6
        let g = 7
        let h = 8
        let i = 9
        let j = 10
        printfn "%d" (a + b + c + d + e + f + g + h + i + j)

    member _.BigMember() = // EXPECT: FL0026
        let a = 1
        let b = 2
        let c = 3
        let d = 4
        let e = 5
        let f = 6
        let g = 7
        let h = 8
        let i = 9
        let j = 10
        let k = 11
        let l = 12
        let m = 13
        let n = 14
        let o = 15
        a + b + c + d + e + f + g + h + i + j + k + l + m + n + o

// --- MaxLinesInProperty (FL0028): maxLines=10 ---
type T28() =
    member _.BigProperty = // EXPECT: FL0028
        let a = 1
        let b = 2
        let c = 3
        let d = 4
        let e = 5
        let f = 6
        let g = 7
        let h = 8
        let i = 9
        let j = 10
        a + b + c + d + e + f + g + h + i + j

// --- MaxLinesInRecord (FL0030): maxLines=10 ---
type BigRecord = // EXPECT: FL0030
    { F1: int
      F2: int
      F3: int
      F4: int
      F5: int
      F6: int
      F7: int
      F8: int
      F9: int
      F10: int
      F11: int }

// --- MaxLinesInEnum (FL0031): maxLines=10 ---
type BigEnum = // EXPECT: FL0031
    | E1 = 1
    | E2 = 2
    | E3 = 3
    | E4 = 4
    | E5 = 5
    | E6 = 6
    | E7 = 7
    | E8 = 8
    | E9 = 9
    | E10 = 10
    | E11 = 11

// --- MaxLinesInUnion (FL0032): maxLines=10 ---
type BigUnion = // EXPECT: FL0032
    | U1
    | U2
    | U3
    | U4
    | U5
    | U6
    | U7
    | U8
    | U9
    | U10
    | U11

// --- MaxNumberOfItemsInTuple (FL0051): maxItems=3 ---
let f51: int * int * int * int = (1, 2, 3, 4) // EXPECT: FL0051

// --- MaxNumberOfFunctionParameters (FL0052): maxItems=4 ---
let f52 a b c d e = a + b + c + d + e // EXPECT: FL0052

// --- MaxNumberOfMembers (FL0053): maxItems=4 ---
type T53() = // EXPECT: FL0053
    member _.M1() = 1
    member _.M2() = 2
    member _.M3() = 3
    member _.M4() = 4
    member _.M5() = 5

// --- MaxNumberOfBooleanOperatorsInCondition (FL0054): maxItems=3 ---
let f54 a b c d e = if a && b && c && d && e then 1 else 0 // EXPECT: FL0054

// --- CyclomaticComplexity (FL0071): maxComplexity=3 ---
let f71 x = // EXPECT: FL0071
    if x = 1 then 1
    elif x = 2 then 2
    elif x = 3 then 3
    elif x = 4 then 4
    else 0
