#load ".paket/load/fsharp.data.fsx"
open FSharp.Data

let info = JsonValue.Parse("""{"foo": { "bar": [ {"a": 1, "b": 2} ], "baz": { "a": 1 } } }""")

let rec replaceJson (replacements: (string * JsonValue)[]) (value: JsonValue) =
    let replaceProperties = function
        | (name, value) ->
            try Array.find (function | (n, v) -> n = name) replacements
            with _ -> (name, (replaceJson replacements value))

    match value with
    | JsonValue.Record properties ->
        properties
            |> Array.map replaceProperties
            |> JsonValue.Record
    | JsonValue.Array elements ->
        elements
            |> Array.map (replaceJson replacements)
            |> JsonValue.Array
    | _ -> value

printfn "Before %O" info
printfn "After %O" (replaceJson [|("a", (JsonValue.Number 3m))|] info)