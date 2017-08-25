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
      member AssembliesOrVersion : string
      member ByteInfo : byte []
      member Counter : int
      member Exception : byte []
      member ExceptionString : string 
      member Level : string
      member MachineName : string
      member Message : string
      member Process : string
      member StringInfo : string 
      member UtcTime : System.DateTime
      member AssembliesOrVersion : string with set
      member ByteInfo : byte [] with set
      member Counter : int with set
      member Exception : byte [] with set
      member ExceptionString : string  with set
      member Level : string with set
      member MachineName : string with set
      member Message : string with set
      member Process : string with set
      member StringInfo : string  with set
      member UtcTime : System.DateTime with set
   
    val insert : azureConnectionString : string -> log : Log -> logNamePrefix : string -> unit

    val insertAsync : azureConnectionString : string -> log : Log -> logNamePrefix : string -> Task<TableResult>

    val list : predicate : Predicate -> azureConnectionString : string -> logNamePrefix : string -> Log seq

    val purgeBeforeDaysBack : daysToPurgeBack : int -> azureConnectionString : string -> logNamePrefix : string -> int

