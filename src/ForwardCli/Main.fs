open Argu
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
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Init
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Explain
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Config of ParseResults<ConfigArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Backup of ParseResults<DbArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Restore of ParseResults<DbArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("ls")>] List of ParseResults<ListArgs>
  | [<CliPrefix(CliPrefix.None); CustomCommandLine("rm")>] Remove of ParseResults<RemoveArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("s")>] Switch of ParseResults<SwitchArgs>
  | [<CliPrefix(CliPrefix.DoubleDash)>] Project of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Root of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Squelch
  | [<CliPrefix(CliPrefix.DoubleDash)>] Format of OutputFormat

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Backup _ -> "backs up a DB."
      | Config _ -> "gets or sets variables in dotenv file."
      | Explain -> "explains the current context."
      | Init -> "initialize a project."
      | List _ -> "list project dotenv files."
      | Remove _ -> "remove project dotenv file."
      | Restore _ -> "restores a DB backup."
      | Switch _ -> "switch the project's dotenv file."
      // Shared/Root CLI Args
      | Project _ -> "specify the project name."
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
  | Some(Backup(args)) -> args |> handleBackupCommand context |> doPrintAndExit
  | Some(Config(args)) -> args |> handleConfigCommand context |> doPrintAndExit
  | Some(Explain) -> context |> handleExplainCommand |> doPrintAndExit
  | Some(Init) -> context |> handleInitCommand |> doPrintAndExit
  | Some(List(args)) -> args |> handleListCommand context |> doPrintAndExit
  | Some(Remove(args)) -> args |> handleRemoveCommand context |> doPrintAndExit
  | Some(Restore(args)) -> args |> handleRestoreCommand context |> doPrintAndExit
  | Some(Switch(args)) -> args |> handleSwitchCommand context |> doPrintAndExit
  | _ -> failWith rootArgs "Got invalid subcommand %O" format

let private parseAndExecuteCommand (argv: string[]) =
  let rootArgs: ParseResults<RootArgs> = parseCommandLine argv
  let format: OutputFormat = rootArgs.GetResult(Format, Standard)
  let maybeProjectName: string option = rootArgs.TryGetResult(Project)
  let maybeRootPath: string option = rootArgs.TryGetResult(Root)
  let squelchError: bool = rootArgs.Contains(Squelch)

  match Forward.CommandContext.buildFileCommandContext maybeRootPath maybeProjectName with
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
