# PSlogger

The logger is a strongly typed system for asynchronous and synchronous logging of program messages from .NET programs to an Azure Table Storage data store, 
optimized for useful log querying.


# Logging


> The `RunTime` refers to the start time of a workflow. Logs are stored and retrieved using this `RunTime`, not the timestamp of the log message.
> Logs are stored in tables per each day.  All logs for a particular workflow are stored in the same table, regardless of how long the workflow takes. 

## Logging to Azure tables
To log to Azure tables, provide the following environment variables: 
- `"loggingType": "azure"`
- `"loggingConnectionString": "<connection string to Azure storage>"`

For local testing, log to the local Azure storage emulator:
- Start the Azure storage emulator locally
- Provide the environment variable: `"loggingConnectionString": "UseDevelopmentStorage=true"`

## Log Levels
- `Error`
- `ErrorException`
- `FatalException`
- `Debug`
- `Info`
- `Warning`

## Log DateTime Operators
All log DateTime operators are applied to the `RunTime`, i.e. the time when a workflow has begun. 
- `Between`: Retrieve logs with a `RunTime` between the `StartDate` and the `EndDate`. `EndDate` is required to use this operator.
- `EQ`: Retrieve logs with a `RunTime` equal to the `StartDate`.
- `LT`: Retrieve logs with a `RunTime` less than the `StartDate`.
- `GT`: Retrieve logs with a `RunTime` greater than the `StartDate`.

## Retrieving Logs
Retrieve logs via the endpoint `{url}/logs` by providing a predicate

| Parameter     | Required | Input               | Description     |
| ------------- | ---------| ------------------- | --------------- |
| `Customer`    | Yes      | String              | Customer for which to retrieve logs                                        |
| `Operator`    | Yes      | Log operator        | Specifies how to filter the logs over time                                 |
| `StartDate`   | Yes      | DateTime            | The RunTime to which the `Operator` is applied                             |
| `EndDate`     | No       | DateTime            | Required when `Operator` is `Between`                                      |
| `ProcessName` | No       | String              | Process (eg, "Optimizations.getFile bdaas-api") for which to retrieve logs |
| `LogLevels`   | No       | Array of log levels | Log levels for which to retrieve logs                                      |
