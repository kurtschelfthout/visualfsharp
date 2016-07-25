

//-----------------------------------------


[<Trait>]
type MergeTrait<'T> =
    abstract Merge : 'T * 'T -> 'T
    abstract Empty : 'T 


// mergeTwice<'T,'U  when 'U :> MergeTrait<'T>>  : 'T -> 'T


// 6. Make type parameters implicit by automatically generalizing 'U here

//let mergeTwice  (x: 'T when 'U :> MergeTrait<'T>) =       
let mergeTwice<'T, 'U when 'U :> MergeTrait<'T>>(x : 'T ) =       
    let x0 = trait<'U>.Empty   
    let x2 = trait<'U>.Merge(x,x0)   
    let x4 = trait<'U>.Merge(x2,x2)   
    x4

[<Witness; Struct>] // 1. get rid of Struct
type MergeInt(_fake:int) =   // 2. get rid of _fake
    interface MergeTrait<int> with
        member __.Merge(a,b) = a + b
        member __.Empty = 0

(*
[<Witness; Struct>]
type MergeInt2(_fake:int) = 
    interface MergeTrait<int> with
        member __.Merge(a,b) = a + b
        member __.Empty = 0
*)

// ?B when ?B :> MergeTrait<int> 
//
// v : int
//




mergeTwice 4 |> printfn "%A"


(*
// 3. Generic witnesses
[<Witness>]
type MergeList<'T>() = 
    interface MergeTrait<List<'T>> with
        member __.Merge(a,b) = List.append a b
        member __.Empty = List.empty

*)


type Box<'T>(x:'T) = 
    member __.Value = x

// 4. Derived witnesses
[<Witness;Struct>]
type MergeBox<'U when 'U :> MergeTrait<int>> =
    interface MergeTrait<Box<int>> with
        member __.Merge(a,b) = Box(trait<'U>.Merge(a.Value, b.Value))
        member __.Empty = Box(trait<'U>.Empty)


mergeTwice (Box(4)) |> printfn "%A"


(*
// 5. Derived generic witnesses
[<Witness>]
type MergeBox<'T,'U when 'U : MergeTrait<'T>> =
    interface MergeTrait<Box<'T>> with
        member __.Merge(a,b) = Box(trait<'U>.Merge(a.Unbox, b.Unbox))
        member __.Empty = Box(trait<'U>.Empty)


*)
