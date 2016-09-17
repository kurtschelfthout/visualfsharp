module Test 
 open Unchecked 


 type Eq<'A> = interface 
     abstract equal: 'A -> 'A -> bool 
 end

 let equal<'A,'EqA when 'EqA : struct and 'EqA :> Eq<'A>> a b = defaultof<'EqA>.equal a b

 type EqInt = struct 
      interface Eq<int> with 
        member this.equal a b = a = b
 end 

 type EqFloat = struct 
      interface Eq<float> with 
        member this.equal a b = a = b
 end 

 type EqList<'A,'EqA when 'EqA : struct and 'EqA :> Eq<'A>> =  struct
      interface Eq<'A list> with
        member this.equal a b = 
            match a,b with
            | a::l,b::m -> equal<'A,'EqA> a b && 
                           equal<'A list,EqList<'A,'EqA>> l m
            | [],[] -> true | _ ,_ -> false
 end 

 let test() = 
  let t0 = equal<int,EqInt> 1 1
  let t1 = equal<int list,EqList<int,EqInt>> [1] [1]
  let t2 = equal<int list list,EqList<int list,EqList<int,EqInt>>> [[1]] [[1]]
  ()
 let () = printfn "%A" (test()) 

 let rec elem<'A,'EqA when 'EqA:struct and 'EqA:>Eq<'A>> x ys =
     match ys with
     | [] -> false
     | y::ys -> equal<'A,'EqA> x y && elem x ys


 module SimpleNum = 
   type Num<'A> = interface
     abstract add: 'A -> 'A ->'A
     abstract mult: 'A -> 'A ->'A
     abstract neg: 'A -> 'A
   end

   type NumInt = struct
     interface Num<int> with
       member this.add a b = a + b
       member this.mult a b = a + b
       member this.neg a = -a
     end
   end

 type Num<'A> = interface
     inherit Eq<'A>
     abstract add: 'A -> 'A ->'A
     abstract mult: 'A -> 'A ->'A
     abstract neg: 'A -> 'A
 end

 type NumInt = struct
     interface Num<int> with
       member this.equal a b = equal<int,EqInt> a b // named instance!
       member this.add a b = a + b
       member this.mult a b  = a + b
       member this.neg a = -a 
     end
 end

 let square<'A,'NumA when 'NumA:struct and 'NumA:>Num<'A>> x =
     defaultof<'NumA>.mult x x

 let rec memsq<'A,'NumA when 'NumA:struct and 'NumA:>Num<'A>> n a = 
       match n with
       | [] -> false
       | (h::t) -> equal<'A,'NumA> h (square<'A,'NumA> a)
                   (*         ^^^^ legal coz NumA :> Num<'A> :> Eq<'A> *)
                  || memsq<'A,'NumA> n a

 

