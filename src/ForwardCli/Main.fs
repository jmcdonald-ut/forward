﻿open Argu
open Spectre.Console

open ForwardCli.OutputResult
open ForwardCli.Config
open ForwardCli.Db
open ForwardCli.Explain
open ForwardCli.Init
open ForwardCli.List
open ForwardCli.Rm
open ForwardCli.Switch

[<RequireSubcommand>]
type RootArgs =
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Db of ParseResults<DbCommand>
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Init
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Explain
  | [<SubCommand; CliPrefix(CliPrefix.None); AltCommandLine("c")>] Config of ParseResults<ConfigArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("ls")>] List of ParseResults<ListArgs>
  | [<CliPrefix(CliPrefix.None); CustomCommandLine("rm")>] Remove of ParseResults<RemoveArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("s")>] Switch of ParseResults<SwitchArgs>
  | [<CliPrefix(CliPrefix.DoubleDash)>] Project of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Project_Path of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Root of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Squelch
  | [<CliPrefix(CliPrefix.DoubleDash)>] Format of OutputFormat

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Config _ -> "gets or sets variables in dotenv file."
      | Db _ -> "backup, restore, or compare DBs in dotenv files."
      | Explain -> "explains the current context."
      | Init -> "initialize a project."
      | List _ -> "list project dotenv files."
      | Remove _ -> "remove project dotenv file."
      | Switch _ -> "switch the project's dotenv file."
      // Shared/Root CLI Args
      | Project _ -> "specify the project name."
      | Project_Path _ -> "specify the project's directory."
      | Root _ -> "specify the path to `fwd` artifacts."
      | Squelch -> "squelch errors"
      | Format _ -> "format"

type RootArgsStringFormat = Printf.StringFormat<(RootArgs list -> string)>

let checkStructure =
#if DEBUG
  true
#else
  false
#endif

// COMMAND
//   fwd
// ****************************************************************************

/// Parses command line arguments into a structure that's easier to map into the
/// business logic.
let parseCommandLine (argv: string[]) =
  let errorHandler: ProcessExiter =
    ProcessExiter(
      colorizer =
        function
        | ErrorCode.HelpText -> None
        | _ -> Some System.ConsoleColor.Red
    )

  ArgumentParser
    .Create<RootArgs>(programName = "fwd", errorHandler = errorHandler, checkStructure = checkStructure)
    .ParseCommandLine(argv)

let failWith (rootArgs: ParseResults<RootArgs>) (strf: RootArgsStringFormat) (format: OutputFormat) =
  let squelchError: bool = rootArgs.Contains(Squelch)

  rootArgs.GetAllResults()
  |> sprintf strf
  |> ErrorResult
  |> printAndExit format squelchError

/// Match a parsed command to its handler function.
let routeCommand
  (rootArgs: ParseResults<RootArgs>)
  (format: OutputFormat)
  (context: Forward.CommandContext.FileCommandContext)
  =
  let squelchError: bool = rootArgs.Contains(Squelch)

  let doPrintAndExit =
    fun (result: CommandResult<'row>) -> printAndExit format squelchError result

  match rootArgs.TryGetSubCommand() with
  | Some(Config(args)) -> handleConfigCommand context format squelchError args
  | Some(Db(args)) -> handleDbCommand context args format squelchError
  | Some(Explain) -> context |> handleExplainCommand |> doPrintAndExit
  | Some(Init) -> context |> handleInitCommand |> doPrintAndExit
  | Some(List(args)) -> args |> handleListCommand context |> doPrintAndExit
  | Some(Remove(args)) -> args |> handleRemoveCommand context |> doPrintAndExit
  | Some(Switch(args)) -> args |> handleSwitchCommand context |> doPrintAndExit
  | _ -> failWith rootArgs "Got invalid subcommand %O" format

let private parseAndExecuteCommand (argv: string[]) =
  let rootArgs: ParseResults<RootArgs> = parseCommandLine argv
  let format: OutputFormat = rootArgs.GetResult(Format, Standard)
  let maybeProjectName: string option = rootArgs.TryGetResult(Project)
  let maybeProjectPath: string option = rootArgs.TryGetResult(Project_Path)
  let maybeRootPath: string option = rootArgs.TryGetResult(Root)
  let squelchError: bool = rootArgs.Contains(Squelch)

  match Forward.CommandContext.buildFileCommandContext maybeRootPath maybeProjectName maybeProjectPath with
  | Ok(context) -> routeCommand rootArgs format context
  | Error(reason) -> reason |> ErrorResult |> printAndExit format squelchError

/// Main entry point that bootstraps and runs the CLI application.
[<EntryPoint>]
let main (argv: string[]) =
  try
    parseAndExecuteCommand argv
  with (ex: exn) ->
    AnsiConsole.WriteException(ex)
    1
