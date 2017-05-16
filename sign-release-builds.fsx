#if INTERACTIVE
#r "System.Xml.Linq"
#endif

#load ".paket/load/fsharp.data.fsx"
open FSharp.Data
open FSharp.Data.JsonExtensions

#load "helpers.fsx"
open Helpers

open System
open System.IO

let upload (file: string) (url: Uri) =
    printfn "Uploading %s to %s" file (url.ToString())

let download (url: Uri) (filename: string) =
    printfn "Downloading %s to %s" (url.ToString()) filename

let signedUrl (url: Uri) (file: string) =
    let segments = url.Segments
    let idx = (Array.length segments) - 1
    Array.set segments idx (sprintf "signed/%s" (Array.last segments))
    (UriBuilder(url.Scheme, url.Host, url.Port, segments |> String.concat "")).Uri

let filesInRelease (release: JsonValue) =
    [for r in release.["release"] -> r.["file"]] |> Set.ofList

let replace (file: string) (dir: string) =
    if not (File.Exists file) then failwith (sprintf "%s is not a file" file)
    if not (Directory.Exists dir) then failwith (sprintf "%s is not a directory" dir)

    let release = JsonValue.Parse (File.ReadAllText file)
    let files   = filesInRelease release
    let signedFiles = DirectoryInfo(dir).EnumerateFiles() |> Seq.map (fun fi -> fi.Name) |> Set.ofSeq
    let missing = Set.difference (files |> Set.map (fun i -> i.AsString())) signedFiles

    if Set.isEmpty missing then printfn "Skipping: %A" missing

    for signed in signedFiles do
        let signedMd5 = md5File signed
        let signedSize = FileInfo(signed).Length
        let r = files |> Seq.find(fun url -> url.AsString().EndsWith signed)
        let newUrl = signedUrl (Uri(r.AsString())) signed
        upload signed newUrl

    release.WriteTo(File.CreateText("signed-" + file), JsonSaveOptions.None)

let fetch (file: string) (dir: string) =
    if not (File.Exists file) then failwith (sprintf "%s is not a file" file)
    if not (Directory.Exists dir) then Directory.CreateDirectory(dir) |> ignore

    for f in filesInRelease (JsonValue.Parse (File.ReadAllText file)) do
        let url = Uri(f.AsString())
        download url (Path.Combine(dir, url.Segments |> Array.last))

let main (args: string[]) =
    match args with
    | [| "fetch"; release; directory |]   -> fetch release directory
    | [| "replace"; release; directory |] -> replace release directory
    | _ -> printfn "upload-signed-builds { fetch | replace } [release json] [directory] }"
    0

#if INTERACTIVE
main [|"hello"; "world"|]
#else
[<EntryPoint>]
let entrypoint args = main args; 0
#endif