#I "bin"
#r "Numeric"

open Numeric
        
// Numeric overloadings for function types

[<Witness>]
type FloatingFunc<'a, 'b, 'c when 'c :> Floating<'b>> =
    interface Floating<'a -> 'b> with
        member plus a b         = fun x -> a x + b x
        member times a b        = fun x -> a x * b x
        member negate a         = Floating.negate << a
        member abs a            = Floating.abs << a
        member signum a         = Floating.signum << a
        member fromInteger a    = fun _ -> Floating.fromInteger a
        member recip a          = Floating.recip << a
        member pi               = fun _ -> Floating.pi
        member exp a            = Floating.exp << a
        member log a            = Floating.log << a
        member sqrt a           = Floating.sqrt << a
        member sin a            = Floating.sin << a
        member cos a            = Floating.cos << a
        member asin a           = Floating.asin << a
        member acos a           = Floating.acos << a
        member atan a           = Floating.atan << a
        member sinh a           = Floating.sinh << a
        member cosh a           = Floating.cosh << a
        member asinh a          = Floating.asinh << a
        member acosh a          = Floating.acosh << a
        member atanh a          = Floating.atanh << a

// Type of D

type D<'a> = D of 'a * 'a

let constD x = D (x, Floating.fromInteger 0)

let idD x = D (x, Floating.fromInteger 1)

let (|><|) f f' (D(a, a')) = D (f a, a' * f' a)
let (/></) f f' (D(a, a')) = D (f a, a' * f' a)

let sqr x = x * x

// Ugly FloatingD that has repeated uses of chain rule

(*[<Witness>]
type FloatingDUgly<'a, 'b when 'b :> Floating<'a>> =
    interface Floating<D<'a>> with
        member plus         (D(x,x')) (D(y,y')) = D (x + y, x' + y')
        member times        (D(x,x')) (D(y,y')) = D (x * y, y' * x + x' * y)
        member negate       (D(x,x'))           = D (-x, -x')
        member signum       (D(x,_))            = D (Floating.signum x, Floating.fromInteger 0)
        member abs          (D(x,x'))           = D (Floating.abs x, x' * Floating.signum x)
        member fromInteger  x                   = constD <| Floating.fromInteger x
        member recip        (D(x,x'))           = D (Floating.recip x, -x' / sqr x)
        member pi                               = constD Floating.pi
        member exp          (D(x,x'))           = D (Floating.exp x, x' * Floating.exp x)
        member log          (D(x,x'))           = D (Floating.log x, x' / x)
        member sqrt         (D(x,x'))           = D (Floating.sqrt x, x' / (Floating.fromInteger 2 * Floating.sqrt x))
        member sin          (D(x,x'))           = D (Floating.sin x, x' * Floating.cos x)
        member cos          (D(x,x'))           = D (Floating.cos x, -x' * Floating.sin x)
        member asin         (D(x,x'))           = D (Floating.asin x, x' / (Floating.sqrt <| Floating.fromInteger 1 - sqr x))
        member acos         (D(x,x'))           = D (Floating.acos x, x' / -(Floating.sqrt <| Floating.fromInteger 1 - sqr x))
        member atan         (D(x,x'))           = D (Floating.atan x, x' / Floating.fromInteger 1 + sqr x)
        member sinh         (D(x,x'))           = D (Floating.sinh x, x' * Floating.cosh x)
        member cosh         (D(x,x'))           = D (Floating.cosh x, x' * Floating.sinh x)
        member asinh        (D(x,x'))           = D (Floating.asinh x, x' / (Floating.sqrt <| Floating.fromInteger 1 + sqr x))
        member acosh        (D(x,x'))           = D (Floating.acosh x, x' / -(Floating.sqrt <| sqr x - Floating.fromInteger 1))
        member atanh        (D(x,x'))           = D (Floating.atanh x, x' / Floating.fromInteger 1 - sqr x)*)

// FloatingD with /></ to generalise over scalar chain rule

[<Witness>]
type FloatingD<'a, 'b, 'c when 'b :> Floating<'a> and 'c :> Floating<'a -> 'a>> =
    interface Floating<D<'a>> with
        member fromInteger  x                   = constD <| Floating.fromInteger x
        member pi                               = constD Floating.pi
        member plus         (D(x,x')) (D(y,y')) = D (x + y, x' + y')
        member times        (D(x,x')) (D(y,y')) = D (x * y, y' * x + x' * y)
        member negate       x                   = x |> Floating.negate      /></    Floating.negate (Floating.fromInteger 1)
        member abs          x                   = x |> Floating.abs         /></    Floating.signum
        member signum       x                   = x |> Floating.signum      /></    Floating.fromInteger 0
        member recip        x                   = x |> Floating.recip       /></    -(sqr << Floating.recip)
        member exp          x                   = x |> Floating.exp         /></    Floating.exp
        member log          x                   = x |> Floating.log         /></    Floating.recip
        member sqrt         x                   = x |> Floating.sqrt        /></    (Floating.recip << Floating.fromInteger 2 * Floating.sqrt)
        member sin          x                   = x |> Floating.sin         /></    Floating.cos
        member cos          x                   = x |> Floating.cos         /></    -Floating.sin
        member asin         x                   = x |> Floating.asin        /></    (Floating.recip << Floating.sqrt << Floating.fromInteger 1 - sqr)
        member acos         x                   = x |> Floating.acos        /></    (Floating.recip << -Floating.sqrt << Floating.fromInteger 1 - sqr)
        member atan         x                   = x |> Floating.atan        /></    (Floating.recip << Floating.fromInteger 1 + sqr)
        member sinh         x                   = x |> Floating.sinh        /></    Floating.cosh
        member cosh         x                   = x |> Floating.cosh        /></    Floating.sinh
        member asinh        x                   = x |> Floating.asinh       /></    (Floating.recip << Floating.sqrt << Floating.fromInteger 1 + sqr)
        member acosh        x                   = x |> Floating.acosh       /></    (Floating.recip << -Floating.sqrt << sqr - Floating.fromInteger 1)
        member atanh        x                   = x |> Floating.atanh       /></    (Floating.recip << Floating.fromInteger 1 - sqr)

let timesThree x = x + x + x

let cube x = x * x * x

let f1 x = Fractional.fromInteger 16 / x

let f2 x = Fractional.recip x

let f3 x = Floating.sqrt <| Floating.fromInteger 3 * Floating.sin x

timesThree 2            |> printf "%d\n"
constD 1                |> printf "%A\n"
idD 1                   |> printf "%A\n"
timesThree (D(2.,1.))   |> printf "%A\n"
cube (D(2.,1.))         |> printf "%A\n"
f1 (D(2.,1.))           |> printf "%A\n"
f2 (D(2.,1.))           |> printf "%A\n"
f3 (D(2.,1.))           |> printf "%A\n"