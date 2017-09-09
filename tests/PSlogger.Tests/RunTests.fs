namespace PSlogger.Tests

open Expecto
open FSharp.Configuration
open System.Diagnostics

module RunTests =

    type Settings = AppSettings<"app.config">

    [<EntryPoint>]
    let main args =

        if Settings.ConnectionStrings.AzureStorage.ToLower() = "usedevelopmentstorage=true" then
            Process.Start(@"C:\Program Files\Microsoft SDKs\Azure\Emulator\csrun", "/devstore").WaitForExit()
        else
            ()

        Tests.runTestsWithArgs defaultConfig args Tests.testSimpleTests |> ignore

        0

