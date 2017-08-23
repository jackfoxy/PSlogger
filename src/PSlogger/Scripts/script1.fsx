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

type Customer(firstName, lastName, email: string, phone: string) =
    inherit TableEntity(partitionKey=lastName, rowKey=firstName)
    new() = Customer(null, null, null, null)
    member val Email = email with get, set
    member val PhoneNumber = phone with get, set

let customer = 
    Customer("Walter", "jones", "Walter@contoso.com", "425-555-0101")

let insertOp = TableOperation.Insert(customer)
let x = table.Execute(insertOp)
x.HttpStatusCode