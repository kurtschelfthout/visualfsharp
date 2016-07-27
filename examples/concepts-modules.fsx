module TypeClasses =
    [<Trait>]
    type MergeTrait<'T> =
        abstract Merge : 'T * 'T -> 'T
        abstract Empty : 'T

module Witnesses =
    open TypeClasses
    [<Witness>] 
    type MergeInt =
        interface MergeTrait<int> with
            member __.Merge(a,b) = a + b
            member __.Empty = 0

module Functions =
    let mergeTwice (x: 'T when 'U :> TypeClasses.MergeTrait<'T>) =       
        let x0 = trait<'U>.Empty   
        let x2 = trait<'U>.Merge(x,x0)   
        let x4 = trait<'U>.Merge(x2,x2)   
        x4

open TypeClasses
open Witnesses
open Functions

type Box<'T>(x:'T) = 
    member __.Value = x
    override __.ToString() = sprintf "Box(%A)" x

[<Witness>]
type MergeBox<'U when 'U :> MergeTrait<int>> =
    interface MergeTrait<Box<int>> with
        member __.Merge(a,b) = Box(trait<'U>.Merge(a.Value, b.Value))
        member __.Empty = Box(trait<'U>.Empty)

mergeTwice 4 |> printfn "%A"
mergeTwice (Box(4)) |> printfn "%A"

module Test =
    open TypeClasses
    open Witnesses
    open Functions
    let x () = mergeTwice 4

Test.x () |> printfn "%A"