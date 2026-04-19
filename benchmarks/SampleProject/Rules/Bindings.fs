module SampleProject.Rules.Bindings

// --- FavourIgnoreOverLetWild (FL0055) ---
let badWild () =
    let _ = printfn "hello" // EXPECT: FL0055
    ()

let goodIgnore () =
    printfn "hello" |> ignore
    ()

// --- WildcardNamedWithAsPattern (FL0056) ---
let f56 x =
    match x with
    | _ as y -> y // EXPECT: FL0056

let f56good x =
    match x with
    | y -> y

// --- UselessBinding (FL0057) ---
let f57 () =
    let x = 1
    let x = x // EXPECT: FL0057
    x

// --- TupleOfWildcards (FL0058) ---
type T58 =
    | Case of int * int * int

let f58 v =
    match v with
    | Case (_, _, _) -> 0 // EXPECT: FL0058

// --- FavourTypedIgnore (FL0070) ---
let f70 (x: int) =
    ignore x // EXPECT: FL0070
    ()

let f70good (x: int) =
    ignore<int> x
    ()

// --- FavourReRaise (FL0073) ---
let f73 () =
    try
        failwith "boom"
    with ex ->
        raise ex // EXPECT: FL0073

let f73good () =
    try
        failwith "boom"
    with _ ->
        reraise ()

// --- UsedUnderscorePrefixedElements (FL0082) ---
let f82 () =
    let _x = 5
    _x + 1 // EXPECT: FL0082

// --- UnneededRecKeyword (FL0083) ---
let rec notRec x = x + 1 // EXPECT: FL0083

let rec actuallyRec x =
    if x <= 0 then 0 else actuallyRec (x - 1)

// --- DisallowShadowing (FL0092) ---
let f92 () =
    let x = 1
    let y =
        let x = 2 // EXPECT: FL0092
        x + 1
    x + y
