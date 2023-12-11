module ForwardCli.Config

open Argu
open dotenv.net

open ForwardCli.OutputResult

// SUBCOMMAND
//   fwd config
// ****************************************************************************

type ConfigVarArgs =
  | [<MainCommand; ExactlyOnce>] Name of name: string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "variable name"

[<RequireSubcommand>]
type ConfigArgs =
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Get of ParseResults<ConfigVarArgs>

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Get _ -> "gets a value from the current dotenv"

let private fallbackToSystemEnv (varName: string) =
  match Forward.FileHelpers.getEnvironmentVariableOpt varName with
  | Some(value) -> StringResult(value)
  | None -> ErrorResult("Unable to resolve a value")

let private extractFromEnvFileOrFallbackToSystemEnv (varName: string) (path: System.IO.FileSystemInfo) =
  let envVars: System.Collections.Generic.IDictionary<string, string> =
    DotEnv.Read(new DotEnvOptions(envFilePaths = [ path.FullName ]))

  match envVars.ContainsKey varName with
  | false -> fallbackToSystemEnv varName
  | true -> StringResult(envVars[varName])

let private handleGet (commandContext: Forward.FileHelpers.CommandFileContext) (varName: string) =
  match Forward.FileHelpers.actualPathToCurrentEnv commandContext with
  | Error(_) -> fallbackToSystemEnv varName
  | Ok(path) -> extractFromEnvFileOrFallbackToSystemEnv varName path

let handleConfigCommand (commandContext: Forward.FileHelpers.CommandFileContext) (args: ParseResults<ConfigArgs>) =
  match args.TryGetSubCommand() with
  | Some(Get(args)) -> args.GetResult Name |> _.ToUpper() |> handleGet commandContext
  | None -> ErrorResult("Trouble parsing command")
