let map = List.map

let internal inc (n:int) : int list =
    let rec inc' cur total =
        match cur with
        | _ when cur = total -> []
        | _ -> cur :: (inc' (cur + 1) total)
    inc' 0 n
let internal inc1 n = inc n |> map ((+) 1)
let internal chars n =
    (inc n)
    |> map (fun i -> char (97 + i))
    |> map (fun c -> new string([|c|]))

let internal printFunc n f =
    inc1 n
    |> map (fun i -> f i <| chars i)

let _ = printFunc 8 <| fun i cs ->
    printfn
        "let forAll%i %s f' = Prop.forAll (map%i %s) (uncurry%i f')"
        i
        (String.concat " " cs)
        i
        (String.concat " " cs)
        i
