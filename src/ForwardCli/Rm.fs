module ForwardCli.Rm

open Argu
open Forward

type RemoveArgs =
  | [<MainCommand; ExactlyOnce>] Name of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "dotenv name"

// SUBCOMMAND
//   fwd rm
// ****************************************************************************

let handleRemoveCommand (commandContext: CommandContext.FileCommandContext) (removeArgs: ParseResults<RemoveArgs>) =
  removeArgs.GetResult(Name)
  |> Project.remove commandContext
  |> OutputResult.stringResultOf
