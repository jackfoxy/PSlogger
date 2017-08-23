
#load "load-project-debug.fsx"

open System
open Microsoft.WindowsAzure.Storage // Namespace for CloudStorageAccount
open Microsoft.WindowsAzure.Storage.Table // Namespace for Table storage types

// Parse the connection string and return a reference to the storage account.
let storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true")

// Create the table client.
let tableClient = storageAccount.CreateCloudTableClient()

// Retrieve a reference to the table.
let table = tableClient.GetTableReference("people")

// Create the table if it doesn't exist.
table.CreateIfNotExists()

type InternalLog (caller, utcRunTime, level : string) =
    inherit TableEntity(partitionKey = caller, rowKey = utcRunTime)
    new() = InternalLog(null, null, null)
    //member val Counter = counter with get, set
    member val Level = level  with get, set

let dateTimeString (dateTime : DateTime) =
    sprintf "%s-%s-%s %s:%s:%s.%s"
        (dateTime.Year.ToString())
        (dateTime.Month.ToString().PadLeft(2, '0'))
        (dateTime.Day.ToString().PadLeft(2, '0')) 
        (dateTime.Hour.ToString().PadLeft(2, '0'))
        (dateTime.Minute.ToString().PadLeft(2, '0'))
        (dateTime.Second.ToString().PadLeft(2, '0'))
        (dateTime.Millisecond.ToString().PadLeft(3, '0'))

let goodInsert log (result : TableResult) = 
        match result.HttpStatusCode with
        | 201 -> ()
        | 204 -> ()
        | n -> invalidArg "" ""
           

let internalLog = InternalLog("test", (dateTimeString DateTime.UtcNow), "INFO")
let insertOp = TableOperation.Insert(internalLog)

table.Execute(insertOp)
|> goodInsert log