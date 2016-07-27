module MonoidLib

[<Trait>]
type Monoid<'T> =
    abstract mempty : 'T
    abstract mappend : 'T -> 'T -> 'T

[<Trait>]
type Group<'T> =
    inherit Monoid<'T>
    abstract neg : 'T -> 'T

//Multiple Witnesses for same type parameter

[<Witness>]
type MonoidIntSum =
    interface Monoid<int> with
        member mempty = 0
        member mappend a b = a + b
    interface Group<int> with
        member neg a = -a

[<Witness>]
type GroupInt =
    interface Group<int> with
        member mempty = 0
        member mappend a b = a + b
        member neg a = -a

[<Witness>]
type MonoidIntProd =
    interface Monoid<int> with
        member mempty = 1
        member mappend a b = a * b

// Generic Witnesses

[<Witness>]
type MonoidList<'T> =
    interface Monoid<List<'T>> with
        member mempty = []
        member mappend a b = a @ b

let mappendTwice x =
    Monoid.mappend x x |> Monoid.mappend x