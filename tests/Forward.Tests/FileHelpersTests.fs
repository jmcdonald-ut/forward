module Forward.Tests.FileHelpersTests

open Expecto
open Forward.FileHelpers

let setRootPathEnv str =
  System.Environment.SetEnvironmentVariable("FORWARD_ROOT_PATH", str)

let getRootPathEnv () =
  Environment.getEnvironmentVariable "FORWARD_ROOT_PATH"

let setHomePathEnv str =
  Environment.setEnvironmentVariable "HOME", str

let getHomePathEnv () =
  Environment.getEnvironmentVariable "HOME"

[<Tests>]
let tests =
  testSequenced
  <| testList
    "Forward.FileHelpers"
    [ testCase "getRootPathOpt will return its argument if it's something"
      <| fun _ ->
        let input = Some "thing"
        let actual = getRootPathOpt input
        Expect.equal actual input "Expected input to be forwarded"

      testCase "getRootPathOpt falls back to $FORWARD_ROOT_PATH if available"
      <| fun _ ->
        let prior = getRootPathEnv ()

        try
          setRootPathEnv "/foo/bar"
          let input = None
          let actual = getRootPathOpt input
          Expect.equal actual (Some "/foo/bar") "Expected fallback to be used"
        finally
          setRootPathEnv prior

      testCase "getRootPathOpt falls back to $HOME/.forward"
      <| fun _ ->
        let priorRootEnv = getRootPathEnv ()
        let priorHomeEnv = getHomePathEnv ()

        try
          setRootPathEnv null
          setHomePathEnv "/home" |> ignore
          let input = None
          let actual = getRootPathOpt input
          Expect.equal actual (Some "/home/.forward") "Expected fallback to be used"
        finally
          setHomePathEnv priorHomeEnv |> ignore
          setRootPathEnv priorRootEnv ]
