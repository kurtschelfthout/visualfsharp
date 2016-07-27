// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

/// Primary relations on types and signatures, with the exception of
/// constraint solving and method overload resolution.
module internal Microsoft.FSharp.Compiler.TypeRelations

open Internal.Utilities
open System.Text

open Microsoft.FSharp.Compiler 
open Microsoft.FSharp.Compiler.AbstractIL 
open Microsoft.FSharp.Compiler.AbstractIL.IL 
open Microsoft.FSharp.Compiler.AbstractIL.Internal 
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library 
open Microsoft.FSharp.Compiler.AbstractIL.Diagnostics 
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.ErrorLogger
open Microsoft.FSharp.Compiler.Tast
open Microsoft.FSharp.Compiler.Tastops
open Microsoft.FSharp.Compiler.Tastops.DebugPrint
open Microsoft.FSharp.Compiler.TcGlobals
open Microsoft.FSharp.Compiler.AbstractIL.IL 
open Microsoft.FSharp.Compiler.Lib
open Microsoft.FSharp.Compiler.Infos
open Microsoft.FSharp.Compiler.PrettyNaming
open Microsoft.FSharp.Compiler.AccessibilityLogic
open Microsoft.FSharp.Compiler.NameResolution

//-------------------------------------------------------------------------
// a :> b without coercion based on finalized (no type variable) types
//------------------------------------------------------------------------- 


