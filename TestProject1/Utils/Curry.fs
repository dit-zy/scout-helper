module TestProject1.Utils.Curry

let curry f' a b = f' (a, b)
let curry2 f' a b = f' (a, b)
let curry3 f' a b c = f' (a, b, c)
let curry4 f' a b c d = f' (a, b, c, d)
let curry5 f' a b c d e = f' (a, b, c, d, e)
let curry6 f' a b c d e f = f' (a, b, c, d, e, f)
let curry7 f' a b c d e f g = f' (a, b, c, d, e, f, g)
let curry8 f' a b c d e f g h = f' (a, b, c, d, e, f, g, h)

let uncurry f' (a, b) = f' a b
let uncurry2 f' (a, b) = f' a b
let uncurry3 f' (a, b, c) = f' a b c
let uncurry4 f' (a, b, c, d) = f' a b c d
let uncurry5 f' (a, b, c, d, e) = f' a b c d e
let uncurry6 f' (a, b, c, d, e, f) = f' a b c d e f
let uncurry7 f' (a, b, c, d, e, f, g) = f' a b c d e f g
let uncurry8 f' (a, b, c, d, e, f, g, h) = f' a b c d e f g h
