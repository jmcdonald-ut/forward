module ForwardCli.Explain

// SUBCOMMAND
//   fwd explain
// ****************************************************************************

let handleExplainCommand (commandContext: Forward.Project.CommandContext) =
  commandContext |> Forward.Project.explain |> OutputResult.recordResultOf
