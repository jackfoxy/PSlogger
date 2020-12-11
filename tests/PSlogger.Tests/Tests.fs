namespace PSlogger.Tests

open Expecto
open PSlogger
open System

module Tests =
    let azureConnectionString = "UseDevelopmentStorage=true"

    let log1 =
        { 
        Caller = "test"
        UtcRunTime = DateTime.UtcNow
        Counter = 0
        UtcTime = DateTime.UtcNow
        Level = LogLevel.Info
        Message = "test message"
        AssembliesOrVersion = "1.0"
        MachineName = "my machine"
        Process = None
        ByteInfo = None
        StringInfo = None
        Exception = None
        ExceptionString = None
        }
    
    let predicate1 = 
        {
        Operator = PredicateOperator.Between
        StartDate = DateTime.UtcNow
        EndDate = None
        Caller = None
        LogLevels = [||]
        }

    [<Tests>]
    let testSimpleTests =

        testList "write and read log record" [
            testCase "equality no optional" <| fun () ->
                let testDate = DateTime.UtcNow.AddDays(-7.)
                let inLog = {log1 with 
                                UtcRunTime = testDate
                                UtcTime = testDate }
                
                let predicate = {predicate1 with
                                  StartDate = testDate.AddMinutes(-1.)
                                  EndDate = testDate.AddMinutes(1.) |> Some }

                insert azureConnectionString inLog "logs"
                let outLog = 
                    list predicate azureConnectionString "logs"
                    |> Seq.filter (fun x-> x.UtcTime = testDate)
                    |> Seq.head

                Expect.isTrue (inLog = outLog) "Expected True"

            testCase "equality no optional write async" <| fun () ->
                let testDate = DateTime.UtcNow.AddDays(-5.)
                let inLog = {log1 with 
                                UtcRunTime = testDate
                                UtcTime = testDate }
                
                let predicate = {predicate1 with
                                  StartDate = testDate.AddMinutes(-1.)
                                  EndDate = testDate.AddMinutes(1.) |> Some }

                insertAsync azureConnectionString inLog "logs"
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> ignore

                let outLog = 
                    list predicate azureConnectionString "logs"
                    |> Seq.filter (fun x-> x.UtcTime = testDate)
                    |> Seq.head

                Expect.isTrue (inLog = outLog) "Expected True"

            testCase "equality all optionals" <| fun () ->
                try
                    let n = 1 / 0
                    Expect.isTrue false "how did we get here?"
                with e ->
                    let testDate = DateTime.UtcNow.AddHours(-6.)
                    let inLog = {log1 with 
                                    UtcRunTime = testDate
                                    UtcTime = testDate 
                                    Process = Some "equality all optionals"
                                    ByteInfo = Some [|0uy;1uy;2uy|]
                                    StringInfo = Some "a string"
                                    Exception = Some e
                                    ExceptionString = Some e.Message
                                    }
                
                    let predicate = {predicate1 with
                                      StartDate = testDate.AddMinutes(-1.)
                                      EndDate = testDate.AddMinutes(1.) |> Some }

                    insert azureConnectionString inLog "logs"
                    let outLog = 
                        list predicate azureConnectionString "logs"
                        |> Seq.filter (fun x-> x.UtcTime = testDate)
                        |> Seq.head

                    //because Exception comparison is by reference, we cannot compare whole log records
                    //interstingly, at more detailed comparison differ in ticks prevents comparison of UtcRunTime,
                    //but the tick difference does not affect full log record comparison
                    Expect.isTrue (inLog.AssembliesOrVersion = outLog.AssembliesOrVersion) "Expected AssembliesOrVersion"
                    Expect.isTrue (inLog.ByteInfo = outLog.ByteInfo) "Expected ByteInfo"
                    Expect.isTrue (inLog.Caller = outLog.Caller) "Expected Caller"
                    Expect.isTrue (inLog.Counter = outLog.Counter) "Expected Counter"
                    Expect.isTrue (sprintf "%A" inLog.Exception = sprintf "%A" outLog.Exception) 
                        (sprintf "inException = %A outException = %A" inLog.Exception outLog.Exception)
                    Expect.isTrue (inLog.ExceptionString = outLog.ExceptionString) "Expected ExceptionString"
                    Expect.isTrue (inLog.Level = outLog.Level) "Expected Level"
                    Expect.isTrue (inLog.MachineName = outLog.MachineName) "Expected MachineName"
                    Expect.isTrue (inLog.Message = outLog.Message) "Expected Message"
                    Expect.isTrue (inLog.Process = outLog.Process) "Expected Process"
                    Expect.isTrue (inLog.StringInfo = outLog.StringInfo) "Expected StringInfo"
                    Expect.isTrue (inLog.UtcTime = outLog.UtcTime) "Expected UtcTime"
                    Expect.isTrue (inLog.UtcRunTime.Second = outLog.UtcRunTime.Second) 
                        (sprintf "inLogSecond = %i outLogSecond = %i" inLog.UtcRunTime.Second outLog.UtcRunTime.Second)
                    Expect.isTrue (inLog.UtcRunTime.Millisecond = outLog.UtcRunTime.Millisecond) 
                        (sprintf "inMillisecond = %i outMillisecond = %i" inLog.UtcRunTime.Millisecond outLog.UtcRunTime.Millisecond)
                    //Expect.isTrue (inLog.UtcRunTime.Ticks = outLog.UtcRunTime.Ticks) 
                    //    (sprintf "inTicks = %i outTicks = %i" inLog.UtcRunTime.Ticks outLog.UtcRunTime.Ticks)


            testCase "select by level" <| fun () ->
                let testDate = DateTime.UtcNow.AddHours(-5.)
                let inLog = {log1 with 
                                UtcRunTime = testDate
                                UtcTime = testDate }
                
                let predicate = {predicate1 with
                                  StartDate = testDate.AddMinutes(-1.)
                                  EndDate = testDate.AddMinutes(1.) |> Some 
                                  LogLevels = [|LogLevel.Info; LogLevel.Debug|]}

                insert azureConnectionString inLog "logs"
                let outLog = 
                    list predicate azureConnectionString "logs"
                    |> Seq.filter (fun x-> x.UtcTime = testDate)
                    |> Seq.head

                Expect.isTrue (inLog = outLog) "Expected True"

            testCase "select by level not found" <| fun () ->
                let testDate = DateTime.UtcNow.AddHours(-4.)
                let inLog = {log1 with 
                                UtcRunTime = testDate
                                UtcTime = testDate }
                
                let predicate = {predicate1 with
                                  StartDate = testDate.AddMinutes(-1.)
                                  EndDate = testDate.AddMinutes(1.) |> Some 
                                  LogLevels = [|LogLevel.Error; LogLevel.Debug|]}

                insert azureConnectionString inLog "logs"
                let outLogLength = 
                    list predicate azureConnectionString "logs"
                    |> Seq.filter (fun x-> x.UtcTime = testDate)
                    |> Seq.length

                Expect.isTrue (outLogLength = 0) "Expected True"
        ]

