#I "bin"
#r "Monoid"

module Internal =

    open Monoid
    open Monoid.Typeclasses

    let invert x =
        Group.neg x

    (*let concat<'T, 'U when 'U :> Monoid<'T>> (xs : 'T list) =
        List.fold trait<'U>.mappend trait<'U>.mempty xs*)

    let concat xs =
        List.fold Monoid.mappend Monoid.mempty xs

    (*let funfunction (x : 'T when 'U :> Ord<'T> and 'U :> Monoid<'T>) =
        if Ord.compare x x = EQ then Monoid.mempty else Monoid.mappend x x

    funfunction 1 |> printf "%d\n"*)


//Higher-kinded types

(*[<Trait>]
type Functor<'F> =
    abstract fmap : ('a -> 'b) -> 'F 'a -> 'F 'b*)

open Internal
open Monoid.Sum
open Monoid.Prod
open Monoid.Typeclasses

let rec f x =
  if x > 0 then
    g ( x- 1)
  else
    Monoid.Typeclasses.Monoid.mappend x x

and g x =
  if x > 0 then
    f (x - 1)
  else
    x

concat [1;2;3;4] |> printf "%d\n"
concat [[1;2];[3;4]] |> printf "%A\n"
concat [[1;2];[3;4]] |> concat |> printf "%A\n"
concat [[[1;2];[3;4]];[[1;2];[3;4]]] |> printf "%A\n"

invert 1 |> printf "%d\n"

(*mappendTwice 2 |> printf "%d\n"*)