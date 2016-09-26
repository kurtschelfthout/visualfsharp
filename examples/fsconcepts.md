# Classes for the Masses

*(A natural representation for type classes in .NET)*

* Claudio Russo (MSR)
* Matt Windsor (York)
* Don Syme (MSR)
* James Clarke (Cambridge) 
* Rupert Horlick (Cambridge)

---

##  Haskell trumps ML! 

![comparison](./images/comparison.png)

--

**How annoying...** 

---

## Introduction

Type classes are an immensely popular and productive feature of Haskell.

(Almost as good as modules, sometimes better!)

So good, other languages have stolen them:
 * ~~C++ concepts~~ (for perf!)
 * Scala *implicits*
 * Rust *traits* 
 * Swift *protocols*
 * Coq (2 variants),Agda, Clean....

--

But not F# or C#.

--

![wecandoit](./images/wecandoit.png)

---
##This talk:

We add type classes to F# using a cheap coding trick that is

* type preserving (no yucky erasure)
* efficient (thanks to .NET's run-time code specialiation)
* essentially free (zero VM modifications required)
* modular

For F#, we call type classes *traits* (Don's preference).

The same technique works interoperably for C# etc. 

*Trait F#* and *Concept C#* are implemented as open source prototypes.

---

##  Recap: Haskell Type Classes

Type classes are an abstraction mechanism for describing generic algorithms.

A *type class* is a predicate on types that specifies a set of required operations by signature.

A *class instance* declares class membership of a type by implementing each operation.

Type classes form hierarchies with *inheritance* and *subsumption*. 

A *generic instance* defines *families* of instances, predicated on class membership.

...



### Why should .NET care?

Instance declarations are decoupled from their types (unlike interfaces).

Type classes can have less overhead than OO abstractions. Even zero overhead.

Type classes allow efficient abstraction over numeric types (sorely missing in .NET).

---
background-image: url(./images/cheaptrick.jpg)
---

##  Haskell Type Classes
 
We represent a Haskell type class, e.g.

```Haskell
  class Eq a where 
    (==) :: a -> a -> Bool
```

as a *generic* F# interface:
```fsharp
 type Eq<'A> = interface 
     abstract equal: 'A -> 'A -> bool 
 end
```

Trait F#
```fsharp
 [<Trait>]
 type Eq<'A> = 
     abstract equal: 'A -> 'A -> bool 
```

---

## Haskell Overloads

The Haskell declaration of class `Eq a` implicitly declares the overload:

```Haskell
    (==) :: (Eq a) => a -> a -> Bool  -- note the added constraint!
```

In F#, an overload  can be encoded as a generic function, parameterized by an additional type parameter `'EqA`.

```fsharp
 let equal<'A,'EqA when 'EqA : struct and 'EqA :> Eq<'A>> a b =
       defaultof<'EqA>.equal a b
```

The ordinary type parameter `'EqA` is:
* constrained to be a `struct` (allocable on the stack); 
* bounded by its interface (`'EqA: Eq<'A>`);
* a named *witness* for the constraint `Eq<'A>`.

(think `equal`: `forall 'A, 'EqA <: Eq<'A>. 'A -> 'A -> bool`)


> *Haskell dictionary value ~ F# witness type*

---

## Zooming in

Look closely:

```fsharp
 let equal<'A,'EqA when 'EqA : struct and 'EqA :> Eq<'A>> a b =
       defaultof<'EqA>.equal a b
```

Where is the *value* parameter for the "dictionary" that GHC would insert?

--

We don't need one!

In F#, primitive `defaultof<T>` has type `T` and returns a default value.

Using `defaultof<T>`, we can create dictionary values on demand, when required for calls.

Doing so explicitly is gross, so in Trait F#, we extend the dot notation to access overloads:

```fsharp
 let equal a b = Eq.equal a b // dot notation for trait members
                              // introduces constraints 
```

(The `struct` constraints ensure dictionary values are never 'null'; no call can fail.)

---


## Haskell Instances

A Haskell ground instance, eg.

```Haskell
  instance Eq Integer where 
    x == y                =  x `integerEq` y
```

is translated to an F# *struct* implementing a trait (i.e. interface).
 
```fsharp
 type EqInt = struct 
      interface Eq<int> with 
        member this.equal a b = a = b
 end 
...
```

The struct is empty (think `unit`) but (!) has associated code.

Trait F#:
```fsharp
 [<Witness>] // a.k.a instance
 type EqInt = 
      interface Eq<int> with 
        member equal a b = a = b // no 'this.'
...
```
---


##  Generic Instances...

Haskells generic instances define *families* of instances.

E.g. 

Given an equality type `a`, we can define an equality
on *lists* of `a` (written `[a]`):

```Haskell
  instance (Eq a) => Eq [a] where 
       nil == nil      = true
    (a:as) == (b:bs)   = (a == b) && (as == bs)
         _ == _        = false
```

---

## ...Generic Instances

We can represent a Haskell *parameterized instance* as a *generic struct*, 
implementing an interface but parameterized by suitably constrained type parameters. 

```fsharp
type EqList<'A,'EqA when 'EqA : struct and 'EqA :> Eq<'A>> = struct
      interface Eq<'A list> with
        member this.equal a b =  // this unused
            match a,b with
            | a::l,b::m -> equal<'A,'EqA> a b && // type args reqd!
                           equal<'A list,EqList<'A,'EqA>> l m
            | [],[] -> true | _ ,_ -> false
 end 
```

Trait F#:
```fsharp
 [<Witness>]
 type EqList<'A,'EqA when 'EqA :> Eq<'A>> = // 'EqA:struct implicit
      interface Eq<'A list> with
        member equal a b = 
            match a,b with
            | a::l,b::m -> equal a b && equal l m
            | [],[] -> true | _ ,_ -> false
```

---

## Constructing Evidence

Derived instances allow Haskell to automatically construct instances as evidence for constraints:

```Haskell
  --- Since Eq Integer and Eq a => Eq (List a),
  --- we have Eq (List Integer) hence Eq (List (List Integer))
   
   [[1],[2,2],[3,3,3]] == [[3,3,3],[2,2],[1]]  -- typechecks!
```

In F# `EqInt:>Eq<int>` so `EqList<int,EqInt> :> Eq<int list>` so `EqList<int list,EqList<int,EqInt>> :> Eq<int list list>`.

In F#, dictionary type arguments cannot be inferred... they usually don't occur elsewhere in the type!

```fsharp
   equal [[1];[2;2];[3;3;3]] [[3;3;3];[2;2];[1]] // type error

   equal<int list list,EqList<int list,EqList<int,EqInt>>> 
   	 [[1];[2;2];[3;3;3]] [[3;3;3];[2;2];[1]] // works
```

Programming in the encoding requires explicit type abstraction and instantiation.

---

## Instance Inference

No programmer should write this crap!

In Trait F#, we extend inference so witness type arguments are derived from the trait hierarchy.

Trait F#:
```fsharp
   equal [[1];[2;2];[3;3;3]] [[3;3;3];[2;2];[1]] // type checks! 
```

Witness are inferred using type driven backchaining, similar to Haskell.

This is an extension of F#'s existing type constraint solver (Don's the man).

---


##  Derived Operations 

We translate Haskell's qualified types as extra, bounded type parameters denoting witness parameters.

For example, equality based list membership in Haskell is defined as follows:

```Haskell
  elem :: Eq a => a -> [a] -> bool
  x `elem`  []            = False
  x `elem` (y:ys)         = x==y || (x `elem` ys)  
``` 

In F#, we can encode this as:
```fsharp
 let rec elem<'A,'EqA when 'EqA:struct and 'EqA:>Eq<'A>> x ys =
     match ys with
     | [] -> false
     | y::ys -> equal<'A,'EqA> x y && elem x ys
```

Trait F#:
```fsharp
 let rec elem x ys =
     match ys with
     | [] -> false
     | y::ys -> equal x y && elem x ys
```

---


##  Type Class Inheritance

Haskell supports (multiple) inheritance of super classes.

```Haskell
  class (Eq a) => Num a where
    Add: a -> a -> a
    Mult: a -> a -> a
    Neg: a -> a
```

* Forall types `a`, `Num a` derives from `Eq a`. 

In F#, we instead use ordinary (multiple) interface inheritance:
```fsharp
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
```

Trait F#:
```fsharp
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
```

* Haskell class inheritance ~ F# interface inheritance 

---
##  Subsumption ...
Subsumption derives (evidence for) a class from (evidence for) its subclasses.

```Haskell
    equals :: (Eq a) => a -> a -> Bool

    square :: (Num a) => a -> a 
    square a = a * a

    memsq :: (Num a) => [a] -> a -> Bool
    memsq nil a = false
    memsq (h:t) a =     equals h (square a) 
                     -- ^^^^ legal coz Num a |= Eq a 
                     || memsq h t
```
---

##  ... Subsumption

F#:
```fsharp
 let square<'A,'NumA when 'NumA:struct and 'NumA:>Num<'A>> x =
     defaultof<'NumA>.mult x x

 let rec memsq<'A,'NumA when 'NumA:struct and 'NumA:>Num<'A>> n a = 
       match n with
       | [] -> false
       | (h::t) -> equal<'A,'NumA> h (square<'A,'NumA> a)
                  // ^^^^ legal coz 'NumA :> Num<'A> :> Eq<'A> 
                  || memsq<'A,'NumA> n a
```
Trait F#:
```fsharp
 let square x = Num.mult x x

 let rec memsq n a = 
       match n with
       | [] -> false
       | (h::t) -> equal h (square a) || memsq n a
```



---

## MSIL ByteCode

IL:
```cil
.method public hidebysig static bool
    Equals< A, 
    	    valuetype .ctor([mscorlib]System.ValueType, class Eq.Eq`1<!!A>) EqA> 
   (!!A a,!!A b) 
   cil managed {
   .locals init ([0] !!EqA loc1,[1] !!EqA loc2)
   ldloca.s loc1  // stack allocate dictionary
   initobj !!EqA 
   ldloc.0
   stloc.1
   ldloca.s loc2
   ldarg.0
   ldarg.1
   constrained. !!EqA
   callvirt instance bool class Eq.Eq`1<!!A>::Equals(!0, !0) 
   ret
}
```
The `callvirt` instruction is typically used for an indirect call to a virtual method.

When `EqA` is a struct, due to specialization, the callee is always known and often inlined.

---


### Performance

Evaluating a polynomial `f(x:T) = x*x + x + (T)666` (in C#), 

where `T`=`int`, `double`, `Class3D`, `Struct3D`.

(We evaluate `f(x)` at `1m` values of `x` using the BenchmarkDotNet harness.)

```csharp
    T ConceptGenericOpt<T, NumT>() where NumT : struct, Num<T> {
       NumT NI = default(NumT);
       T y = NI.FromInteger(0);
       T c = NI.FromInteger(666);
       for (int i = 0; i < n; i++) {
          T x = NI.FromInteger(i);
          y = NI.Plus(NI.Plus(NI.Mult(x,x),x), c);
       }
       return y;
    }
```
Comparisons:
+ Baseline: non generic, hand-specialized  code (one per T)
+ AbstractClass: generic class based (OOP style)
+ Interface: generic interface based (OOP style)
+ Delegate: generic, firt-class function based (OOP/FP style)
+ Instance: trait based (one dictionary value per call)
+ OptimizedInstance: optimized trait based (CSE on dictionaries) (shown above)

---

![x86](./images/bench/Slide1.png)

---

![x86](./images/bench/Slide2.png)

---

##x86 

This is the actual code jitted at `NumInt:Num<int>`:

```masm
00007FF9B5820D50  mov         byte ptr [rsp+20h],0  
00007FF9B5820D55  xor         eax,eax  
00007FF9B5820D57  xor         edx,edx  
00007FF9B5820D59  mov         ecx,dword ptr [7FF9B5704884h]  
00007FF9B5820D5F  test        ecx,ecx  
00007FF9B5820D61  jle         00007FF9B5820D75  
00007FF9B5820D63  mov         eax,edx  
00007FF9B5820D65  imul        eax,edx  
00007FF9B5820D68  add         eax,edx  
00007FF9B5820D6A  add         eax,29Ah  
00007FF9B5820D6F  inc         edx  
00007FF9B5820D71  cmp         edx,ecx  
00007FF9B5820D73  jl          00007FF9B5820D63  
00007FF9B5820D75  add         rsp,28h  
00007FF9B5820D79  ret  
```

Just straight-up arithmetic: despite  abstraction, *no* function calls remain!
---

## Summary & Take Home

-----------------------------
| Haskell | .NET | Trait F# |
|----------|--------|---------|
|type class | generic interface| trait |
|instance| (named) struct  | (named) witness |
|derived instance | generic struct | generic witness |
|inheritance	| interface inheritance | trait inheritance|
|overload | generic method | generic function|
|implicit dictionary passing | explicit type passing | implicit type passing |
|constraint solving | constraint checking | constraint solving |
|constraint propagation | NA | constraint propagation |
-------------------------------------------------------


* Haskell 98's type classes have a type preserving .NET representation.
* Dictionaries can be manually constructed and provided or, better, inferred.
* Generated code is efficient:
    * Dictionaries are empty (stack-allocated) structs. 
    * Dictionary allocation has zero runtime cost.
    * CLR's code specialization ensures all dictionary calls are direct calls at runtime. Many are in-lined.

---

##  Links & References

C# version of this document https://github.com/CaptainHayashi/roslyn/blob/master/concepts/docs/concepts.md.

Roslyn fork: https://github.com/CaptainHayashi/roslyn.

Roslyn https://github.com/dotnet/roslyn.

D. Gregor, J. Jarvi, J. Siek, B. Stroustrup, G. Dos Reis, and A. Lumsdaine. *Concepts: Linguistic support for generic programming in C++*, OOPSLA'06.

A. Kennedy and D. Syme. Design and implementation of generics for the .net common language
  runtime, PLDI 2001.

B. C. Oliveira, A. Moors, and M. Odersky. *Type classes as objects and implicits*, OOPSLA '10.

S. Peyton Jones. *Haskell 98 language and libraries : the revised report*. Cambridge University Press, May 2003.

P. Wadler and S. Blott. *How to make ad-hoc polymorphism less ad hoc*. POPL '89

D. Yu, A. Kennedy, and D. Syme. *Formalization of generics for the .net common language runtime.*, POPL 2004.




---
 
##  Example: Numeric types

Haskell has a rich numeric hierarchy.
```Haskell
  class Num a where
    Add: a -> a -> a
    Mult: a -> a -> a
    Neg: a -> a
  
  instance Num Integer where
    Add a b = a + b
    Mult a b = a * b
    Neg a  = -a
  
  instance Num Float where 
    ...
``` 

F#:
```fsharp
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
```

Trait F#:
```fsharp
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
```

---


##  Classy QuickSort 

C#:
```fsharp
    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    public static void qsort<IOrdT, T>(T[] arr, int a, int b)
      where IOrdT : struct, IOrd<T>  {
      IOrdT iordt = default(IOrdT); // <- explicit (stack) allocation :-{
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (iordt.Compare(arr[i], x) < 0) i++; // explicit receiver 'iord' :-{
          while (iordt.Compare(x, arr[j]) < 0) j--; // explicit receiver 'iord' :-{
          if (i <= j) {
            swap<T>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort<IOrdT, T>(arr, a, j);  // <-- non-inferrable type arguments coz of 'IOrdT' :-{
        qsort<IOrdT, T>(arr, i, b);  // <-- non-inferrable type argument coz of 'IOrdT' :-{
      }
    }
```

Concept C#:
```fsharp
    public static void qsort<T>(T[] arr, int a, int b)
      where IOrdT : IOrd<T> { 
      // sort arr[a..b]			   
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (Compare(arr[i], x) < 0) i++; // <-- inferred receiver :-)
          while (Compare(x, arr[j]) < 0) j--; // <-- inferred receiver :-)
          if (i <= j) {
            swap<T>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j); // <-- inferred type arguments :-)
        qsort(arr, i, b); // <-- inferred type arguments :-)
      }
    }
```


---


## Performance  (Variations of QuickSort)


![Perf](./images/perf.png)

---


```masm
--- C:\concepts\roslyn\concepts\conceptbench\conceptbench\Program.cs -----------
        T ConceptGenericOpt<T, NumT>() where NumT : struct, Num<T> {
...
            System.Diagnostics.Debugger.Break();
030E37CB  call        735E4558  
030E37D0  nop  
            NumT NI = default(NumT);
030E37D1  lea         eax,[ebp-40h]  
030E37D4  mov         byte ptr [eax],0  
            T y = NI.FromInteger(0);
030E37D7  lea         ecx,[ebp-40h]  
030E37DA  xor         edx,edx  
030E37DC  call        030E31B8  
030E37E1  mov         dword ptr [ebp-5Ch],eax  
030E37E4  mov         eax,dword ptr [ebp-5Ch]  
030E37E7  mov         dword ptr [ebp-44h],eax  
            T c = NI.FromInteger(666);
030E37EA  lea         ecx,[ebp-40h]  
            T c = NI.FromInteger(666);
030E37ED  mov         edx,29Ah  
030E37F2  call        030E31B8  
030E37F7  mov         dword ptr [ebp-60h],eax  
030E37FA  mov         eax,dword ptr [ebp-60h]  
030E37FD  mov         dword ptr [ebp-48h],eax  
            for (int i = 0; i < n; i++) {
030E3800  xor         edx,edx  
030E3802  mov         dword ptr [ebp-4Ch],edx  
030E3805  nop  
030E3806  jmp         030E3866  
030E3808  nop  
                T x = NI.FromInteger(i);
030E3809  lea         ecx,[ebp-40h]  
030E380C  mov         edx,dword ptr [ebp-4Ch]  
030E380F  call        030E31B8  
030E3814  mov         dword ptr [ebp-64h],eax  
030E3817  mov         eax,dword ptr [ebp-64h]  
030E381A  mov         dword ptr [ebp-50h],eax  
                y = NI.Plus(NI.Plus(NI.Mult(x,x),x), c);
030E381D  lea         eax,[ebp-40h]  
030E3820  mov         dword ptr [ebp-68h],eax  
030E3823  lea         eax,[ebp-40h]  
030E3826  mov         dword ptr [ebp-6Ch],eax  
030E3829  push        dword ptr [ebp-50h]  
030E382C  lea         ecx,[ebp-40h]  
030E382F  mov         edx,dword ptr [ebp-50h]  
030E3832  call        030E31C8  
030E3837  mov         dword ptr [ebp-70h],eax  
030E383A  push        dword ptr [ebp-50h]  
030E383D  mov         ecx,dword ptr [ebp-6Ch]  
030E3840  mov         edx,dword ptr [ebp-70h]  
030E3843  call        030E31D8  
                y = NI.Plus(NI.Plus(NI.Mult(x,x),x), c);
030E3848  mov         dword ptr [ebp-74h],eax  
030E384B  push        dword ptr [ebp-48h]  
030E384E  mov         ecx,dword ptr [ebp-68h]  
030E3851  mov         edx,dword ptr [ebp-74h]  
030E3854  call        030E31D8  
030E3859  mov         dword ptr [ebp-78h],eax  
030E385C  mov         eax,dword ptr [ebp-78h]  
030E385F  mov         dword ptr [ebp-44h],eax  
            }
030E3862  nop  
            for (int i = 0; i < n; i++) {
030E3863  inc         dword ptr [ebp-4Ch]  
030E3866  mov         eax,dword ptr [ebp-4Ch]  
030E3869  cmp         eax,dword ptr ds:[5F4051Ch]  
030E386F  setl        al  
030E3872  movzx       eax,al  
030E3875  mov         dword ptr [ebp-54h],eax  
030E3878  cmp         dword ptr [ebp-54h],0  
030E387C  jne         030E3808  
            return y;
030E387E  mov         eax,dword ptr [ebp-44h]  
030E3881  mov         dword ptr [ebp-58h],eax  
030E3884  nop  
030E3885  jmp         030E3887  
        }
030E3887  mov         eax,dword ptr [ebp-58h]  
030E388A  lea         esp,[ebp-0Ch]  
030E388D  pop         ebx  
030E388E  pop         esi  
030E388F  pop         edi  
030E3890  pop         ebp  
030E3891  ret  
```

---

##  Syntactic Support

* Distinguish type class declarations (new attribute [<Trait>])
* Anonymize instance declarations (new keyword instance)
* Add semi-implicit dictionary type abstraction (induced by concept constraints)
* Add implicit dictionary type instantiation (by extending type argument inference)

---


##  Anonymous classes, instances constraitns

In Haskell, instances and constraints (but not type classes) are *anonymous*:
* the programmer never  provides evidence for constraints
* evidence can always be inferred 
* evidence is always unique (by construction and imposed rules). 

Concept C#:
```fsharp
  instance EqInt : Eq<int> {
    bool Equals(int a, int b) => a == b;
  }

  instance <A>Eq<A[]> where EqA: Eq<A> {
    bool Equals(A[] a, A[] b) {
      if (a == null) return b == null;
      if (b == null) return false;
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if (Equals(a[i], b[i])) return false;
      return true;
    }
```

We *could* do something similar:

Concept C# with implicit instances:
```fsharp
  instance Eq<int> {
    bool Equals(int a, int b) => a == b;
  }
  instance <A>Eq<A[]> where Eq<A> {
    bool Equals(A[] a, A[] b) {
      if (a == null) return b == null;
      if (b == null) return false;
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if (Equals(a[i], b[i])) return false;
      return true;
    }
 }
```

More concise, but less flexible, and probably a bad idea, given C#'s limited type inference.

---

##  Implementation Status

* working compilers for C# and F#
* separate compilation via trivial attributes recognized on imports, emitted on export.
* C# syntax for named concepts, named instance and named concept constraints.
* concept parameters must be invariant.
* extended type argument inference to resolve concepts to instances 
  (simple backchaining, exploiting Roslyn's type unification algorithm to find instantiations).
* C# operators (+/- etc.) in concepts with extended operator resolution (just sugar, but important sugar).

---

Future:
* Associated types (concepts with abstract type components; compiled to additional parameters).
* Anonymous concepts, instance and constraints - not convinced this is a good idea for C#.
  * sans names, it's impossible to be explicit when C# type inference fails.
* Currently, different type instantiations, though all type safe, can produce different semantics.
  * adopt Haskell like restrictions to force coherence; or
  * disambiguate with ad-hoc *betterness* rules.
* Decidability of constraint solving.
* Allow & exploit variance (no such thing in Haskell).

---

##  Case Studies (In Progress)
* Micro-benchmarks for perf (sorting done, need more)
* Automatic Differentiation, overloading arithmetic to compute function derivatives [1]
* Concept C# rendition of Haskell Prelude, including numeric tower (think BCL)
* Generic QuickHull (one convex hull algorithm for generic vector spaces)
* Generic Graph Library - Haskell used to trump C#, does it still?
* C#,F# Numeric towers (haskell prelude)
* Generic Interface to System.Numerics.Vector2D/Vector3D/Vector4D (see QuickHull)
* THIS SPACE FOR RENT

[1]["Conal Elliot, Beautiful Differentiation"]


---
##  Machine Code


![x86](./images/x86.png)

---
