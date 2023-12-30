module TestProject1.UtilsTests

open 

open Xunit
open FsCheck

[<Fact>]
let ``My test`` () =
    Prop.forAll