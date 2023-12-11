module ForwardCli.Explain

open ForwardCli.OutputResult

// SUBCOMMAND
//   fwd explain
// ****************************************************************************

let handleExplainCommand (commandContext: Forward.Project.CommandContext) =
  match Forward.Project.explain commandContext with
  | Ok(record) -> RecordResult(record)
  | Error(reason) -> ErrorResult(reason)
