module ForwardCli.Switch

open Argu

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

  normalizedSwitchArgs
  |> Forward.Project.switch commandContext
  |> Forward.Result.teeResult (fun _ -> printfn "SUCCESS")
