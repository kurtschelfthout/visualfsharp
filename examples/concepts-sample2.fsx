
[<Trait>]
type MergeTrait<'T> =
    abstract Merge : 'T * 'T -> 'T
    abstract Empty : 'T 

let trait<'T> = Unchecked.defaultof<'T>

let mergeTwice<'T, 'U when 'U :> MergeTrait<'T>>(x : 'T ) =       
    let x0 = trait<'U>.Empty   
    let x2 = trait<'U>.Merge(x,x0)   
    let x4 = trait<'U>.Merge(x2,x2)   
    x4

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


mergeTwice 4 |> printfn "%A"
mergeTwice (Box(4)) |> printfn "%A"
mergeTwice (Box(Box(4))) |> printfn "%A"
mergeTwice (Box(Box(Box(4)))) |> printfn "%A"

