module Test 
 open Unchecked 

 [<Trait>]
 type Eq<'A> = 
     abstract equal: 'A -> 'A -> bool 

 let equal a b = Eq.equal a b

 [<Witness>]
 type EqInt = 
      interface Eq<int> with 
        member equal a b = a = b // no 'this.'

 [<Witness>]
 type EqFloat = 
      interface Eq<float> with 
        member equal a b = a = b // no 'this.'

 [<Witness>]
 type EqList<'A,'EqA when 'EqA :> Eq<'A>> = // implied 'EqA:struct
      interface Eq<'A list> with
        member this.equal a b = 
            match a,b with
            | a::l,b::m -> equal a b && 
                           equal l m
            | [],[] -> true | _ ,_ -> false


 let test() = 
  let true = equal 1 1
  let true = equal [1] [1]
  let true = equal [[1]] [[1]]
  ()
 let () = printfn "%A" (test()) 

 let rec elem x ys =
     match ys with
     | [] -> false
     | y::ys -> equal x y && elem x ys


 module SimpleNum = 
   [<Trait>]
   type Num<'A> = 
     abstract add: 'A -> 'A ->'A
     abstract mult: 'A -> 'A ->'A
     abstract neg: 'A -> 'A

   [<Witness>]
   type NumInt = 
     interface Num<int> with
       member add a b = a + b
       member mult a b = a + b
       member neg a = -a
     end

 [<Trait>]
 type Num<'A> = 
     inherit Eq<'A>
     abstract add: 'A -> 'A ->'A
     abstract mult: 'A -> 'A ->'A
     abstract neg: 'A -> 'A

 [<Witness>]
 type NumInt = 
     interface Num<int> with
       member equal a b = equal<_,EqInt> a b 
       member add a b = a + b
       member mult a b  = a + b
       member neg a = -a 
     end


 let square x = Num.mult x x

 let rec memsq n a = 
       match n with
       | [] -> false
       | (h::t) -> equal h (square a) || memsq n a

 

