namespace GetLog

open System
open System.Configuration
open PSlogger
//open Microsoft.FSharp.Quotations
//open QuotationCompiler
  

module console1 =

    let mutable cont = true
    let mutable (input : string []) = [||] 

//    let exprRaw = <@fun x -> x |> List.map (fun x -> x.Company) |> List.choose id |> List.distinct @>
    //let exprRaw = <@fun x -> x |> List.map (fun x -> x.MachineName) |> List.distinct @>
    
    //let f : unit -> Log list -> string list = QuotationCompiler.ToFunc exprRaw
   
    let processInput () =

        if input.Length > 0 && input.[0].ToLower() = "q" then cont <- false
        else
            let parsedCommand = CommandLine.parse "GetLog" input 

            match parsedCommand.Error with
            | Some e -> 
                printfn "%s" e.Message
                printfn "%s" parsedCommand.Usage
            | None -> 
                let azureConnectionString = ConfigurationManager.ConnectionStrings.["DashboardLogsAzureStorage"].ConnectionString
                let logs = IO.list parsedCommand.Predicate.Value azureConnectionString "logs"
            
                logs
                |> Seq.iter (fun x -> 
                    let line = sprintf "%s %O %O %O %s %s" x.Caller x.UtcRunTime x.Level x.UtcTime (defaultArg x.Process "") x.Message
                    printfn "%s" line)
            cont <- true

    [<EntryPoint>]
    let main argv = 

        printfn "q and enter twice to exit"
        input <- argv

        while cont do
            processInput ()
            input <- 
                Console.ReadLine().Split ' '
                |> Array.fold (fun (quoted, ls) t ->
                    match quoted with
                    | Some quote ->
                        if t.EndsWith("\"") || t.EndsWith("\'") then
                            (None, (quote + " " + (t.Replace("\"", "").Replace("\'", "")))::ls)
                        else
                            ((Some (quote + " " + t)), ls)
                    | None -> 
                        if t.StartsWith("\"") then
                            ((Some (t.Replace("\"", "").Replace("\'", ""))), ls) 
                        else 
                            (None, (t::ls))) (None, [])
                |> snd
                |> List.rev
                |> Array.ofList
            printfn "%A" input


        printfn "Hit any key to exit."
        System.Console.ReadKey() |> ignore

        printf "%s" "good-bye"
        0