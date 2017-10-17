namespace GetLog

open System
open System.Configuration
open PSlogger

module Console =

    let mutable cont = true
    let mutable (input : string []) = [||] 

    let processInput () =

        if input.Length > 0 && input.[0].ToLower() = "q" then cont <- false
        else
            let parsedCommand = CommandLine.parse "GetLog" input 

            match parsedCommand.Error with
            | Some e -> 
                printfn "%s" e.Message
                printfn "%s" parsedCommand.Usage
            | None -> 
                let azureConnectionString = ConfigurationManager.ConnectionStrings.["AzureStorage"].ConnectionString
                let logs = IO.list parsedCommand.Predicate.Value azureConnectionString "logs"
            
                logs
                |> Seq.iter (fun x -> 
                    let errorMsg =
                        match x.Exception with
                        | Some e -> e.Message
                        | None -> String.Empty

                    let line = 
                            sprintf "%s | %O | %O | %O | %s | %s | %s" 
                                x.Caller x.UtcRunTime x.Level x.UtcTime (defaultArg x.Process "") x.Message errorMsg
                    printfn "%s" line)

                if parsedCommand.Predicate.Value.Operator = PredicateOperator.EQ
                    && Seq.length logs > 0
                    && (Seq.head logs).Exception.IsSome then
                        printfn ""
                        printfn "%s" <|Common.formatExceptionDisplay (Seq.head logs).Exception.Value
                else
                    ()
            printfn ""
            printfn "(q and enter twice to exit)"
            cont <- true

    [<EntryPoint>]
    let main argv = 
        input <- argv

        let readLine =
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

        while cont do
            processInput ()
            input <- readLine

        printfn "%s" "good-bye"
        0