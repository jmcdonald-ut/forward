module ForwardCli.Rm

open Argu

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
  let handleRemoveCommandSuccess _ = name |> printfn "OK rm → %s" |> ignore

  args
  |> Forward.Project.remove commandContext
  |> Forward.Result.teeResult handleRemoveCommandSuccess