// QUERY: This relation is approximate and not part of the language specification. 
//
//  Some appropriate uses: 
//     patcompile.fs: IsDiscrimSubsumedBy (approximate warning for redundancy of 'isinst' patterns)
//     tc.fs: TcRuntimeTypeTest (approximate warning for redundant runtime type tests)
//     tc.fs: TcExnDefnCore (error for bad exception abbreviation)
//     ilxgen.fs: GenCoerce (omit unnecessary castclass or isinst instruction)
//
let rec TypeDefinitelySubsumesTypeNoCoercion ndeep g amap m ty1 ty2 = 
  if ndeep > 100 then error(InternalError("recursive class hierarchy (detected in TypeDefinitelySubsumesTypeNoCoercion), ty1 = "^(DebugPrint.showType ty1),m))
  if ty1 === ty2 then true 
  // QUERY : quadratic
  elif typeEquiv g ty1 ty2 then true
  else
    let ty1 = stripTyEqns g ty1
    let ty2 = stripTyEqns g ty2
    match ty1,ty2 with 
    | TType_app (tc1,l1)  ,TType_app (tc2,l2) when tyconRefEq g tc1 tc2  ->  
        List.lengthsEqAndForall2 (typeEquiv g) l1 l2
    | TType_ucase (tc1,l1)  ,TType_ucase (tc2,l2) when g.unionCaseRefEq tc1 tc2  ->  
        List.lengthsEqAndForall2 (typeEquiv g) l1 l2
    | TType_tuple (tupInfo1,l1)    ,TType_tuple (tupInfo2,l2)     -> 
        evalTupInfoIsStruct tupInfo1 = evalTupInfoIsStruct tupInfo2 && 
        List.lengthsEqAndForall2 (typeEquiv g) l1 l2 
    | TType_fun (d1,r1)  ,TType_fun (d2,r2)   -> 
        typeEquiv g d1 d2 && typeEquiv g r1 r2
    | TType_measure measure1, TType_measure measure2 ->
        measureEquiv g measure1 measure2
    | _ ->  
        (typeEquiv g ty1 g.obj_ty && isRefTy g ty2) || (* F# reference types are subtypes of type 'obj' *)
        (isAppTy g ty2 &&
         isRefTy g ty2 && 

         ((match GetSuperTypeOfType g amap m ty2 with 
           | None -> false
           | Some ty -> TypeDefinitelySubsumesTypeNoCoercion (ndeep+1) g amap m ty1 ty) ||

           (isInterfaceTy g ty1 &&
            ty2 |> GetImmediateInterfacesOfType SkipUnrefInterfaces.Yes g amap m 
                |> List.exists (TypeDefinitelySubsumesTypeNoCoercion (ndeep+1) g amap m ty1))))



type CanCoerce = CanCoerce | NoCoerce

/// The feasible equivalence relation. Part of the language spec.
let rec TypesFeasiblyEquiv ndeep g amap m ty1 ty2 = 

    if ndeep > 100 then error(InternalError("recursive class hierarchy (detected in TypeFeasiblySubsumesType), ty1 = "^(DebugPrint.showType ty1),m));
    let ty1 = stripTyEqns g ty1
    let ty2 = stripTyEqns g ty2
    match ty1,ty2 with 
    // QUERY: should these be false for non-equal rigid typars? warn-if-not-rigid typars?
    | TType_var _ , _  
    | _, TType_var _ -> true
    | TType_app (tc1,l1)  ,TType_app (tc2,l2) when tyconRefEq g tc1 tc2  ->  
        List.lengthsEqAndForall2 (TypesFeasiblyEquiv ndeep g amap m) l1 l2
    | TType_tuple (tupInfo1, l1)    ,TType_tuple (tupInfo2, l2)     -> 
        evalTupInfoIsStruct tupInfo1 = evalTupInfoIsStruct tupInfo2 &&
        List.lengthsEqAndForall2 (TypesFeasiblyEquiv ndeep g amap m) l1 l2 
    | TType_fun (d1,r1)  ,TType_fun (d2,r2)   -> 
        (TypesFeasiblyEquiv ndeep g amap m) d1 d2 && (TypesFeasiblyEquiv ndeep g amap m) r1 r2
    | TType_measure _, TType_measure _ ->
        true
    | _ -> 
        false

/// The feasible coercion relation. Part of the language spec.

let rec TypeFeasiblySubsumesType ndeep g amap m ty1 canCoerce ty2 = 
    if ndeep > 100 then error(InternalError("recursive class hierarchy (detected in TypeFeasiblySubsumesType), ty1 = "^(DebugPrint.showType ty1),m))
    let ty1 = stripTyEqns g ty1
    let ty2 = stripTyEqns g ty2
    match ty1,ty2 with 
    // QUERY: should these be false for non-equal rigid typars? warn-if-not-rigid typars?
    | TType_var _ , _  | _, TType_var _ -> true

    | TType_app (tc1,l1)  ,TType_app (tc2,l2) when tyconRefEq g tc1 tc2  ->  
        List.lengthsEqAndForall2 (TypesFeasiblyEquiv ndeep g amap m) l1 l2
    | TType_tuple (tupInfo1,l1)    ,TType_tuple (tupInfo2,l2)     -> 
        evalTupInfoIsStruct tupInfo1 = evalTupInfoIsStruct tupInfo2 && 
        List.lengthsEqAndForall2 (TypesFeasiblyEquiv ndeep g amap m) l1 l2 
    | TType_fun (d1,r1)  ,TType_fun (d2,r2)   -> 
        (TypesFeasiblyEquiv ndeep g amap m) d1 d2 && (TypesFeasiblyEquiv ndeep g amap m) r1 r2
    | TType_measure _, TType_measure _ ->
        true
    | _ -> 
        // F# reference types are subtypes of type 'obj' 
        (isObjTy g ty1 && (canCoerce = CanCoerce || isRefTy g ty2)) 
        ||
        (isAppTy g ty2 &&
         (canCoerce = CanCoerce || isRefTy g ty2) && 
         begin match GetSuperTypeOfType g amap m ty2 with 
         | None -> false
         | Some ty -> TypeFeasiblySubsumesType (ndeep+1) g amap m ty1 NoCoerce ty
         end ||
         ty2 |> GetImmediateInterfacesOfType SkipUnrefInterfaces.Yes g amap m 
             |> List.exists (TypeFeasiblySubsumesType (ndeep+1) g amap m ty1 NoCoerce))

let FindWitness g amap m tcenv (traitTy: TType) =

    match tcenv with 
    | None -> 
        let typeText = NicePrint.stringOfTy (DisplayEnv.Empty g) traitTy
        errorR(Error(FSComp.SR.tcNoSolutionToTrait2(typeText),m))
        None //errorR(Error(FSComp.SR.typrelCannotResolveImplicitGenericInstantiation((DebugPrint.showType x), (DebugPrint.showType maxSoFar)),m)); maxSoFar
    | Some (tcrefs, instantiationGenerator) ->  

         let possibilities = 
            tcrefs
            |> Seq.choose (fun witnessTyconRef -> 
            
                 if TyconRefHasAttribute g m g.attrib_WitnessAttribute witnessTyconRef &&
                    witnessTyconRef.IsStructOrEnumTycon then

                    let tinst = instantiationGenerator m (witnessTyconRef.Typars m)
                    let witnessTyp = TType_app(witnessTyconRef,tinst)

                    if TypeFeasiblySubsumesType 0 g amap m traitTy CanCoerce witnessTyp
                    then Some witnessTyp
                    else None
                 else None)
            |> Seq.toList

         match possibilities with 
         | [sln] -> Some sln
         | [] -> 
             let typeText = NicePrint.stringOfTy (DisplayEnv.Empty g) traitTy
             errorR(Error(FSComp.SR.tcNoSolutionToTrait(typeText),m))
             None
         | sln :: _ -> 
             printfn "Too many solutions for trait %s:" (NicePrint.stringOfTy (DisplayEnv.Empty g) traitTy)
             let _ = possibilities |> List.map (fun possTy -> printfn "Possibility: %s" (NicePrint.stringOfTy (DisplayEnv.Empty g) possTy))
             Some sln



/// Choose solutions for Expr.TyChoose type "hidden" variables introduced
/// by letrec nodes. Also used by the pattern match compiler to choose type
/// variables when compiling patterns at generalized bindings.
///     e.g. let ([],x) = ([],[])
/// Here x gets a generalized type "list<'T>".
let ChooseTyparSolutionAndRange g amap (tp:Typar) =
    let m = tp.Range
    let max,m = 
         let initial = 
             match tp.Kind with 
             | TyparKind.Type -> g.obj_ty 
             | TyparKind.Measure -> TType_measure Measure.One
         // Loop through the constraints computing the lub
         ((initial,m), tp.Constraints) ||> List.fold (fun (maxSoFar,_) tpc -> 


             let join m x = 
                 if TypeFeasiblySubsumesType 0 g amap m x CanCoerce maxSoFar then maxSoFar
                 elif TypeFeasiblySubsumesType 0 g amap m maxSoFar CanCoerce x then x
                 else errorR(Error(FSComp.SR.typrelCannotResolveImplicitGenericInstantiation((DebugPrint.showType x), (DebugPrint.showType maxSoFar)),m)); maxSoFar

             // Don't continue if an error occurred and we set the value eagerly 
             if tp.IsSolved then maxSoFar,m else
             match tpc with 
             | TyparConstraint.CoercesTo(x,m,tcenv) -> 
                 
                 if isAppTy g x && TyconRefHasTraitAttribute g m (tcrefOfAppTy g x) then

                    // Traits can't act as solutions, instead we need to find an instance
                    // of the trait to act as a solution.  So go search for one.
                    let slnOpt = FindWitness g amap m tcenv x
                    match slnOpt with 
                    | Some sln -> join m sln,m
                    | None -> maxSoFar, m // an error has already been reported
                 else 
                    join m x,m

             | TyparConstraint.MayResolveMember(TTrait(_,nm,_,_,_,_),m) -> 
                 errorR(Error(FSComp.SR.typrelCannotResolveAmbiguityInOverloadedOperator(DemangleOperatorName nm),m))
                 maxSoFar,m
             | TyparConstraint.SimpleChoice(_,m) -> 
                 errorR(Error(FSComp.SR.typrelCannotResolveAmbiguityInPrintf(),m))
                 maxSoFar,m
             | TyparConstraint.SupportsNull m -> 
                 maxSoFar,m
             | TyparConstraint.SupportsComparison m -> 
                 join m g.mk_IComparable_ty,m
             | TyparConstraint.SupportsEquality m -> 
                 maxSoFar,m
             | TyparConstraint.Associated _ -> 
                 maxSoFar,m
             | TyparConstraint.IsEnum(_,m) -> 
                 errorR(Error(FSComp.SR.typrelCannotResolveAmbiguityInEnum(),m))
                 maxSoFar,m
             | TyparConstraint.IsDelegate(_,_,m) -> 
                 errorR(Error(FSComp.SR.typrelCannotResolveAmbiguityInDelegate(),m))
                 maxSoFar,m
             | TyparConstraint.IsNonNullableStruct m -> 
                 join m g.int_ty,m
             | TyparConstraint.IsUnmanaged m ->
                 errorR(Error(FSComp.SR.typrelCannotResolveAmbiguityInUnmanaged(),m))
                 maxSoFar,m
             | TyparConstraint.RequiresDefaultConstructor m -> 
                 maxSoFar,m
             | TyparConstraint.IsReferenceType m -> 
                 maxSoFar,m
             | TyparConstraint.DefaultsTo(_priority,_ty,m) -> 
                 maxSoFar,m)
    max,m

let ChooseTyparSolution g amap tp = 
    // We don't expect to solve any trait constraints for TyChoose expressions, hence use NoWitnessEnv
    let ty,_m = ChooseTyparSolutionAndRange g amap tp
    if tp.Rigidity = TyparRigidity.Anon && typeEquiv g ty (TType_measure Measure.One) then
        warning(Error(FSComp.SR.csCodeLessGeneric(),tp.Range))
    // We don't expect any required subsumptions (because we are using NoWitnessEnv), so we
    // can throw away _reqd
    ty

// Solutions can, in theory, refer to each other
// For example
//   'a = Expr<'b>
//   'b = int
// In this case the solutions are 
//   'a = Expr<int>
//   'b = int
// We ground out the solutions by repeatedly instantiating
let IterativelySubstituteTyparSolutions g tps solutions = 
    let tpenv = mkTyparInst tps solutions
    let rec loop n curr = 
        let curr' = curr |> instTypes tpenv 
        // We cut out at n > 40 just in case this loops. It shouldn't, since there should be no cycles in the
        // solution equations, and we've only ever seen one example where even n = 2 was required.
        // Perhaps it's possible in error recovery some strange situations could occur where cycles
        // arise, so it's better to be on the safe side.
        //
        // We don't give an error if we hit the limit since it's feasible that the solutions of unknowns
        // is not actually relevant to the rest of type checking or compilation.
        if n > 40 || List.forall2 (typeEquiv g) curr curr' then 
            curr 
        else 
            loop (n+1) curr'

    loop 0 solutions

let ChooseTyparSolutionsForFreeChoiceTypars g amap e = 
    match e with 
    | Expr.TyChoose(tps,e1,_m)  -> 
    
        /// Only make choices for variables that are actually used in the expression 
        let ftvs = (freeInExpr CollectTyparsNoCaching e1).FreeTyvars.FreeTypars
        let tps = tps |> List.filter (Zset.memberOf ftvs)
        
        let solutions =  
            tps 
            |> List.map (ChooseTyparSolution g amap) 
            |> IterativelySubstituteTyparSolutions g tps
        
        let tpenv = mkTyparInst tps solutions
        
        instExpr g tpenv e1

    | _ -> e
                 

/// Break apart lambdas. Needs ChooseTyparSolutionsForFreeChoiceTypars because it's used in
/// PostTypeCheckSemanticChecks before we've eliminated these nodes.
let tryDestTopLambda g amap (ValReprInfo (tpNames,_,_) as tvd) (e,ty) =
    let rec stripLambdaUpto n (e,ty) = 
        match e with 
        | Expr.Lambda (_,None,None,v,b,_,retTy) when n > 0 -> 
            let (vs',b',retTy') = stripLambdaUpto (n-1) (b,retTy)
            (v :: vs', b', retTy') 
        | _ -> 
            ([],e,ty)

    let rec startStripLambdaUpto n (e,ty) = 
        match e with 
        | Expr.Lambda (_,ctorThisValOpt,baseValOpt,v,b,_,retTy) when n > 0 -> 
            let (vs',b',retTy') = stripLambdaUpto (n-1) (b,retTy)
            (ctorThisValOpt,baseValOpt, (v :: vs'), b', retTy') 
        | Expr.TyChoose (_tps,_b,_) -> 
            startStripLambdaUpto n (ChooseTyparSolutionsForFreeChoiceTypars g amap e, ty)
        | _ -> 
            (None,None,[],e,ty)

    let n = tvd.NumCurriedArgs
    let tps,taue,tauty = 
        match e with 
        | Expr.TyLambda (_,tps,b,_,retTy) when not (isNil tpNames) -> tps,b,retTy 
        | _ -> [],e,ty
    let ctorThisValOpt,baseValOpt,vsl,body,retTy = startStripLambdaUpto n (taue,tauty)
    if vsl.Length <> n then 
        None 
    else
        Some (tps,ctorThisValOpt,baseValOpt,vsl,body,retTy)

let destTopLambda g amap topValInfo (e,ty) = 
    match tryDestTopLambda g amap topValInfo (e,ty) with 
    | None -> error(Error(FSComp.SR.typrelInvalidValue(), e.Range))
    | Some res -> res
    
let IteratedAdjustArityOfLambdaBody g arities vsl body  =
      (arities, vsl, ([],body)) |||> List.foldBack2 (fun arities vs (allvs,body) -> 
          let vs,body = AdjustArityOfLambdaBody g arities vs body
          vs :: allvs, body)

/// Do AdjustArityOfLambdaBody for a series of  
/// iterated lambdas, producing one method.  
/// The required iterated function arity (List.length topValInfo) must be identical 
/// to the iterated function arity of the input lambda (List.length vsl) 
let IteratedAdjustArityOfLambda g amap topValInfo e =
    let tps,ctorThisValOpt,baseValOpt,vsl,body,bodyty = destTopLambda g amap topValInfo (e, tyOfExpr g e)
    let arities = topValInfo.AritiesOfArgs
    if arities.Length <> vsl.Length then 
        errorR(InternalError(sprintf "IteratedAdjustArityOfLambda, List.length arities = %d, List.length vsl = %d" arities.Length vsl.Length, body.Range))
    let vsl,body = IteratedAdjustArityOfLambdaBody g arities vsl body
    tps,ctorThisValOpt,baseValOpt,vsl,body,bodyty


/// "Single Feasible Type" inference
/// Look for the unique supertype of ty2 for which ty2 :> ty1 might feasibly hold
let FindUniqueFeasibleSupertype g amap m ty1 ty2 =  
    if not (isAppTy g ty2) then None else
    let supertypes = Option.toList (GetSuperTypeOfType g amap m ty2) @ (GetImmediateInterfacesOfType SkipUnrefInterfaces.Yes g amap m ty2)
    supertypes |> List.tryFind (TypeFeasiblySubsumesType 0 g amap m ty1 NoCoerce) 
    

