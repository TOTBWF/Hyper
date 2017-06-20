open System
open FSharp.Data.Hyper
open FSharp.Control

[<EntryPoint>]
let main args =
    Request.create "http://www.google.com"
    |> Request.execute
    |> Future.complete(function
        | Success r -> printfn "Success: %s" r.Body
        | FailedWith e -> printfn "Failed: %s" e.Message)
    0