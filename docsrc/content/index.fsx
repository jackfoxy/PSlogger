(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../src/PSlogger/bin/Debug"
#r "../../Packages/WindowsAzure.Storage/lib/net45/Microsoft.WindowsAzure.Storage.dll"
#r "PSlogger.dll"

(**
PSlogger
======================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The PSlogger library can be <a href="https://nuget.org/packages/PSlogger">installed from NuGet</a>:
      <pre>PM> Install-Package PSlogger</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

The logger is a strongly typed system for asynchronous and synchronous logging of program messages from .NET programs to an Azure Table Storage data store. 

Optimized for message retrieval by software execution
-----------------------------------------------------

It is optimized for a particular style of message logging. Specifically it is convenient for querying all messages for an arbitrary "run" of 
software. For example:

* a single request/response cycle

* a specific run of a batch program

It accomplished this by using the software assembly identifier (caller) as the partition key, and one timestamp (associated with the run) as the
partial row key. Data is organized by daily tables. All of messages for a given run reside in the same daily table, regardless of when they were 
actually generated.

Message retrieval by caller is also generaly fast, but due to the log deletion optimization retrieval over an excessive number of days will result
in performance degradation, even to the point of unusable. Generally this is not a useful scenario. The logger is optimized for more common usage.

The repo solution includes the `GetLog` console app with examples of message retrieval. The [`Predicate`](reference/PSlogger-predicate.html) type 
drives queries by run time.

* ``Operator`` EQ, GT, LT, Between

* ``StartDate`` run time

* ``EndDate`` only used for Between

* ``Caller`` identifier of logging software assembly

* ``LogLevels`` list of requested log levels, empty list for all

Guaranteed ordering, even with async inserts
--------------------------------------------

The calling software can either manage the log counter, or take advantage of the `CountingLog` type which will manage the counter. This assures 
messages can be ordered by their temporal generation, even when inserted asynchronously.

Optimized for old message deletion
----------------------------------

Storage may be cheap, but it is not free, and table row deletion is slow. Physical storage organization by day allows the `purgeBeforeDaysBack`
function to simply drop tables, a much more efficient operation.

Suggested Usage
---------------

The logger records messages and data from program runs (transactions, batch runs, etc.) to determine program health, timely exectution, 
verify processing, and record exceptions.

In general enough information should be logged so that a reasonable query strategy on the data store can determine:

* Is the process alive. (Is it executing on schedule, performing intended processes, and creating expected outputs?)

* The process has not thrown errors. (Trap exceptions and record them in the log with enough supporting information to make problems transparent.)

To facilitate complete logging, follow this protocol:

* ``Caller`` identifies the program.

* ``UtcRunTime`` is the process start time for each message recorded in an entire run of a program (or request/response).

* ``UtcTime`` records the time of each message generated.

* ``Counter`` It is possible to generate messages in rapid succession with asyncronous inserts so that ``UtcTime`` does not increment. There is no 
guarantee of the order in which messages post. It is the user's responsibility to increment ``Counter`` for correct message ordering. 
Use the ``CountingLog`` type as a log template for the duration of the program to automatically increment the ``Counter`` with each message.

* ``Message`` is the primary logged message.

* ``AssembliesOrVersion``

* ``MachineName``

* ``Process`` optional, process within program generating the message

* ``StringInfo`` optional, any custom data to further specify program state.

* ``ByteInfo`` optional, any custom data to further specify program state.

* ``Exception`` optional, System.Exception

* ``ExceptionString`` optional, exception in string format, e.g. for logging from REST interface

Examples
--------

Initializing a ``CountingLog`` template to generate messages.

*)
open System
open PSlogger
open Microsoft.WindowsAzure.Storage.Table

let initLog processName =

    let utcTime = DateTime.UtcNow

    let assemblyFullName =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        assembly.FullName

    CountingLog("Dashboard", utcTime, LogLevel.Info, assemblyFullName, Environment.MachineName, (Some processName))
(**
Writing messages.
*)
let logMessage (initLog : CountingLog) connString company curretnProcess message addlInfo  =
    
    IO.insertAsync connString {initLog.Log with 
                                UtcTime = DateTime.UtcNow;
                                Process = curretnProcess
                                Message = message
                                StringInfo = addlInfo
                                } "MyLogPrefix"
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore
(**
Logging exceptions.
*)
let logException (initLog : CountingLog) connString (exn : Exception) currentRecord  = 
    
    IO.insert connString {initLog.Log with 
                            UtcTime = DateTime.UtcNow;
                            Level = LogLevel.ErrorException
                            Message = exn.Message
                            Exception = Some exn
                            StringInfo = Some currentRecord
                            }  "MyLogPrefix"
(**
Samples & documentation
-----------------------

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. 
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/PSlogger/tree/master/docs/content
  [gh]: https://github.com/fsprojects/PSlogger
  [issues]: https://github.com/fsprojects/PSlogger/issues
  [readme]: https://github.com/fsprojects/PSlogger/blob/master/README.md
  [license]: https://github.com/fsprojects/PSlogger/blob/master/LICENSE.txt
*)
