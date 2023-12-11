module ForwardCli.Init

// SUBCOMMAND
//   fwd init
// ****************************************************************************

let handleInitCommand (commandContext: Forward.Project.CommandContext) =
  commandContext |> Forward.Project.init |> OutputResult.stringResultOf
