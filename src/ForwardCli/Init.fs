module ForwardCli.Init

// SUBCOMMAND
//   fwd init
// ****************************************************************************

let handleInitCommand (commandContext: Forward.Project.CommandContext) =
  commandContext
  |> Forward.Project.init
  |> Forward.Result.teeResult (fun (initResult: string) -> printfn "OK init → %O" initResult)
