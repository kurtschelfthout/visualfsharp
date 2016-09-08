module Test =

 [<Trait>] type Eq<'A> = interface 
     abstract equals: 'A -> 'A -> bool 
 end

 [<Witness>] type EqInt = struct 
     interface Eq<int> with member equals a b = a = b
 end 

 [<Witness>] type EqList<'A,'EqA when 'EqA :> Eq<'A>> =  struct
    interface Eq<'A list> with
     member equals a b = match a,b with
      | a::l,b::m -> Eq.equals a b && Eq.equals l m
      | [],[] -> true | _ ,_ -> false
 end 

 
 let eq = Eq.equals

 let test() = 
    let t0 = Eq.equals 1 1
    let t1 = Eq.equals [1] [1]
    let t2 = Eq.equals [[1]] [[1]]
    t0 && t1 && t2 

 let () = printfn "%A" (test()) 