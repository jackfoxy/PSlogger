module PSlogger

open System
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open MBrace.FsPickler

type LogLevel =
    | Error 
    | ErrorException
    | FatalException
    | Debug
    | Info 
    | Warning 
    override __.ToString() =
        match __ with
        | Error -> "ERROR"
        | ErrorException -> "ErrorException"
        | FatalException -> "FatalException"
        | Debug -> "DEBUG"
        | Info -> "INFO"
        | Warning -> "WARNING"
    static member Parse (severity : string) =
        match severity.ToLower() with
        | x when x = "error" -> LogLevel.Error
        | x when x = "errorexception" -> LogLevel.ErrorException
        | x when x = "fatalexception" -> LogLevel.FatalException
        | x when x = "debug" -> LogLevel.Debug
        | x when x = "info" -> LogLevel.Info
        | x when x = "warning" -> LogLevel.Warning
        | _ -> invalidArg "LogLevel Parse" (sprintf "cannot parse %s" severity)
    static member All =
        [| 
            LogLevel.Error
            LogLevel.ErrorException
            LogLevel.FatalException
            LogLevel.Debug
            LogLevel.Info
            LogLevel.Warning
        |]

type PredicateOperator =
    | EQ
    | GT
    | LT
    | Between

    override __.ToString() =
        match __ with
        | EQ -> "eq"
        | GT -> "gt"
        | LT -> "lt"
        | Between -> "between"

type Predicate =
    {
    Operator : PredicateOperator
    StartDate : DateTime
    EndDate : DateTime option
    Caller : string option
    LogLevels : LogLevel []
    }
    static member Create operator startDate endDate caller logLevels =
        match endDate with
        | Some _x ->
            match operator with
            | EQ
            | GT
            | LT -> 
                sprintf "operator is %O and end date is requested" operator
                |> invalidArg "Log Predicate Create"
            | Between ->
                {
                Operator = operator
                StartDate = startDate
                EndDate = endDate
                Caller = caller
                LogLevels = logLevels
                }

        | None -> 
            match operator with
            | EQ
            | GT
            | LT -> 
                {
                Operator = operator
                StartDate = startDate
                EndDate = endDate
                Caller = caller
                LogLevels = logLevels
                }
            | Between ->
                sprintf "operator is %O and no end date is requested" operator
                |> invalidArg "Log Predicate Create"

[<CustomEquality; CustomComparison>]
type Log =
    { 
    Caller : string
    UtcRunTime : DateTime
    Counter : int
    UtcTime : DateTime
    Level : LogLevel
    Message : string
    AssembliesOrVersion : string
    MachineName : string
    Process : string option
    ByteInfo : byte [] option
    StringInfo : string option
    Exception : System.Exception option
    ExceptionString : string option
    }
    override __.Equals yobj =
        match yobj with
        | :? Log as y ->
            if (yobj.GetHashCode()) = (__.GetHashCode()) then    
                __.AssembliesOrVersion = y.AssembliesOrVersion
                && __.MachineName = y.MachineName 
                && __.Process = y.Process
                && __.ByteInfo = y.ByteInfo
                && __.StringInfo = y.StringInfo
                && __.Exception = y.Exception
                && __.ExceptionString = y.ExceptionString
            else false
        | _ -> invalidArg "Log" "cannot compare values of different types"

    override __.GetHashCode() = 
        hash  (__.Caller + __.UtcRunTime.ToString() + __.Counter.ToString() + __.UtcTime.ToLongDateString() + __.Level.ToString() + __.Message)

    interface System.IComparable with
        member __.CompareTo yobj = 
            match yobj with
            | :? Log as y ->
                if __.Caller > y.Caller then 1
                else
                    if __.Caller < y.Caller then -1
                    else
                        if __.UtcRunTime > y.UtcRunTime then 1
                        else 
                            if __.UtcRunTime < y.UtcRunTime then -1
                            else
                                if __.Counter > y.Counter then 1
                                else 
                                    -1

            | _ -> invalidArg "Log" "cannot compare values of different types"

