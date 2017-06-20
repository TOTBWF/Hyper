namespace FSharp.Data.Hyper

open System
open System.IO
open System.Text
open System.Net
open FSharp.Control

type ContentType = 
    {
        Type: string
        SubType: string
    }
    with 
        override x.ToString() =
            x.Type + "/" + x.SubType


type Method =
    | Get
    | Post
    | Put
    | Delete
    | Patch

type RequestHeader =
    | Accept of string
    | Authorization of string
    | ContentType of ContentType
    | Custom of string * string

type Request = {
    Url: Uri
    Method : Method option
    Headers: RequestHeader list
    Body: string option
}

type Response = {
    Body: string
    Status: int
    StatusText: string
}

module internal Helpers =
    
    let getMethodString request =
        match request.Method with
        | None -> "GET"
        | Some Get -> "GET"
        | Some Post -> "POST"
        | Some Put -> "PUT"
        | Some Delete -> "DELETE"
        | Some Patch -> "PATCH"
    
    let serializeHeader = function
        | Accept v -> "Accept", v
        | Authorization v -> "Authorization", v
        | ContentType v -> "Content-Type", v.ToString()
        | Custom (k, v) -> (k, v)
    
    let serializeBody (wr: WebRequest) (request: Request) =
        match request.Body with
        | Some body ->
            future {
                let! stream = wr.GetRequestStreamAsync() |> Future.ofTask
                use writer = new StreamWriter(stream, Encoding.UTF8)
                do! writer.WriteAsync(body) |> Future.ofUnitTask
                return wr
            } 
        | None -> future { return wr }

    let toWebRequest request =
        future {
            let wr = WebRequest.Create(request.Url)
            // For whatever reason, netstandard doesn't have WebHeaderCollection.Add
            let headers = 
                request.Headers
                |> List.fold(fun (acc: WebHeaderCollection) h -> 
                    let k, v = serializeHeader h
                    acc.[k] <- v
                    acc) wr.Headers
            wr.Method <- getMethodString request
            wr.Headers <- headers
            return! serializeBody wr request
        }
    
    let toResponse (wr: WebResponse) =
        let httpwr = wr :?> HttpWebResponse
        use reader = new StreamReader(httpwr.GetResponseStream(), Encoding.UTF8)
        future {
            let! body = Future.ofTask (reader.ReadToEndAsync())
            let status = int(httpwr.StatusCode)
            let statusText = httpwr.StatusDescription
            return { Body = body; Status = status; StatusText = statusText }
        }

module Request =
    let create url =
        { 
            Url = (Uri url)
            Method = None 
            Headers = []
            Body = None
        }
    
    let withMethod method request =
        { request with Method = Some method }

    let withHeader header request =
        { request with Headers = header::request.Headers }

    let withHeaders headers request =
        { request with Headers = headers@request.Headers }
    
    let withBody body (request: Request) =
        { request with Body = Some body }
    
    let execute request = 
        future {
            let! wr = Helpers.toWebRequest request
            let success (wr:WebResponse) = downcast wr |> Helpers.toResponse 
            let! res = 
                async {
                     let! c = wr.AsyncGetResponse() |> Async.Catch
                     return c |> Try.ofChoice
                } |> Future.ofAsyncTry
            return! Helpers.toResponse res
        }

        