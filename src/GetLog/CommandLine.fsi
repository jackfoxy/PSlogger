namespace GetLog

open System
open PSlogger

module CommandLine = 
    
    type ParsedCommand =
        {
        Usage : string
        Predicate : Predicate option
        Error: Exception option
        }

    val parse : programName : string -> argv : string [] -> ParsedCommand
