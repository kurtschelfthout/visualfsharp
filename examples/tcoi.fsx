[<Trait>]
type Ord<'T> =
    abstract compare : 'T -> 'T -> bool

let partition (a : 'T[] byref) (lo : int) (hi : int) =
    printfn "lo: %u hi: %u" lo hi
    let pivot = a.[hi]
    let mutable i = lo
    for j = lo to (hi - 1) do
        printfn "i: %u j: %u a[j]: %A pivot: %A" i j a.[j] pivot
        if Ord.compare a.[j] pivot
        then
            let tmp1 = a.[i]
            printfn "%u %u" i j
            a.[i] <- a.[j]
            a.[j] <- tmp1
            i <- i + 1
    let tmp1 = a.[hi]
    a.[hi] <- a.[i]
    a.[i] <- tmp1
    i

let rec qsort (lo : int) (hi : int) (a : 'T[] byref) =
    if lo < hi
    then
        let p = partition &a lo hi
        printfn "%A %u" a p
        qsort lo (p - 1) &a
        qsort (p + 1) hi &a

let sort (xs : 'T list) : 'T list when 'OrdT :> Ord<'T> =
    let mutable a = List.toArray xs
    qsort 0 (xs.Length - 1) &a
    Array.toList a 

[<Witness>]
type IntOrd =
    interface Ord<int> with
        member compare a b = a <= b

printfn "> sort [3; 2; 1] = %A" (sort [ 3; 2; 1 ])

// TODO: We don't have nested instances yet...

[<Trait>]
type Monoid<'T> =
    abstract mempty : 'T
    abstract mappend : 'T -> 'T -> 'T

let concat (xs : 'T list when 'U :> Monoid<'T>) =
    List.fold Monoid.mappend Monoid.mempty xs

module SumMod =
    [<Witness>]
    type Sum =
        interface Monoid<int> with
            member mempty = 0
            member mappend x y = x + y

    let sum (xs : int list) = concat xs

module ProdMod =
    [<Witness>]
    type Prod =
        interface Monoid<int> with
            member mempty = 1
            member mappend x y = x * y

    let prod (xs : int list) = concat xs

open SumMod
open ProdMod

printfn "Sum [1; 2; 3; 4] = %u" (sum [1; 2; 3; 4])
printfn "Prod [1; 2; 3; 4] = %u" (prod [1; 2; 3; 4])
printfn "Concat<Sum> [1; 2; 3; 4] = %u" (concat<int, Sum> [1; 2; 3; 4])