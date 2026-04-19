module SampleProject.Rules.Conventions

// --- RecursiveAsyncFunction (FL0013) ---
let rec f13 () = async { // EXPECT: FL0013
    do! f13 ()
    return ()
}

// --- RedundantNewKeyword (FL0014) ---
open System.IO
let s14 = new MemoryStream() // EXPECT: FL0014
s14.Dispose()

// --- NestedStatements (FL0015): depth > 4 per config ---
let f15 x =
    if x > 0 then // EXPECT: FL0015
        if x > 1 then
            if x > 2 then
                if x > 3 then
                    if x > 4 then
                        x
                    else 0
                else 0
            else 0
        else 0
    else 0

// --- ReimplementsFunction (FL0034) ---
let f34 x y = if x > y then x else y // EXPECT: FL0034

// --- CanBeReplacedWithComposition (FL0035) ---
let f35 a b x = a (b x) // EXPECT: FL0035

// --- AvoidSinglePipeOperator (FL0077) ---
let f77 xs = xs |> List.length // EXPECT: FL0077

// --- FavourStaticEmptyFields (FL0076) ---
let f76a = [] // EXPECT: FL0076
let f76b = [||] // EXPECT: FL0076

// --- FavourAsKeyword (FL0086) ---
let f86 v =
    match v with
    | Some x when x > 0 -> Some x // EXPECT: FL0086
    | _ -> None

// --- InterpolatedStringWithNoSubstitution (FL0087) ---
let f87 = $"plain string no subs" // EXPECT: FL0087

// --- IndexerAccessorStyleConsistency (FL0088): OCaml style requires .[i] ---
let f88 () =
    let arr = [| 1; 2; 3 |]
    arr.[0] + arr[1] // EXPECT: FL0088

// --- FavourSingleton (FL0089) ---
let f89 = [ 1 ] // EXPECT: FL0089
let f89good = List.singleton 1

// --- DisallowShadowing covered in Bindings ---
