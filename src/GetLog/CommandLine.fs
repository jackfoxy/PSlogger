namespace GetLog

open PSlogger
open Argu
open Prelude
open System


module CommandLine = 

    type ParsedCommand =
        {
        Usage : string
        Predicate : Predicate option
        Error: Exception option
        }

    type CLIArguments =
        | [<AltCommandLine("-eq"); Unique>] EQ of string
        | [<AltCommandLine("-gt"); Unique>] GT of string
        | [<AltCommandLine("-lt"); Unique>] LT of string
        | [<AltCommandLine("-btwn"); Unique>] Between of string * string
        | [<AltCommandLine("-c"); Unique>] Caller of string
        | [<AltCommandLine("-l"); Unique>] LogLevels of string list
  
         with
            interface IArgParserTemplate with
                member s.Usage =
                    match s with
                    | EQ _ -> "equals datetime, e.g. \"08/23/2017 14:57:32\" or 08/23/2017"
                    | GT _ -> "greater than datetime"
                    | LT _ -> "less than datetime"
                    | Between _ -> "between datetimes, comma separated"
                    | Caller _ -> "calling program"
                    | LogLevels _ -> "log level list, comma separated"

    let parseCommandLine programName (argv : string []) = 
        try
            match argv, argv.Length with
            | _, 0 -> 
                Result.Error (invalidArg "no arguments" "")
            | help, 1  when help.[0].ToLower() = "--help" ->
                Result.Error (invalidArg "" "")
            | _, _ ->
                let parser = 
                    ArgumentParser.Create<CLIArguments>(programName = programName)

                let commandLine = parser.Parse argv
                let usage = parser.PrintUsage()

                Ok (commandLine, usage)
        with e ->
            Result.Error e

    let getcaller (commandLine : ParseResults<CLIArguments>) =

        try
            Ok <| commandLine.TryGetResult <@ Caller @>
        with e ->
            Result.Error e
        
    let getLogLevels (commandLine : ParseResults<CLIArguments>) = 
        
        try
            match commandLine.TryGetResult <@ LogLevels @> with
            | Some x ->
                x
                |> List.toArray
                |> Array.map LogLevel.Parse
                |> Ok 
            | None -> Ok LogLevel.All
        with e ->
            Result.Error e

    let parseDate msg (date : string) =
        let d = date.Replace("\'", "").Replace("\"", "")
        match DateTime.TryParse d with
        | Some dt -> dt 
        | _ -> 
            match DateTime.TryParse (sprintf "%s 0:0:0" d) with
            | Some dt -> dt 
            | _ -> 
                invalidArg msg date

    let getEq (commandLine : ParseResults<CLIArguments>) =
        try
            match commandLine.TryGetResult <@ EQ @> with
            | Some dateTime ->
                {
                Operator = PredicateOperator.EQ
                StartDate = parseDate "error parsing EQ date" dateTime
                EndDate = None
                Caller = None
                LogLevels = [||]
                } |> Some |> Ok
            | None -> Ok None
            
        with e ->
            Result.Error e

    let getGt (commandLine : ParseResults<CLIArguments>) =
        try
            match commandLine.TryGetResult <@ GT @> with
            | Some dateTime ->
                {
                Operator = PredicateOperator.GT
                StartDate = parseDate "error parsing GT date" dateTime
                EndDate = None
                Caller = None
                LogLevels = [||]
                } |> Some |> Ok
            | None -> Ok None
            
        with e ->
            Result.Error e

    let getLt (commandLine : ParseResults<CLIArguments>) =
        try
            match commandLine.TryGetResult <@ LT @> with
            | Some dateTime ->
                {
                Operator = PredicateOperator.LT
                StartDate = parseDate "error parsing LT date" dateTime
                EndDate = None
                Caller = None
                LogLevels = [||]
                } |> Some |> Ok
            | None -> Ok None
            
        with e ->
            Result.Error e

    let getBetween (commandLine : ParseResults<CLIArguments>) =
        try

            let x = commandLine.TryGetResult <@ Between @>

            match commandLine.TryGetResult <@ Between @> with
            | Some (dateTime1, dateTime2) ->
                let dt1 = parseDate "error parsing Between first date" dateTime1
                let dt2 = parseDate "error parsing Between 2nd date" dateTime2

                if dt2 > dt1 then
                    {
                    Operator = PredicateOperator.Between
                    StartDate = dt1
                    EndDate = Some dt2
                    Caller = None
                    LogLevels = [||]
                    } |> Some |> Ok
                else invalidArg "start date not less than end date" ""
            | None -> Ok None
            
        with e ->
            Result.Error e

    let mergePredicate eq gt lt between logLevels caller =
        try
            match ([eq; gt; lt; between] |> List.choose id) with
            | [predicate] ->
                {predicate with Caller = caller; LogLevels = logLevels}
                |> Ok
            | hd::tl ->
                sprintf "%s, %s" (hd.Operator.ToString()) (tl.Head.Operator.ToString())
                |> invalidArg "multiple predicates selected" 
            | _ -> invalidArg "no predicate selected" "EQ, GT, LT, Between"
        with e ->
            Result.Error e

    let parse programName argv = 

        match choose { 
                        let! commandLine, usage = parseCommandLine programName argv

                        let! caller = getcaller commandLine
                        let! logLevels = getLogLevels commandLine
                        let! eq = getEq commandLine
                        let! gt = getGt commandLine
                        let! lt = getLt commandLine
                        let! between = getBetween commandLine
                        let! mergedPredicate = 
                            mergePredicate eq gt lt between logLevels caller
                        
                        return 
                            {
                            Usage = usage
                            Predicate = Some mergedPredicate
                            Error = None
                            } 
                        } with
        | Ok x -> x
        | Result.Error (e : Exception) -> 
            let usage = ArgumentParser.Create<CLIArguments>(programName = programName).PrintUsage()
            {
            Usage = usage
            Predicate = None
            ParsedCommand.Error = Some e
            } 
