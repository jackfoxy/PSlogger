namespace PSlogger

open System
open Microsoft.WindowsAzure.Storage.Table
open System.Threading.Tasks

type PredicateOperator =
    | EQ
    | GT
    | LT
    | Between

type LogLevel =
    | Error 
    | ErrorException
    | FatalException
    | Debug
    | Info 
    | Warning 
  with
    static member Parse : severity : string -> LogLevel
    static member All : LogLevel list

type Predicate =
    {
    Operator : PredicateOperator
    StartDate : DateTime
    EndDate : DateTime option
    Caller : string option
    LogLevels : LogLevel list
    }
    with
        static member Create : operator : PredicateOperator -> startDate : DateTime -> endDate : DateTime option -> caller : string option -> logLevels : LogLevel list -> Predicate

/// log message and supporting data
[<CustomEquality; CustomComparison>]
type Log =
    { 
    /// program calling the logger
    Caller : string 
    /// UTC row key YYYY-MM-DD HH:MM:SS.mmm.ticks + Guid (time is associated with process/program execution)
    UtcRunTime : DateTime
    /// overrides UtcRunTime as row key, must be unique within caller
    Counter : int
    /// UtcTime of the message generation
    UtcTime : DateTime
    /// optional local server time
    Level : LogLevel
    /// user defined
    Message : string
    AssembliesOrVersion : string
    MachineName : string
    /// database in use during message generation
    Process : string option
    /// user defined
    ByteInfo : byte [] option
    /// user defined
    StringInfo : string option
    Exception : System.Exception option
    /// exception in string format (optional)
    ExceptionString : string option
    }
  with
    interface IComparable

type CountingLog =
    new : caller : string * utcRunTime : DateTime * level : LogLevel * assemblyOrVersion : string *  machineName : string * processName : string option -> CountingLog
    member Log : Log

module IO =
    /// should not have to make this type public
    /// workaround for https://github.com/Azure/azure-storage-net/issues/523
    [<Class>]
    type InternalLog =
      inherit TableEntity
      new : log:Log -> InternalLog
      member AssembliesOrVersion : string with get, set
      member ByteInfo : byte [] with get, set
      member Counter : int with get, set
      member Exception : byte [] with get, set
      member ExceptionString : string  with get, set
      member Level : string with get, set
      member MachineName : string with get, set
      member Message : string with get, set
      member Process : string with get, set
      member StringInfo : string  with get, set
      member UtcTime : System.DateTime with get, set
   
    val insert : azureConnectionString : string -> log : Log -> logNamePrefix : string -> unit

    val insertAsync : azureConnectionString : string -> log : Log -> logNamePrefix : string -> Task<TableResult>

    val list : predicate : Predicate -> azureConnectionString : string -> logNamePrefix : string -> Log seq

    val purgeBeforeDaysBack : daysToPurgeBack : int -> azureConnectionString : string -> logNamePrefix : string -> int

