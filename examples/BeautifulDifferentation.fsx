
// Num Typeclass

[<Trait>]
type Num<'a> =
    abstract plus : 'a -> 'a -> 'a
    abstract minus : 'a -> 'a -> 'a
    abstract times : 'a -> 'a -> 'a

[<Witness>]
type NumInt =
    interface Num<int> with
        member __.plus a b = a + b
        member __.minus a b = a - b
        member __.times a b = a * b

type D<'a> = D of 'a * 'a

let (|><|) f f' (D(a, a')) = D(f a, a' * f' a)

[<Witness>]
type NumDiff<'T, 'U when 'U :> Num<'T>> =
    interface Num<D<'T>> with
        member __.plus (D(x,x')) (D(y,y')) = D(trait<'U>.plus x y, trait<'U>.plus x' y')
        member __.minus (D(x,x')) (D(y,y')) = D(trait<'U>.minus x y, trait<'U>.minus x' y')
        member __.times (D(x,x')) (D(y,y')) = D(trait<'U>.times x y, trait<'U>.plus (trait<'U>.times y' x) (trait<'U>.times x' y))

let plusTwice (x : 'T when 'U :> Num<'T>) =
//let plusTwice<'T, 'U when 'U :> Num<'T>> (x : 'T) =
    trait<'U>.plus x x

plusTwice 2 |> printf "%d\n"