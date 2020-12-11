namespace PSlogger.Tests

open Expecto
open System.Diagnostics

module RunTests =
    [<EntryPoint>]
    let main args =
        Process.Start(@"C:\Program Files\Microsoft SDKs\Azure\Emulator\csrun", "/devstore").WaitForExit()

        Tests.runTestsInAssembly defaultConfig args
