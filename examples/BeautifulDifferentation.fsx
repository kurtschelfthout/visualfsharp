
// Num Typeclass

[<Trait>]
type Num<'a> =
    abstract plus : 'a -> 'a -> 'a
    abstract times : 'a -> 'a -> 'a
    abstract negate : 'a -> 'a
    abstract abs : 'a -> 'a
    abstract signum : 'a -> 'a
    abstract fromInteger : int -> 'a

[<Witness>]
type NumInt =
    interface Num<int> with
        member plus a b = a + b
        member times a b = a * b
        member negate a = -a
        member abs a = abs a
        member signum a =
            match a with
                | a when a > 0 -> 1
                | a when a = 0 -> 0
                | a -> -1
        member fromInteger a = a

[<Witness>]
type NumDouble =
    interface Num<double> with
        member plus a b = a + b
        member times a b = a * b
        member negate a = -a
        member abs a = abs a
        member signum a =
            match a with
                | a when a > 0. -> 1.
                | a when a = 0. -> 0.
                | a -> -1.
        member fromInteger a = double a

[<Trait>]
type Fractional<'a> =
    inherit Num<'a>
    abstract recip : 'a -> 'a
    //default div a b = a * recip b

[<Witness>]
type FractionalInt =
    interface Fractional<int> with
        member plus a b = a + b
        member times a b = a * b
        member negate a = -a
        member abs a = abs a
        member signum a =
            match a with
                | a when a > 0 -> 1
                | a when a = 0 -> 0
                | a -> -1
        member fromInteger a = a
        member recip a = 1 / a

[<Witness>]
type FractionalDouble =
    interface Fractional<double> with
        member plus a b = a + b
        member times a b = a * b
        member negate a = -a
        member abs a = abs a
        member signum a =
            match a with
                | a when a > 0. -> 1.
                | a when a = 0. -> 0.
                | a -> -1.
        member fromInteger a = double a
        member recip a = 1. / a

[<Trait>]
type Floating<'a> =
    inherit Fractional<'a>
    abstract pi : 'a
    abstract exp : 'a -> 'a
    abstract log : 'a -> 'a
    abstract sqrt : 'a -> 'a
    abstract sin : 'a -> 'a
    abstract cos : 'a -> 'a
    abstract asin : 'a -> 'a
    abstract acos : 'a -> 'a
    (*abstract atan : 'a -> 'a
    abstract sinh : 'a -> 'a
    abstract cosh : 'a -> 'a
    abstract asinh : 'a -> 'a
    abstract acosh : 'a -> 'a
    abstract atanh : 'a -> 'a*)

[<Witness>]
type FloatingDouble =
    interface Floating<double> with
        member plus a b = a + b
        member times a b = a * b
        member negate a = -a
        member abs a = abs a
        member signum a =
            match a with
                | a when a > 0. -> 1.
                | a when a = 0. -> 0.
                | a -> -1.
        member fromInteger a = double a
        member recip a = 1. / a
        member pi = System.Math.PI
        member exp a = Operators.exp a
        member log a = Operators.log a
        member sqrt a = Operators.sqrt a
        member sin a = Operators.sin a
        member cos a = Operators.cos a
        member asin a = Operators.asin a
        member acos a = Operators.acos a
        (*member atan a = Operators.atan a
        member sinh a = Operators.sinh a
        member cosh a = Operators.cosh a
        member asinh a = Operators.log (a + Operators.sqrt (1. + a * a))
        member acosh a = Operators.log (a + Operators.sqrt (a * a - 1.))
        member atanh a = 0.5 * (Operators.log (1. + a) - Operators.log (1. - a))*)

        
// Numeric overloadings for function types

let constFunc x _ = x

[<Witness>]
type FloatingFunc<'a,'b,'c when 'c :> Floating<'b>> =
    interface Floating<'a -> 'b> with
        member plus a b = fun x -> Floating.plus (a x) (b x)
        member times a b = fun x -> Floating.times (a x) (b x)
        member negate a = Floating.negate << a
        member abs a =  Floating.abs << a
        member signum a = Floating.signum << a
        member fromInteger a = constFunc (Floating.fromInteger a)
        member recip a = Floating.recip << a
        member pi = constFunc Floating.pi
        member exp a = Floating.exp << a
        member log a = Floating.log << a
        member sqrt a = Floating.sqrt << a
        member sin a = Floating.sin << a
        member cos a = Floating.cos << a
        member asin a = Floating.asin << a
        member acos a = Floating.acos << a
        (*member atan a = Floating.atan << a
        member sinh a = Floating.sinh << a
        member cosh a = Floating.cosh << a
        member asinh a = Floating.asinh << a
        member acosh a = Floating.acosh << a
        member atanh a = Floating.atanh << a*)

type D<'a> = D of 'a * 'a

let constD x = D(x, Floating.fromInteger 0)

let idD x = D(x, Floating.fromInteger 1)

