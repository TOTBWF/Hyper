# Hyper
A simple HTTP Client with a functional flavor

## Usage ##
The get a web page, simply use
```fsharp
open FSharp.Data.Hyper
open FSharp.Control

Request.create "http://www.google.com"
|> Request.execute
|> Future.complete(fun res ->
  match res with
  | Success r -> printfn "Response: %s" r.Body
  | FailedWith e -> printfn "Oh No! Exception: %s" + e.Message)
```

Hyper uses the [Future](https://github.com/TOTBWF/FSharp.Control.Future) library, which is focused around
dealing with asynchronous programming in the face of failure.

