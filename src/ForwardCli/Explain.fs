module ForwardCli.Explain

// SUBCOMMAND
//   fwd explain
// ****************************************************************************

let handleExplainCommand (commandContext: Forward.Project.CommandContext) =
  commandContext
  |> Forward.Project.explain
  |> Forward.Result.teeResult (fun (explainResult: Forward.Project.ExplainOutput) ->
    explainResult |> printfn "%O" |> ignore)