let (|><|) f f' (D(a, a')) = D(f a, Floating.times a' (f' a))


//FloatingD with |><| to generalise over scalar chain rule

(*[<Witness>]
type FloatingD<'a, 'b, 'c when 'b :> Floating<'a> and 'c :> Floating<'a -> 'a>> =
    interface Floating<D<'a>> with
        member negate a = (Floating.negate |><| Floating.negate (Floating.fromInteger 1)) a*)

//Helper wrappers
let (+) = Num.plus
let (-) a b = Num.plus a (Num.negate b)
let (*) = Num.times

// Witnesses for type D 'a

[<Witness>]
type NumD<'a, 'b when 'b :> Num<'a>> =
    interface Num<D<'a>> with
        member plus (D(x,x')) (D(y,y')) = D(x + y, x' + y')
        member times (D(x,x')) (D(y,y')) = D(x * y, (y' * x) + (x' * y))
        member negate (D(x,x')) = D(Num.negate x, Num.negate x')
        member signum (D(x,_)) = D(Num.signum x, Num.fromInteger 0)
        member abs (D(x,x')) = D(Num.abs x, Num.times x' (Num.signum x))
        member fromInteger x = constD (Num.fromInteger x)

let sqr (x : 'T when 'U :> Num<'T>) =
    trait<'U>.times x x

[<Witness>]
type FractionalD<'a, 'b when 'b :> Fractional<'a>> =
    interface Fractional<D<'a>> with
        member plus (D(x,x')) (D(y,y')) = D(Fractional.plus x y, Fractional.plus x' y')
        member times (D(x,x')) (D(y,y')) = D(Fractional.times x y, Fractional.plus (Fractional.times y' x) (Fractional.times x' y))
        member negate (D(x,x')) = D(Fractional.negate x, Fractional.negate x')
        member signum (D(x,_)) = D(Fractional.signum x, Fractional.fromInteger 0)
        member abs (D(x,x')) = D(Fractional.abs x, Fractional.times x' (Fractional.signum x))
        member fromInteger x = constD (Fractional.fromInteger x)
        member recip (D(x,x')) = D(Fractional.recip x, Fractional.negate (Fractional.times x' (Fractional.recip (sqr x))))

[<Witness>]
type FloatingDUgly<'a, 'b when 'b :> Floating<'a>> =
    interface Floating<D<'a>> with
        member plus (D(x,x')) (D(y,y')) = D(Floating.plus x y, Floating.plus x' y')
        member times (D(x,x')) (D(y,y')) = D(Floating.times x y, Floating.plus (Floating.times y' x) (Floating.times x' y))
        member negate (D(x,x')) = D(Floating.negate x, Floating.negate x')
        member signum (D(x,_)) = D(Floating.signum x, Floating.fromInteger 0)
        member abs (D(x,x')) = D(Floating.abs x, Floating.times x' (Floating.signum x))
        member fromInteger x = constD (Floating.fromInteger x)
        member recip (D(x,x')) = D(Floating.recip x, Floating.negate (Floating.times x' (Floating.recip (sqr x))))
        member pi = constD Floating.pi
        member exp (D(x,x')) = D(Floating.exp x, Floating.times x' (Floating.exp x))
        member log (D(x,x')) = D(Floating.log x, Floating.times x' (Floating.recip x))
        member sqrt (D(x,x')) = D(Floating.sqrt x, Floating.times x' (Floating.recip (Floating.times (Floating.fromInteger 2) (Floating.sqrt x))))
        member sin (D(x,x')) = D(Floating.sin x, Floating.times x' (Floating.cos x))
        member cos (D(x,x')) = D(Floating.cos x, Floating.times x' (Floating.negate (Floating.sin x)))
        member asin (D(x,x')) = D(Floating.asin x, Floating.times x' (Floating.recip (Floating.sqrt (Floating.plus (Floating.fromInteger 1) (Floating.negate (sqr x))))))
        member acos (D(x,x')) = D(Floating.acos x, Floating.times x' (Floating.recip (Floating.negate (Floating.sqrt (Floating.plus (Floating.fromInteger 1) (Floating.negate (sqr x)))))))

let plusTwice x =
    Num.plus x x |> Num.plus x

let cube x =
    Num.times x x |> Num.times x

let f1 x =
    Fractional.times (Fractional.fromInteger 16) (Fractional.recip x)

let f2 x =
    Fractional.recip x

let f3 x =
    Floating.sqrt (Floating.times (Floating.fromInteger 3) (Floating.sin x))

plusTwice 2 |> printf "%d\n"
constD 1 |> printf "%A\n"
idD 1 |> printf "%A\n"
plusTwice (D(2.,1.)) |> printf "%A\n"
cube (D(2.,1.)) |> printf "%A\n"
f1 (D(2.,1.)) |> printf "%A\n"
f2 (D(2.,1.)) |> printf "%A\n"
f3 (D(2.,1.)) |> printf "%A\n"
