[<Trait>]
type Monoid<'T> =
    abstract mempty : 'T
    abstract mappend : 'T -> 'T -> 'T

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

let concat<'T, 'U when 'U :> Monoid<'T>> =
    List.fold trait<'U>.mappend trait<'U>.mempty

concat [1;2;3;4] |> printf "%d"