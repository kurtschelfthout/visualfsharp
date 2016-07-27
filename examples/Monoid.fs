module Monoid

module Typeclasses =

    [<Trait>]
    type Monoid<'T> =
        abstract mempty : 'T
        abstract mappend : 'T -> 'T -> 'T

    [<Trait>]
    type Group<'T> =
        inherit Monoid<'T>
        abstract neg : 'T -> 'T

open Typeclasses

//Multiple Witnesses for same type parameter

module Sum =

    [<Witness>]
    type MonoidIntSum =
        interface Monoid<int> with
            member mempty = 0
            member mappend a b = a + b
        interface Group<int> with
            member neg a = -a

module Prod =

    [<Witness>]
    type MonoidIntProd =
        interface Monoid<int> with
            member mempty = 1
            member mappend a b = a * b

[<Witness>]
type GroupInt =
    interface Group<int> with
        member mempty = 0
        member mappend a b = a + b
        member neg a = -a

// Generic Witnesses

[<Witness>]
type MonoidList<'T> =
    interface Monoid<List<'T>> with
        member mempty = []
        member mappend a b = a @ b

(*let mappendTwice x =
    Monoid.mappend x x |> Monoid.mappend x*)

// Ordering

type Ordering = LT | EQ | GT

[<Witness>]
type MonoidOrdering =
    interface Monoid<Ordering> with
        member mempty = EQ
        member mappend a b =
            match a with
                | LT -> LT
                | EQ -> b
                | GT -> GT

[<Trait>]
type Ord<'a> =
    abstract compare : 'a -> 'a -> Ordering

[<Witness>]
type OrdInt =
    interface Ord<int> with
        member compare a b =
            match a - b with
                | x when x > 0 -> GT
                | x when x = 0 -> EQ
                | _ -> LT

[<Witness>]
type MonoidOrdInt =
    interface Ord<int> with
        member compare a b =
            match a - b with
                | x when x > 0 -> GT
                | x when x = 0 -> EQ
                | _ -> LT
    interface Monoid<int> with
        member mempty = 1
        member mappend a b = a * b