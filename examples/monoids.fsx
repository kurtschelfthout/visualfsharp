#I "bin"
#r "MonoidLib"

open MonoidLib

let concat(*<'T, 'U when 'U :> Monoid<'T>>*) (xs : 'T list when 'U :> Monoid<'T>) =
    List.fold trait<'U>.mappend trait<'U>.mempty xs

let invert (x : 'T when 'U :> Group<'T>) =
    trait<'U>.neg x

(*let concat xs =
    List.fold Monoid.mappend Monoid.mempty xs*)

(*
Higher-kinded types

[<Trait>]
type Functor<'F> =
    abstract fmap : ('a -> 'b) -> 'F 'a -> 'F 'b

*)

concat [1;2;3;4] |> printf "%d\n"
concat [[1;2];[3;4]] |> printf "%A\n"
concat [[1;2];[3;4]] |> concat |> printf "%A\n"
concat [[[1;2];[3;4]];[[1;2];[3;4]]] |> printf "%A\n"

invert 1 |> printf "%d\n"

mappendTwice 2 |> printf "%d\n"