module ForwardCli.Explain

// SUBCOMMAND
//   fwd explain
// ****************************************************************************

let handleExplainCommand (commandContext: Forward.CommandContext.FileCommandContext) =
  commandContext |> Forward.Project.explain |> OutputResult.recordResultOf
