module Numeric

// Num Typeclass

[<Trait>]
type Num<'a> =
    abstract plus : 'a -> 'a -> 'a
    abstract times : 'a -> 'a -> 'a
    abstract negate : 'a -> 'a
    abstract abs : 'a -> 'a
    abstract signum : 'a -> 'a
    abstract fromInteger : int -> 'a

(*[<Witness>]
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
        member fromInteger a = double a*)

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

(*[<Witness>]
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
        member recip a = 1. / a*)

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
    abstract atan : 'a -> 'a
    abstract sinh : 'a -> 'a
    abstract cosh : 'a -> 'a
    abstract asinh : 'a -> 'a
    abstract acosh : 'a -> 'a
    abstract atanh : 'a -> 'a

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
        member atan a = Operators.atan a
        member sinh a = Operators.sinh a
        member cosh a = Operators.cosh a
        member asinh a = Operators.log (a + Operators.sqrt (1. + a * a))
        member acosh a = Operators.log (a + Operators.sqrt (a * a - 1.))
        member atanh a = 0.5 * (Operators.log (1. + a) - Operators.log (1. - a))
        
//Operator wrappers

let (+) = Floating.plus
let (-) a b = Floating.plus a (Floating.negate b)
let (~-) = Floating.negate
let (*) = Floating.times
let (/) a b = Floating.times a (Floating.recip b)
