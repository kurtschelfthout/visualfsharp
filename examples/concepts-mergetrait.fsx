[<Trait>]
type MergeTrait<'T> =
    abstract Merge : 'T * 'T -> 'T
    abstract Empty : 'T

[<Witness>]
type MergeInt =
    interface MergeTrait<int> with
        member __.Merge(a,b) = a + b
        member __.Empty = 0

type Box<'T>(x:'T) =
    member __.Value = x
    override __.ToString() = sprintf "Box(%A)" x

[<Witness>]
type MergeBox<'T,'U when 'U :> MergeTrait<'T>> =
    interface MergeTrait<Box<'T>> with
        member __.Merge(a,b) = Box(trait<'U>.Merge(a.Value, b.Value))
        member __.Empty = Box(trait<'U>.Empty)

let mergetrait<'T,'U when 'U :> MergeTrait<'T>> = trait<'U>

let mergeOnce(x : 'T) =
    let x0 = mergetrait.Empty
    let x2 = mergetrait.Merge(x,x0)
    x2
