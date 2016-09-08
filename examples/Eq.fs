module Test =

 [<Trait>] type Eq<'A> = // an interface
   abstract equals: 'A -> 'A -> bool

 [<Witness>] type EqInt = // a struct
   interface Eq<int> with 
     member equals a b  = a = b

 [<Witness>] type EqList<'A,'EqA when 'EqA :> Eq<'A>> = 
   interface Eq<'A list> with
     member equals a b = match a,b with
       | [],[] -> true
       | a::l,b::m -> Eq.equals a b && Eq.equals l m
       | _,_ -> false

 let test() = 
    let t0 = Eq.equals 1 1
    let t1 = Eq.equals [1] [1]
    let t2 = Eq.equals [[1]] [[1]]
    t0 && t1 && t2 

 let () = printfn "%A" (test())