#if INTERACTIVE
#r "System.Xml.Linq"
#endif

#load ".paket/load/net45/fsharp.data.fsx"
open FSharp.Data
open FSharp.Data.JsonExtensions

#load "helpers.fsx"
open Helpers

open System
open System.IO
open System.Xml.Linq
open System.Xml.XPath

let ReleasesJsonDirectory = Path.Combine(HomeDirectory, "git", "xamarinhq", "releases")
let InstallerXmlDirectory = Path.Combine(HomeDirectory, "git", "xamarinhq", "updater-release-xml")

let insert (name: string) (value: string) (e: XElement) =
    let name = XName.Get name
    let element = e.Element name
    if isNull element then
        e.Add(XElement(name, value))
    else
        element.Value <- value
    e

let processProduct (product: JsonValue) (commit: string) (filename: string) =
    let guid  = product.["productGuid"].AsString()
    let id    = product.["releaseId"].AsString()

    if (id.Length = 0) then () else

    let xml    = Path.Combine(InstallerXmlDirectory, guid + ".xml")
    let doc    = XDocument.Parse(File.ReadAllText xml)
    let update = doc.XPathSelectElement(sprintf "/ApplicationInfo/Update[@id='%s']" id)

    if (isNull update) then () else

    printfn "Inserting %s %s into %s in %s" (commit.Substring(0, 8)) filename id xml
    update
        |> insert "ReleaseHash" commit
        |> insert "ReleaseName" filename
        |> ignore

    doc.Save(xml)

let processCommit commit filename =
    let output   = system ((sprintf "git show '%s:%s'" commit filename).Split(' ')) |> String.concat "\n"
    let data     = JsonValue.Parse output
    let release  = data.["release"].AsArray()
    let products = release |> Array.map (fun r -> r.TryGetProperty("updaterProduct"))

    printfn "Processing %s:%s" (commit.Substring(0, 8)) filename

    for p in products do
        match p with
        | Some(product) -> processProduct product commit filename
        | None -> ()
let main =
    Environment.CurrentDirectory <- ReleasesJsonDirectory
    let commits = system ("git log --reverse --pretty=tformat:'%H %s' 627dc925afb4fbf38e084e9ec3b9504a8d8c22a4..HEAD".Split(' '))

    for c in commits do
        match c with
        | Regex @"(\w{40}) New Release - (.+)$" [commit; filename] ->
            processCommit commit filename
        |_ -> ()



#if INTERACTIVE
#else
[<EntryPoint; STAThread>]
let entryPoint args = main; 0
#endif
