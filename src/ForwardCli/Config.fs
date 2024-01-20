module ForwardCli.Config

open Argu
open dotenv.net
open Spectre.Console

open ForwardCli.OutputResult
open Forward.Project

// SUBCOMMAND
//   fwd config get <name>
//   fwd config get-many [<name>...]
//   fwd config compare [<name>...]
// ****************************************************************************

type ConfigVarArgs =
  | [<MainCommand; ExactlyOnce>] Name of name: string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "variable name"

type ConfigGetManyVarArgs =
  | [<MainCommand; Last>] Names of string list
  | [<CustomCommandLine("-t")>] Terse

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Names _ -> "variable names."
      | Terse -> "outputs a list."

type ConfigCompareArgs =
  | [<MainCommand; Last>] Compare_Names of string list

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Compare_Names _ -> "variable names."

[<RequireSubcommand>]
type ConfigArgs =
  | [<SubCommand; CliPrefix(CliPrefix.None); AltCommandLine("g")>] Get of ParseResults<ConfigVarArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None); AltCommandLine("gm")>] Get_Many of ParseResults<ConfigGetManyVarArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None); AltCommandLine("c")>] Compare of ParseResults<ConfigCompareArgs>

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Compare _ -> "compares 1+ variables across dotenv files."
      | Get _ -> "gets a variable from the current dotenv."
      | Get_Many _ -> "gets 1+ variables from the current dotenv."

let private noneConst (_: string) = "<NONE>"

let private fallbackToSystemEnv (varName: string) : CommandResult<string> =
  match Environment.getEnvironmentVariableOpt varName with
  | Some(value) -> StringResult(value)
  | None -> ErrorResult("Unable to resolve a value")

let private extractFromEnvFileOrFallbackToSystemEnv (varName: string) (path: System.IO.FileSystemInfo) =
  let envVars: System.Collections.Generic.IDictionary<string, string> =
    DotEnv.Read(new DotEnvOptions(envFilePaths = [ path.FullName ]))

  match envVars.ContainsKey varName with
  | false -> fallbackToSystemEnv varName
  | true -> StringResult(envVars[varName])

let private handleGet (commandContext: Forward.CommandContext.FileCommandContext) (varName: string) =
  match FileHelpers.actualPathToCurrentEnv commandContext with
  | Error(_) -> fallbackToSystemEnv varName
  | Ok(path) -> extractFromEnvFileOrFallbackToSystemEnv varName path

let private handleGetMany
  (commandContext: Forward.CommandContext.FileCommandContext)
  (args: ParseResults<ConfigGetManyVarArgs>)
  (varNames: string seq)
  =
  match FileHelpers.actualPathToCurrentEnv commandContext with
  | Error(_) -> varNames |> Seq.map noneConst |> SeqResult
  | Ok(path) ->
    let dotEnv = Utils.asDotEnv commandContext path

    if args.Contains(Terse) then
      dotEnv |> DotEnv.getVarsOr noneConst varNames |> SeqResult
    else
      let folder (table: Table) (item: (string * string)) =
        let (varName, varValue) = item
        table.AddRow([| varName; varValue |])

      dotEnv
      |> DotEnv.getVarsOr noneConst varNames
      |> Seq.zip varNames
      |> makeTableResult [| "Var"; "Val" |] folder
      |> TableResult

let handleCompare (commandContext) (args) (varNames: string list) =
  let columns: string array = "DotEnv" :: varNames |> Array.ofList
  let pickValues = DotEnv.getVarsOr noneConst varNames

  let makeSingleRow (entry: Utils.ListEntry) =
    let l1: string list = entry |> pickValues |> Seq.toList
    entry.Name :: l1

  let folder (table: Table) (columns: string list) = columns |> Array.ofList |> table.AddRow

  commandContext
  |> Utils.listDotEnvs
  |> Seq.map makeSingleRow
  |> makeTableResult columns folder
  |> TableResult

let handleConfigCommand
  (commandContext: Forward.CommandContext.FileCommandContext)
  (format: OutputFormat)
  (squelchError: bool)
  (args: ParseResults<ConfigArgs>)
  =
  let doPrintAndExit =
    fun (result: CommandResult<'row>) -> printAndExit format squelchError result

  match args.TryGetSubCommand() with
  | Some(Get(args)) -> args.GetResult Name |> _.ToUpper() |> handleGet commandContext |> doPrintAndExit
  | Some(Get_Many(args)) ->
    args.GetResult Names
    |> List.map _.ToUpper()
    |> handleGetMany commandContext args
    |> doPrintAndExit
  | Some(Compare(args)) ->
    args.GetResult Compare_Names
    |> List.map _.ToUpper()
    |> handleCompare commandContext args
    |> doPrintAndExit
  | None -> "Trouble parsing command" |> ErrorResult |> doPrintAndExit