type CountingLog(caller, utcRunTime, level, assemblyOrVersion, machineName, processName) =
    let mutable count = 0
    let mutable log : Log =
        { 
        Caller = caller
        UtcRunTime = DateTime.UtcNow
        Counter = 0
        UtcTime = DateTime.UtcNow
        Level = level
        Message = ""
        AssembliesOrVersion = ""
        MachineName = "" 
        Process = None
        ByteInfo = None
        StringInfo = None
        Exception = None
        ExceptionString = None
        }

    do
        log <-
            { 
            Caller = caller
            UtcRunTime = utcRunTime
            Counter = 0
            UtcTime = utcRunTime
            Level = level
            Message = ""
            AssembliesOrVersion = assemblyOrVersion
            MachineName = machineName 
            Process = processName
            ByteInfo = None
            StringInfo = None
            Exception = None
            ExceptionString = None
            }
        
    member __.Log =
        let log = {log with Counter = count}
        count <- count + 1
        log

type ClientTable =
    {
    TableClient : CloudTableClient
    TableName : string
    }

exception LogInsertException of string

let exceptionSerialize (exc : System.Exception option) =
    match exc with
    | Some x ->
        let binary = FsPickler.CreateBinarySerializer()
        binary.Pickle x
    | None -> [|new Byte()|]

let exceptionDeserialize (exc : byte []) =
    if exc.Length = 1 then None
    else 
        let binary = FsPickler.CreateBinarySerializer()
        Some (binary.UnPickle<Exception> exc)

let dateTimeString (dateTime : DateTime) =
    sprintf "%s-%s-%s %s:%s:%s.%s"
        (dateTime.Year.ToString())
        (dateTime.Month.ToString().PadLeft(2, '0'))
        (dateTime.Day.ToString().PadLeft(2, '0')) 
        (dateTime.Hour.ToString().PadLeft(2, '0'))
        (dateTime.Minute.ToString().PadLeft(2, '0'))
        (dateTime.Second.ToString().PadLeft(2, '0'))
        (dateTime.Millisecond.ToString().PadLeft(3, '0'))

let dateTimeStringToDateTime (s : string) =
    let s1 = s.Split ' '
    let sMajor = s1.[0].Split '-'
    let sMinor = s1.[1].Split ':'
    let sVeryMinor = sMinor.[2].Split '.'

    let year = int sMajor.[0]
    let month = int sMajor.[1]
    let day = int sMajor.[2]
    let hour = int sMinor.[0]
    let minute = int sMinor.[1]
    let second = int sVeryMinor.[0]
    let millisecond = int sVeryMinor.[1]

    DateTime(year, month, day, hour, minute, second, millisecond)

[<Class>]
type InternalLog (log : Log) =
    inherit TableEntity(partitionKey = log.Caller, rowKey = dateTimeString log.UtcRunTime)
    member val Counter = log.Counter with get, set
    member val UtcTime = log.UtcTime  with get, set
    member val Level = log.Level.ToString()  with get, set
    member val Message = log.Message  with get, set
    member val AssembliesOrVersion = log.AssembliesOrVersion  with get, set
    member val MachineName = log.MachineName  with get, set
    member val Process = 
        match log.Process with
        | Some x -> x
        | None -> null
        with get, set
    member val ByteInfo = 
        match log.ByteInfo with
        | Some x -> x
        | None -> [|new Byte()|] 
        with get, set
    member val StringInfo = 
        match log.StringInfo with
        | Some x -> x
        | None -> null
        with get, set
    member val Exception = exceptionSerialize log.Exception  with get, set
    member val ExceptionString = 
        match log.ExceptionString with
        | Some x -> x
        | None -> null
        with get, set

let tableNameFromTime logNamePrefix (time : DateTime) =
    sprintf "%s%i%s%s" logNamePrefix time.Year (time.Month.ToString().PadLeft(2, '0')) (time.Day.ToString().PadLeft(2, '0'))
        
let getClientTable (log : Log) azureConnectionString logNamePrefix =
    let account = CloudStorageAccount.Parse azureConnectionString 
    let tableClient  = account.CreateCloudTableClient()

    let tableName = tableNameFromTime logNamePrefix log.UtcRunTime
            
    let table = tableClient.GetTableReference(tableName)
    table.CreateIfNotExistsAsync().Result |> ignore

    table

let goodInsert log (result : TableResult) = 
    match result.HttpStatusCode with
    | 201 -> ()
    | 204 -> ()
    | n -> 
        raise (LogInsertException (sprintf "Insert into Partition : %s Row : %s HttpCode : %i" log.Caller (dateTimeString log.UtcRunTime) n))
 
