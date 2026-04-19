module SampleProject.Rules.Smells

// --- FailwithWithSingleArgument (FL0016): failwith takes exactly one string ---
// Note: the rule fires when failwith is used "unnecessarily" in specific shapes.
// A common trigger: failwith called with "" (already a single arg, so this rule is
// named oddly — it actually warns when you pass empty/single arg that could be a cleaner shape).
let f16 () = failwith "" // EXPECT: FL0016

// --- RaiseWithSingleArgument (FL0017): raise with non-exception ---
open System
let f17 () = raise (Exception "boom") // EXPECT: FL0017

// --- NullArgWithSingleArgument (FL0018) ---
let f18 (x: obj) = nullArg "" // EXPECT: FL0018

// --- InvalidOpWithSingleArgument (FL0019) ---
let f19 () = invalidOp "" // EXPECT: FL0019

// --- InvalidArgWithTwoArguments (FL0020) ---
let f20 () = invalidArg "x" "" // EXPECT: FL0020

// --- FailwithfWithArgumentsMatchingFormatString (FL0021) ---
let f21 x = failwithf "value was %d" x // EXPECT: FL0021

// --- FailwithBadUsage (FL0072) ---
let f72 () = failwith null // EXPECT: FL0072

// --- NoPartialFunctions (FL0066) ---
let f66 (xs: int list) = List.head xs // EXPECT: FL0066

// --- DiscourageStringInterpolationWithStringFormat (FL0093) ---
let f93 (x: obj) =
    let s = System.String.Format("{0}", x)
    $"{s}" // EXPECT: FL0093
