module SampleProject.Rules.Hints

// --- Hints (FL0065): a single rule code with many patterns ---
let xs = [ 1; 2; 3 ]

let h1 a b = not (a = b) // EXPECT: FL0065
let h2 = List.head (List.sort xs) // EXPECT: FL0065
let h3 = List.map (fun x -> x + 1) (List.map (fun x -> x * 2) xs) // EXPECT: FL0065
let h4 = List.rev (List.rev xs) // EXPECT: FL0065
let h5 = (List.length xs) = 0 // EXPECT: FL0065
let h6 = xs = [] // EXPECT: FL0065
let h7 a = if a then true else false // EXPECT: FL0065
let h8 = fun x -> x // EXPECT: FL0065
let h9 (x: obj) = x = null // EXPECT: FL0065

// --- Good versions (should not fire FL0065) ---
let g1 a b = a <> b
let g2 = List.min xs
let g3 = xs |> List.map ((*) 2 >> (+) 1)
let g4 = xs
let g5 = List.isEmpty xs
let g6 = List.isEmpty xs
let g7 a = a
let g8 = id
let g9 (x: obj) = isNull x
