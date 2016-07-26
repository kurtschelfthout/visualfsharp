
[<Trait>]
type MergeTrait<'T> =
    abstract Merge : 'T * 'T -> 'T
    abstract Empty : 'T 

let trait<'T> = Unchecked.defaultof<'T>

let mergeTwice(x : 'T) =
    let x0 = trait<'U1 :> MergeTrait<'T>>.Empty
    let x2 = trait<'U2 :> MergeTrait<'T>>.Merge(x,x0)
    let x4 = trait<'U3 :> MergeTrait<'T>>.Merge(x2,x2)
    x4

[<Witness>] 
type MergeInt =  
    interface MergeTrait<int> with
        member Merge(a,b) = a + b
        member Empty = 0

type Box<'T>(x:'T) = 
    member __.Value = x
    override __.ToString() = sprintf "Box(%A)" x

[<Witness>]
type MergeBox<'T,'U when 'U :> MergeTrait<'T>> =
    interface MergeTrait<Box<'T>> with
        member Merge(a,b) = Box(MergeTrait.Merge(a.Value, b.Value))
        member Empty = Box(MergeTrait.Empty)


mergeTwice 4 |> printfn "%A"
mergeTwice (Box(4)) |> printfn "%A"
mergeTwice (Box(Box(4))) |> printfn "%A"
mergeTwice (Box(Box(Box(4)))) |> printfn "%A"

