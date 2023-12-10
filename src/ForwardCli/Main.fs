open Argu
open Spectre.Console

open ForwardCli.Config
open ForwardCli.Db
open ForwardCli.Explain
open ForwardCli.Init
open ForwardCli.List
open ForwardCli.Rm
open ForwardCli.Switch

// COMMAND
//   fwd
// ****************************************************************************

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

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Init -> "initialize a project."
      | Backup _ -> "backs up a DB."
      | Restore _ -> "restores a DB backup."
      | Config _ -> "gets or sets variables in dotenv file."
      | Explain -> "explains the current context."
      | List _ -> "list project dotenv files."
      | Remove _ -> "remove project dotenv file."
      | Switch _ -> "switch the project's dotenv file."
      // Shared/Root CLI Args
      | Project _ -> "specify the project name."
      | Root _ -> "specify the path to `fwd` artifacts."
      | Squelch -> "squelch errors"

let checkStructure =
#if DEBUG
  true
#else
  false
#endif

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

/// Match a parsed command to its handler function.
let routeCommand (results: ParseResults<RootArgs>) (context: Forward.Project.CommandContext) =
  match results.TryGetSubCommand() with
  | Some(Init) -> handleInitCommand context
  | Some(Explain) -> handleExplainCommand context
  | Some(Config(args)) -> handleConfigCommand context args
  | Some(Backup(args)) -> handleBackupCommand context args
  | Some(Restore(args)) -> handleRestoreCommand context args
  | Some(List(args)) -> handleListCommand context args
  | Some(Switch(args)) -> handleSwitchCommand context args
  | Some(Remove(args)) -> handleRemoveCommand context args
  | Some _ -> Error(sprintf "Got invalid subcommand %O" (results.GetAllResults()))
  | None -> Error(sprintf "Got none with %O" (results.GetAllResults()))

let private parseAndExecuteCommand (argv: string[]) =
  let results: ParseResults<RootArgs> = parseCommandLine argv
  let maybeProjectName: string option = results.TryGetResult(Project)
  let maybeRootPath: string option = results.TryGetResult(Root)

  let commandContext: Result<Forward.FileHelpers.CommandFileContext, string> =
    Forward.FileHelpers.buildCommandContext maybeRootPath maybeProjectName

  match Result.bind (routeCommand results) commandContext with
  | Ok unitFunc ->
    unitFunc ()
    0
  | Error(reason: string) ->
    match results.Contains(Squelch) with
    | true -> 1
    | false ->
      printfn "ERR: %s" reason
      1

/// Main entry point that bootstraps and runs the CLI application.
[<EntryPoint>]
let main (argv: string[]) =
  try
    parseAndExecuteCommand argv
  with (ex: exn) ->
    AnsiConsole.WriteException(ex)
    1
