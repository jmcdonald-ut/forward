module ForwardCli.Switch

open Argu

open ForwardCli.OutputResult

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

let handleSwitchCommand (commandContext: Forward.Project.CommandContext) (switchArgs: ParseResults<SwitchArgs>) =
  let normalizedSwitchArgs: Forward.Project.SwitchArgs =
    { Forward.Project.SwitchArgs.Name = switchArgs.GetResult(Name)
      Forward.Project.SwitchArgs.Mode =
        match switchArgs.Contains(Create) with
        | true -> Forward.Project.SwitchMode.Create
        | _ -> Forward.Project.SwitchMode.ReadOnly }

  match Forward.Project.switch commandContext normalizedSwitchArgs with
  | Ok(string) -> StringResult(sprintf "Switched to %s" string)
  | Error(reason) -> ErrorResult(reason)
