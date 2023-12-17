module ForwardCli.Init

// SUBCOMMAND
//   fwd init
// ****************************************************************************

let handleInitCommand (commandContext: Forward.CommandContext.FileCommandContext) =
  commandContext |> Forward.Project.init |> OutputResult.stringResultOf
