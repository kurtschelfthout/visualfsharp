module MonoidLib

[<Trait>]
type Monoid<'T> =
    abstract mempty : 'T
    abstract mappend : 'T -> 'T -> 'T

//Multiple Witnesses for same type parameter

[<Witness>]
type MonoidIntSum =
    interface Monoid<int> with
        member __.mempty = 0
        member __.mappend a b = a + b

[<Witness>]
type MonoidIntProd =
    interface Monoid<int> with
        member __.mempty = 1
        member __.mappend a b = a * b

// Generic Witnesses

[<Witness>]
type MonoidList<'T> =
    interface Monoid<List<'T>> with
        member __.mempty = []
        member __.mappend a b = a @ b