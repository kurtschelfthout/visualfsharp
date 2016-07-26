[<Trait>]
type MergeTrait<'T> =
    abstract Merge : 'T * 'T -> 'T
    abstract Empty : 'T

[<Witness>]
type MergeInt =
    interface MergeTrait<int> with
        member Merge(a,b) = a + b
        member Empty = 0

type Box<'T>(x:'T) =
    member Value = x
    override ToString() = sprintf "Box(%A)" x

[<Witness>]
type MergeBox<'T,'U when 'U :> MergeTrait<'T>> =
    interface MergeTrait<Box<'T>> with
        member Merge(a,b) = Box(MergeTrait.Merge(a.Value, b.Value))
        member Empty = Box(MergeTrait.Empty)

let mergetrait<'T,'U when 'U :> MergeTrait<'T>> = trait<'U>

let mergeTwice(x : 'T) =
    let x0 = mergetrait.Empty
    let x2 = mergetrait.Merge(x,x0)
    let x4 = mergetrait.Merge(x2,x2)
    x4

let mergeOnce(x : 'T) =
    let x0 = mergetrait.Empty
    (x0 : 'T)

mergeTwice 4 |> printfn "%A"
mergeTwice (Box(4)) |> printfn "%A"
mergeTwice (Box(Box(4))) |> printfn "%A"
mergeTwice (Box(Box(Box(4)))) |> printfn "%A"