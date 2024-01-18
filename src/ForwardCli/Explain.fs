module ForwardCli.Explain

// SUBCOMMAND
//   fwd explain
// ****************************************************************************

let handleExplainCommand (commandContext: Forward.CommandContext.FileCommandContext) =
  commandContext |> Forward.Project.Core.explain |> OutputResult.recordResultOf
