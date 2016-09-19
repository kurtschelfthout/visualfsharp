# Classes for the Masses

*(A natural representation for type classes in .NET)*

Claudio Russo, Matt Windsor, Don Syme, James Clarke, Rupert Horlick

---

## Abstract:


Type classes are an immensely popular and productive feature of Haskell.

*Really!*

It turns out that they have a natural and efficient representation in .NET that is:
* type preserving (no yucky erasure)
* efficient (exploits Generic's USP: run-time code specialiation)
* essentially free (zero CLR changes required)

Our encoding provides safe and optional cross-language interoperation.

This paves the way for the extension of C#, F# etc. with Haskell style type classes. 

For C#, we call them *concepts*, as a nod to C++ *concepts*.

For F#, we call them *traits* (Don's preference).

---

##  Background: Haskell Type Classes

Haskell's *type classes* are a powerful abstraction mechanism for describing generic algorithms 
applicable to types that have distinct representations but common interfaces.

* A *type class* is a predicate on types that specifies a set of required operations by their type signatures.
* A *class instance* declares membership of an existing type in a class by providing 
  implementations for each of the class' operations.
* Type classes may be arranged hierarchically, permitting *inheritance* and *subsumption*. 
* Class instances may be generic, describing whole families of instances, predicated on type class membership.


---


## Why should .NET care?

Unlike OO interfaces:

* Instance declarations are decoupled from their associated types, allowing post-hoc interface ascription.
* Adding a concept to a framework type requires zero changes to the framework itself.
* Any operation in a type class can have a default implementation, allowing class instances to omit or override the default.
* A (much) cheaper yet efficient alternative to CLR += "static interfaces methods".


---

##  Why didn't we do this before?

Times have changed: classes aren't just a Haskell features anymore ...
*	Swift *protocols*
*	Scala *implicits* 
*	Rust	*traits*	
*	Go structural interfaces 
*	Academic proposals: JavaGI, Static Interface Methods for the CLR
*	Isabelle, Coq, LEAN (theorem provers)
*	~~C++ concepts~~
*	...


---


##  Compare with: "Static Interface Methods for the CLR"

Why wasn't this adopted?
*  Cost: Required CLR & BCL changes
* (soundness issues)

This approach requires *no* changes to CLR or BCL (compiler changes + conventions only).
It's *sound by construction*.

![staticinterfaces](./images/staticinterfaces.png)


---


##  Haskell comes top! 

![comparison](./images/comparison.png)

--

**How annoying...** 

---

##  Haskell Type Classes
 
We represent Haskell type classes as Generic interfaces.

```Haskell
  class Eq a where 
    (==)                  :: a -> a -> Bool
```

F#:
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

The Haskell declaration of class `Eq a` implicitly declares the overloaded 
operation(s) induced by class `Eq a` 's members.

```Haskell
    (==)                    :: (Eq a) => a -> a -> Bool 
```

In F#, an overload  is just a generic function, parameterized by an additional dictionary type parameter `'EqA`.

```fsharp
 let equal<'A,'EqA when 'EqA : struct and 'EqA :> Eq<'A>> a b =
       defaultof<'EqA>.equal a b
```

The dictionary type parameter `'EqA` is:
* constrained to be a `struct` (a stack allocated type); 
* bounded by its interface ('EqA: Eq<'A>)

> *Haskell dictionary value ~ F# dictionary type*

Passing a dictionary as a type (not value) is enough: we can always access the dictionary's operation(s) by allocating a default value. Stack allocation is cheap.

---

That's gross, so in Trait F#, we extend the dot notation to access overloads: 

```fsharp
 let equal a b = Eq.equal a b // dot notation for trait members
                              // introduces constraints
```




---


## Haskell Instances

A Haskell ground instance, eg.

```Haskell
  instance Eq Integer where 
    x == y                =  x `integerEq` y
```

is translated to a *struct* implementing a trait (i.e. interface).
 
```fsharp
 type EqInt = struct 
      interface Eq<int> with 
        member this.equal a b = a = b
 end 
...
```

Trait F#:
```fsharp
 [<Witness>] // a.k.a instance
 type EqInt = 
      interface Eq<int> with 
        member equal a b = a = b // no 'this.'
...
```


---


##  Derived Instances...

Derived instances define *families* of instances.

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

## ...Derived Instances

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

In F#, instance type arguments cannot be inferred from arguments' types. (Why? No occurrences in parameters' types!)

```fsharp
   equal [[1];[2;2];[3;3;3]] [[3;3;3];[2;2];[1]] // type error

   equal<int list list,EqList<int list,EqList<int,EqInt>>> 
   	 [[1];[2;2];[3;3;3]] [[3;3;3];[2;2];[1]] // works
```

---

## Instance Inference

No programmer should write this crap!

In Trait F#, we extend type argument inference so:

* so witness type arguments are inferred from trait hierarchy.

Trait F#:
```fsharp
   equal [[1];[2;2];[3;3;3]] [[3;3;3];[2;2];[1]] // type checks! 
```

Instance type parameters are inferred using type driven backchaining, similar to Haskell.

This is an extension of F#'s existing type constraint solver (ask Don).

---


##  Derived Operations 

We translate Haskell's qualified types as extra type parameters, constrained to be both structs and bound by translations of their type class constraints.

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


## Disassembly

C#:
```fsharp
public static bool Equals<EqA,A>(A a, A b) 
                     where EqA : struct, Eq<A>
       => default(EqA).Equals(a, b);
```

Concept C#:
```fsharp
public static bool Equals<A>(A a, A b) 
                     where EqA : Eq<A>
       => Equals(a, b);
```

IL:
```
.method public hidebysig static bool
    Equals<valuetype .ctor([mscorlib]System.ValueType, class Eq.Eq`1<!!A>) EqA, A> 
                       // dictionary EqA is a type (not value) parameter   ^^^
        (!!A a,!!A b) 
   cil managed {
   .locals init ([0] !!EqA loc1,[1] !!EqA loc2)
   IL_0000: ldloca.s loc1  // stack allocation of default struct (actually an empty token)
   IL_0002: initobj !!EqA 
   IL_0008: ldloc.0
   IL_0009: stloc.1
   IL_000a: ldloca.s loc2
   IL_000c: ldarg.0
   IL_000d: ldarg.1
   IL_000e: constrained. !!EqA  
   IL_0014: callvirt instance //a direct call to an interface method on that struct
              bool class Eq.Eq`1<!!A>::Equals(!0, !0) 
   IL_0019: ret
}

```

---

##  Machine Code


![x86](./images/x86.png)


---


## Summary

-----------------------------
| Haskell | .NET | Trait F# |
|----------|--------|---------|
|type class	| generic interface| trait |
|(anonymous) instance| (named) struct  | (named) witness |
|derived instance | generic struct | generic witness |
|type class inheritance	| interface inheritance | trait inheritance|
|overloaded operation | constrained generic value | constrained generic value|
|dictionary construction | explicit type construction | implicit type construction|
|implicit dictionary passing | explicit type passing | implicit type passing |
|constraint inference & propagation | constraint checking | constraint solving |
--------------------------------------------------------------------------------

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

More concise, but less flexiblee, and probably a bad idea, given C#'s limited type inference.

---

##  Implementation Status

* working compilers for C# and F#
* separate compilation via trivial attributes recognized on imports, emitted on export.
* C# syntax for named concepts, named instance and named concept constraints.
* concept parameters must be invariant.
* extended type argument inference to resolve concepts to instances 
  (simple backchaining, exploiting Roslyn's type unification algorithm to find instantiations).
* C# operators (+/- etc.) in concepts with extended operator resolution (just sugar, but important sugar).

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


##  Take Home

* Haskell 98's type classes have a type preserving .NET representation.
* Dictionaries must be manually constructed and provided  (a modified C#/F# compiler could do this for the user.)
* Generated code is efficient:
    * Dictionaries are empty (stack-allocated) structs. 
    * Dictionary allocation has zero runtime cost.
    * CLR's code specialization ensures all dictionary calls are direct calls at runtime. (In principle, these calls could be in-lined by the JIT compiler)

---


##  Links & References


C# version of this document https://github.com/CaptainHayashi/roslyn/blob/master/concepts/docs/concepts.md.

Roslyn fork: https://github.com/CaptainHayashi/roslyn.

Roslyn https://github.com/dotnet/roslyn.

Rust  *traits* https://doc.rust-lang.org/book/traits.html.

Swift *protocols* https://developer.apple.com/library/ios/documentation/Swift/Conceptual/Swift_Programming_Language/Protocols.html.

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

