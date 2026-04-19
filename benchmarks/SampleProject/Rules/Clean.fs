module SampleProject.Rules.Clean


// This file intentionally contains NO rule violations. The coverage test asserts
// that no warnings fire against this file under the sample project's fsharplint.json.


type Point = { PositionX: double; PositionY: double }


type Shape =
    | Circle of radius: double
    | Square of side: double


let computeArea shape =
    match shape with
    | Circle radius -> System.Math.PI * radius * radius
    | Square side -> side * side


let addPoints left right =
    { PositionX = left.PositionX + right.PositionX
      PositionY = left.PositionY + right.PositionY }


let zeroPoint = { PositionX = 0.0; PositionY = 0.0 }


let sumAreas shapes = shapes |> List.sumBy computeArea