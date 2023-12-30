module TestProject1.Utils.FsCheckUtils

open Curry
open FsCheck

let gen2 a b = gen {
    let! a' = a
    let! b' = b
    return (a', b')
}
let gen3 a b c = gen {
    let! a' = a
    let! b' = b
    let! c' = c
    return (a', b', c')
}
let gen4 a b c d = gen {
    let! a' = a
    let! b' = b
    let! c' = c
    let! d' = d
    return (a', b', c', d')
}
let gen5 a b c d e = gen {
    let! a' = a
    let! b' = b
    let! c' = c
    let! d' = d
    let! e' = e
    return (a', b', c', d', e')
}
let gen6 a b c d e f = gen {
    let! a' = a
    let! b' = b
    let! c' = c
    let! d' = d
    let! e' = e
    let! f' = f
    return (a', b', c', d', e', f')
}
let gen7 a b c d e f g = gen {
    let! a' = a
    let! b' = b
    let! c' = c
    let! d' = d
    let! e' = e
    let! f' = f
    let! g' = g
    return (a', b', c', d', e', f', g')
}
let gen8 a b c d e f g h = gen {
    let! a' = a
    let! b' = b
    let! c' = c
    let! d' = d
    let! e' = e
    let! f' = f
    let! g' = g
    let! h' = h
    return (a', b', c', d', e', f', g', h')
}

let map2 a b =
    gen2
        (Arb.toGen a)
        (Arb.toGen b)
    |> Arb.fromGen
let map3 a b c =
    gen3
        (Arb.toGen a)
        (Arb.toGen b)
        (Arb.toGen c)
    |> Arb.fromGen
let map4 a b c d =
    gen4
        (Arb.toGen a)
        (Arb.toGen b)
        (Arb.toGen c)
        (Arb.toGen d)
    |> Arb.fromGen
let map5 a b c d e =
    gen5
        (Arb.toGen a)
        (Arb.toGen b)
        (Arb.toGen c)
        (Arb.toGen d)
        (Arb.toGen e)
    |> Arb.fromGen
let map6 a b c d e f =
    gen6
        (Arb.toGen a)
        (Arb.toGen b)
        (Arb.toGen c)
        (Arb.toGen d)
        (Arb.toGen e)
        (Arb.toGen f)
    |> Arb.fromGen
let map7 a b c d e f g =
    gen7
        (Arb.toGen a)
        (Arb.toGen b)
        (Arb.toGen c)
        (Arb.toGen d)
        (Arb.toGen e)
        (Arb.toGen f)
        (Arb.toGen g)
    |> Arb.fromGen
let map8 a b c d e f g h =
    gen8
        (Arb.toGen a)
        (Arb.toGen b)
        (Arb.toGen c)
        (Arb.toGen d)
        (Arb.toGen e)
        (Arb.toGen f)
        (Arb.toGen g)
        (Arb.toGen h)
    |> Arb.fromGen

let forAll2 a b f' = Prop.forAll (map2 a b) (uncurry2 f')
let forAll3 a b c f' = Prop.forAll (map3 a b c) (uncurry3 f')
let forAll4 a b c d f' = Prop.forAll (map4 a b c d) (uncurry4 f')
let forAll5 a b c d e f' = Prop.forAll (map5 a b c d e) (uncurry5 f')
let forAll6 a b c d e f f' = Prop.forAll (map6 a b c d e f) (uncurry6 f')
let forAll7 a b c d e f g f' = Prop.forAll (map7 a b c d e f g) (uncurry7 f')
let forAll8 a b c d e f g h f' = Prop.forAll (map8 a b c d e f g h) (uncurry8 f')
