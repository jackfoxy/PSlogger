namespace PSlogger.Tests

open Expecto
open FSharp.Configuration
open System.Diagnostics

type Settings = AppSettings<"app.config">

module RunTests =

    [<EntryPoint>]
    let main args =

        let azureConnectionString = Settings.ConnectionStrings.AzureStorage

        Process.Start(@"C:\Program Files\Microsoft SDKs\Azure\Emulator\csrun", "/devstore").WaitForExit();

        Tests.runTestsWithArgs defaultConfig args (Tests.testSimpleTests azureConnectionString) |> ignore

        0

