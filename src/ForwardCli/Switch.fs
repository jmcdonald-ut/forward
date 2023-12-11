module ForwardCli.Switch

open Argu

open Forward.Project

type SwitchArgs =
  | [<MainCommand; ExactlyOnce>] Name of name: string
  | [<CustomCommandLine("-b")>] Create

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "environment name."
      | Create -> "create a new environment using <name>."

// SUBCOMMAND
//   fwd switch
// ****************************************************************************

let handleSwitchCommand (commandContext: CommandContext) (args: ParseResults<SwitchArgs>) =
  let hasCreateFlag: bool = args.Contains(Create)
  let mode: SwitchMode = if hasCreateFlag then SwitchMode.Create else ReadOnly
  let name: string = args.GetResult(Name)
  let args: Forward.Project.SwitchArgs = { Name = name; Mode = mode }

  args |> switch commandContext |> OutputResult.stringResultOf
