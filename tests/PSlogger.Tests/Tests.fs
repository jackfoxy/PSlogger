namespace PSlogger.Tests

open Expecto
open FsCheck
open PSlogger
open System

module Tests =
    let config10k = { FsCheckConfig.defaultConfig with maxTest = 10000}
    // bug somewhere:  registering arbitrary generators causes Expecto VS test adapter not to work
    //let config10k = { FsCheckConfig.defaultConfig with maxTest = 10000; arbitrary = [typeof<Generators>] }
    let configReplay = { FsCheckConfig.defaultConfig with maxTest = 10000 ; replay = Some <| (1940624926, 296296394) }

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
        LogLevels = []
        }

    [<Tests>]
    let testSimpleTests azureConnectionString =

        testList "write and read log record no optional" [
            testCase "equality" <| fun () ->
                let testDate = DateTime.UtcNow.AddDays(-7.)
                let inLog = {log1 with 
                                UtcRunTime = testDate
                                UtcTime = testDate }
                
                let predicate = {predicate1 with
                                  StartDate = testDate.AddMinutes(-1.)
                                  EndDate = testDate.AddMinutes(1.) |> Some }

                IO.insert azureConnectionString inLog
                let outLog = 
                    IO.list predicate azureConnectionString
                    |> Seq.head

                Expect.isTrue (inLog = outLog) "Expected True"

            //testPropertyWithConfig config10k "whitespace" <|
            //    fun  () ->
            //        Prop.forAll (Arb.fromGen <| whitespaceString())
            //            (fun (x : string) -> 
            //                x = x)
        ]

