module ForwardCli.Rm

open Argu

open ForwardCli.OutputResult

type RemoveArgs =
  | [<MainCommand; ExactlyOnce>] Name of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "dotenv name"

// SUBCOMMAND
//   fwd rm
// ****************************************************************************

let handleRemoveCommand (commandContext: Forward.Project.CommandContext) (removeArgs: ParseResults<RemoveArgs>) =
  let name: string = removeArgs.GetResult Name
  let args: Forward.Project.RemoveArgs = { Name = name }

  match Forward.Project.remove commandContext args with
  | Ok(string) -> StringResult(sprintf "Removed `%s`" string)
  | Error(reason) -> ErrorResult(reason)
