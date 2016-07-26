open System

/// <summary>
///     A JSON value.
/// </summary>
type JValue =
| JString of string
| JNumber of double
| JBool of bool
| JNull
| JObject of Map<string, JValue>
| JArray of JValue list

type Result<'A> =
    | Ok of 'A
    | Error of string

let fmap<'A, 'B> (f : 'A -> 'B) (r : Result<'A>) : Result<'B> =
    match r with
    | Ok v -> Ok (f v)
    | Error e -> Error e

let collect<'A> (xs : Result<'A> list) : Result<'A list> =
    List.foldBack
        (fun e vs ->
         match (e, vs) with
         | _, Error s -> Error s
         | Error s, _ -> Error s
         | Ok v, Ok vs -> Ok (v::vs))
        xs
        (Ok [])

let rec render =
    function
    | JString s -> s
    | JNumber k -> sprintf "%f" k
    | JBool true -> "true"
    | JBool false -> "false"
    | JNull -> "null"
    | JObject o ->
        o
        |> Map.toSeq
        |> Seq.map (fun (k, v) -> sprintf "\"%s\": %s" k (render v))
        |> (fun xs -> String.Join(", ", xs))
        |> sprintf "{%s}"
    | JArray xs ->
        sprintf "[%s]"
                (String.Join(", ", List.map render xs))

/// <summary>
///     Concept for things that can be converted to or from JSON.
/// </summary>
/// <typeparam name="A">
///     The type of the thing to be converted.
/// </typeparam>
[<Trait>]
type CJson<'A> =
    abstract ToJValue : 'A -> JValue
    abstract FromJValue : JValue -> Result<'A>

[<Witness>]
type CJsonJValue =
    interface CJson<JValue> with
        member ToJValue a = a
        member FromJValue a = Ok a

[<Witness>]
type CJsonBool =
    interface CJson<bool> with
        member ToJValue a = JBool a
        member FromJValue a =
            match a with
            | JBool b -> Ok b
            | _ -> Error "not a JSON boolean"

[<Witness>]
type CJsonString =
    interface CJson<string> with
        member ToJValue a = JString a
        member FromJValue a =
            match a with
            | JString s -> Ok s
            | _ -> Error "not a JSON string"

[<Witness>]
type CJsonInt =
    interface CJson<int> with
        member ToJValue a = JNumber (double a)
        member FromJValue a =
            match a with
            | JNumber n -> Ok (int n)
            | _ -> Error "not a JSON number"

[<Witness>]
type CJsonDouble =
    interface CJson<double> with
        member ToJValue a = JNumber a
        member FromJValue a =
            match a with
            | JNumber n -> Ok n
            | _ -> Error "not a JSON number"

[<Witness>]
type CJsonArray<'A, 'CJsonA when 'CJsonA :> CJson<'A>> =
    interface CJson<'A list> with
        member ToJValue xs =
            xs
            |> List.map CJson.ToJValue
            |> JArray

        member FromJValue a =
            match a with
            | JArray xs ->
                xs
                |> List.map CJson.FromJValue
                |> collect
            | _ -> Error "not a JSON array"

[<Witness>]
type CJsonDict<'A, 'CJsonA when 'CJsonA :> CJson<'A>> =
    interface CJson<Map<string, 'A>> with
        member ToJValue adict =
            adict
            |> Map.map (fun _ t -> CJson.ToJValue t)
            |> JObject

        member FromJValue a =
            match a with
            | JObject dict ->
                dict
                |> Map.toList
                |> List.map
                    (fun (k, v) ->
                        v
                        |> CJson.FromJValue
                        |> fmap (fun v' -> (k, v')))
                |> collect
                |> fmap Map.ofList
            | _ -> Error "not a JSON object"

// Toy instance for tuples, to show how you might do general object structures.

[<Witness>]
type CJsonTup2<'A, 'B, 'CJsonA, 'CJsonB when 'CJsonA :> CJson<'A> and 'CJsonB :> CJson<'B>> =
    interface CJson<((string * 'A) * (string * 'B))> with
        member ToJValue (((ka, va), (kb, vb))) =
            Map.ofList [ (ka, CJson.ToJValue va)
                         (kb, CJson.ToJValue vb) ]
            |> JObject

        member FromJValue v =
            match v with
            | JObject dict ->
                dict
                |> Map.toList
                |> function
                   | [ (kx, vx); (ky, vy) ] ->
                       match (CJson.FromJValue vx,
                              CJson.FromJValue vy) with
                       | (Ok va, Ok vb) -> Ok ((kx, va), (ky, vb))
                       | _ ->
                           match (CJson.FromJValue vx,
                                  CJson.FromJValue vy) with
                           | (Ok vb, Ok va) -> Ok ((kx, va), (ky, vb))
                           | _ -> Error "invalid fields of object"
                   | _ -> Error "invalid object size"
            | _ -> Error "not a JSON object"


let jsonify<'A, 'CJsonA when 'CJsonA :> CJson<'A>> : 'A -> JValue =
    trait<'CJsonA>.ToJValue

let ob =
    Map.ofList
        [
            ("title", "Simon Peyton Jones: papers")
            ("snippet", "Tackling the awkward squad: monadic input/output, concurrency, exceptions, and foreign-language calls in Haskell")
            ("url", "http://research.microsoft.com/~simonpj/papers/marktoberdorf/")
        ]
printfn "%s" (ob |> jsonify |> render)

let ob2 = (("name", "Nineteen Eighty-Four"), ("year", 1948))
printfn "%s" (ob2 |> jsonify |> render)