let insert azureConnectionString log logNamePrefix =
    let table = getClientTable log azureConnectionString logNamePrefix

    let internalLog = InternalLog(log)
    let insertOp = TableOperation.Insert(internalLog)

    table.ExecuteAsync(insertOp).Result
    |> goodInsert log

let insertAsync azureConnectionString log logNamePrefix =
    let table = getClientTable log azureConnectionString logNamePrefix

    let internalLog = InternalLog(log)
    let insertOp = TableOperation.Insert(internalLog)

    table.ExecuteAsync(insertOp)

let getTablesForPredicate (tableClient : CloudTableClient) predicate logNamePrefix = 
    let startTableName = tableNameFromTime logNamePrefix predicate.StartDate

    let mutable token = new TableContinuationToken()

    let storageAccountTables = 
        [|
            while token <> null do
                let tables = tableClient.ListTablesSegmentedAsync(token).Result
                token <- tables.ContinuationToken
                for x in tables.Results do
                    x
        |]

    let filterRows f =
        storageAccountTables
        |> Array.filter (fun table -> 
            f table.Name startTableName
            )

    match predicate.Operator with
    | EQ -> 
        filterRows (=)

    | LT -> 
        filterRows (<=)

    | GT -> 
        filterRows (>=)

    | Between ->
        let endTableName = tableNameFromTime logNamePrefix predicate.EndDate.Value

        storageAccountTables
        |> Array.filter (fun table -> 
            (table.Name >= startTableName) && (table.Name <= endTableName)
            )
    |> Array.map (fun table -> table.Name)
    |> Array.sort

let timeFilter predicate =
    let filterHighLow stringStartDate stringEndDate =
        TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition(
                "RowKey", QueryComparisons.GreaterThanOrEqual, stringStartDate),
            TableOperators.And,
            TableQuery.GenerateFilterCondition(
                "RowKey", QueryComparisons.LessThanOrEqual, stringEndDate) )

    let filterOneDate comparison stringDate =
        TableQuery.GenerateFilterCondition("RowKey", comparison, stringDate)

    match predicate.Operator with
    | EQ ->
        let startDate = dateTimeString predicate.StartDate
        let stringStartDate, stringEndDate = 
            if startDate.EndsWith("00:00:00.000") then
                startDate.Replace(" 00:00:00.000", ""), (dateTimeString (predicate.StartDate.AddDays(1.))).Replace(" 00:00:00.000", "")
            else
                startDate, (dateTimeString (predicate.StartDate.AddSeconds(1.)))
        filterHighLow stringStartDate stringEndDate

    | LT ->
        filterOneDate QueryComparisons.LessThan <| dateTimeString predicate.StartDate

    | GT ->
        filterOneDate QueryComparisons.GreaterThan <| dateTimeString predicate.StartDate

    | Between ->
        filterHighLow (dateTimeString predicate.StartDate) (dateTimeString predicate.EndDate.Value)

let inline toOption x  = 
    match x with
    | true, v    -> Some v
    | _   -> None

let parseLogLevel (dynamicTableEntity : DynamicTableEntity) =
    match toOption <| dynamicTableEntity.Properties.TryGetValue("Level") with
    | Some x -> x.StringValue
    | None -> invalidArg "dynamicTableEntityToLogInternal" "Level"

