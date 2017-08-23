(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../src/PSlogger/bin/Debug"
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

The logger is for asynchronous and synchronous logging of program messages from F# programs to an Azure Table Storage data store. 

The data store is also available via Azure REST API.

Suggested Usage
---------------

The logger records messages and data from program runs (transactions) to determine program health, timely exectution, verify processing, 
and record exceptions.

In general enough information should be logged so that a reasonably query strategy on the data store can determine:

* Is the process alive. (Is it executing on schedule, performing intended processes, and creating expected outputs?)

* The process has not thrown errors. (Trap exceptions and record them in the log with enough supporting information to make problems transparent.)

To facilitate complete logging, follow this protocol:

* ``Caller`` identifies the program.

* ``UtcRunTime`` is the process start time for each message recorded in an entire run of a program (or request/response).

* ``UtcTime`` records the time of each message generated.

* ``Counter`` It is possible to generate messages in rapid succession so that ``UtcTime`` does not increment. Also with asyncronous writes there is no guarantee of the order in 
which messages post. It is the user's responsibility to increment ``Counter`` if message ordering is required. Use the ``CountingLog`` type as a log template for the duration of the program to automatically increment the ``Counter`` with each message.

* ``Message`` is the primary logged message.

* ``Process`` optional, process within program generating the message

* ``StringInfo`` optional, any custom data to further specify program state.

See the ``Log`` [reference doc](reference/index.html) for the full message format.

Examples
--------

Initializing a ``CountingLog`` template to generate messages.

*)
open System
open PSlogger

let initLog processName =

    let utcTime = DateTime.UtcNow

    let assemblyFullName =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        assembly.FullName

    CountingLog("Dashboard", utcTime, LogLevel.Info, assemblyFullName, Environment.MachineName, (Some processName))
(**
Writing messages.
*)
let logMessage (initLog : CountingLog) connString company curretnProcess message addlInfo  = async {
    
    do! AzureIO.insertAsync connString {initLog.Log with 
                                        UtcTime = DateTime.UtcNow;
                                        Process = curretnProcess
                                        Message = message
                                        StringInfo = addlInfo
                                        }
}
(**
Logging exceptions.
*)
let logException (initLog : CountingLog) connString (exn : Exception) currentRecord  = async {
    
    do! IO.insertAsync connString {initLog.Log with 
                                            UtcTime = DateTime.UtcNow;
                                            Level = LogLevel.ErrorException
                                            Message = exn.Message
                                            Exception = Some exn
                                            StringInfo = Some currentRecord
                                            }
}
(**
Alternate Usage
---------------

Optional ``StringInfo`` and ``ByteInfo`` for recording custom ``string`` and ``byte array`` information. 

Querying persisted messages includes specifying [predicates](reference/PSlogger-predicate.html).

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
