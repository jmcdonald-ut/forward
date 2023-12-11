module ForwardCli.Init

open ForwardCli.OutputResult

// SUBCOMMAND
//   fwd init
// ****************************************************************************

let handleInitCommand (commandContext: Forward.Project.CommandContext) =
  match Forward.Project.init commandContext with
  | Ok(string) -> StringResult(string)
  | Error(reason) -> ErrorResult(reason)