let dynamicTableEntityToLogInternal (dynamicTableEntity : DynamicTableEntity) =
    let counter =
        match toOption <| dynamicTableEntity.Properties.TryGetValue("Counter") with
        | Some x -> 
            if x.Int32Value.HasValue then
                x.Int32Value.Value
            else
                invalidArg "dynamicTableEntityToLogInternal" "Counter"
        | None -> invalidArg "dynamicTableEntityToLogInternal" "Counter"
    let utcTime =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("UtcTime") with
        | Some x -> 
            if  x.DateTime.HasValue then 
                x.DateTime.Value
            else
                invalidArg "dynamicTableEntityToLogInternal" "UtcTime"
        | None -> invalidArg "dynamicTableEntityToLogInternal" "UtcTime"
    let level = parseLogLevel dynamicTableEntity
    let message =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("Message") with
        | Some x -> x.StringValue
        | None -> invalidArg "dynamicTableEntityToLogInternal" "Message"
    let assembliesOrVersion =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("AssembliesOrVersion") with
        | Some x -> x.StringValue
        | None -> invalidArg "dynamicTableEntityToLogInternal" "AssembliesOrVersion"
    let machineName =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("MachineName") with
        | Some x -> x.StringValue
        | None -> invalidArg "dynamicTableEntityToLogInternal" "MachineName"
    let process' =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("Process") with
        | Some x -> Some x.StringValue
        | None -> None
    let byteInfo =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("ByteInfo") with
        | Some x -> x.BinaryValue
        | None -> invalidArg "dynamicTableEntityToLogInternal" "ByteInfo"
    let stringInfo =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("StringInfo") with
        | Some x -> Some x.StringValue
        | None -> None
    let exception' =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("Exception") with
        | Some x -> x.BinaryValue
        | None -> invalidArg "dynamicTableEntityToLogInternal" "Exception"
    let exceptionString =  
        match toOption <| dynamicTableEntity.Properties.TryGetValue("ExceptionString") with
        | Some x -> Some x.StringValue
        | None -> None

    { 
    Caller = dynamicTableEntity.PartitionKey
    UtcRunTime = dateTimeStringToDateTime dynamicTableEntity.RowKey
    UtcTime = utcTime
    Counter = counter
    Level = LogLevel.Parse level
    Message = message
    AssembliesOrVersion = assembliesOrVersion
    MachineName = machineName
    Process = process'
    ByteInfo =
        match byteInfo with
        | [|0uy|] -> None
        | _ -> Some byteInfo
    StringInfo = stringInfo
    Exception = exceptionDeserialize exception'
    ExceptionString = exceptionString
    }

let listLogsOneDay (tableClient : CloudTableClient) tableName (tableStorageQuery : TableQuery) (predicate : Predicate) =
    let table = tableClient.GetTableReference(tableName)
    let mutable token = new TableContinuationToken()

    let tableRows =
        [|
            while token <> null do
                let rows = table.ExecuteQuerySegmentedAsync(tableStorageQuery, token).Result
                token <- rows.ContinuationToken
                for x in rows.Results do
                    x
        |]

    if table.ExistsAsync().Result then
        if predicate.LogLevels.Length = 0 then
            tableRows
            |> Array.map dynamicTableEntityToLogInternal
        else
            tableRows
            |> Array.filter (fun t -> 
                    Array.contains (LogLevel.Parse (parseLogLevel t)) predicate.LogLevels)
            |> Array.map dynamicTableEntityToLogInternal
    else
        Array.empty
        
let list (predicate : Predicate) azureConnectionString logNamePrefix =
    let account = CloudStorageAccount.Parse azureConnectionString 
    let tableClient  = account.CreateCloudTableClient()

    let tableStorageQuery =
        TableQuery().Where(
            match predicate.Caller with
            | Some x ->
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, x),
                    TableOperators.And,
                    (timeFilter predicate)
                    )
            | None ->
                timeFilter predicate
            )

    getTablesForPredicate tableClient predicate logNamePrefix
    |> Seq.collect (fun tableName -> 
        listLogsOneDay tableClient tableName tableStorageQuery predicate
        )
    |> Seq.sortBy (fun t -> t.Caller, t.UtcTime, t.Counter)

let purgeBeforeDaysBack daysToPurgeBack azureConnectionString logNamePrefix =
    let account = CloudStorageAccount.Parse azureConnectionString 
    let tableClient  = account.CreateCloudTableClient()

    let purgeTableName = 
        DateTime.UtcNow.AddDays((float daysToPurgeBack) * -1.)
        |> tableNameFromTime logNamePrefix

    let mutable token = new TableContinuationToken()

    let storageAccountTables = 
        [|
            while token <> null do
                let tables = tableClient.ListTablesSegmentedAsync(logNamePrefix, token).Result
                token <- tables.ContinuationToken
                for x in tables.Results do
                    x
        |]


    storageAccountTables 
    |> Array.ofSeq
    |> Array.fold (fun s (table : Table.CloudTable) ->
        if table.Name < purgeTableName then
            table.DeleteIfExistsAsync() |> ignore
            s + 1
        else
            s
        ) 0
 