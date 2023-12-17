module Forward.Processes

open System.Diagnostics

type ExecutableProcess =
  { StartInfo: ProcessStartInfo
    Executable: Process
    Inspect: string }

  static member Build
    (
      executableName: string,
      arguments: string,
      ?workingDirectory: string,
      ?useShellExecute: bool,
      ?redirectStandardOutput: bool,
      ?redirectStandardInput: bool
    ) =
    let startInfo: ProcessStartInfo = new ProcessStartInfo()
    startInfo.FileName <- executableName
    startInfo.Arguments <- arguments
    startInfo.UseShellExecute <- Option.defaultValue false useShellExecute
    startInfo.RedirectStandardInput <- Option.defaultValue false redirectStandardInput
    startInfo.RedirectStandardOutput <- Option.defaultValue false redirectStandardOutput
    Option.iter (fun (x: string) -> startInfo.WorkingDirectory <- x) workingDirectory

    let executable: Process = new Process()
    executable.StartInfo <- startInfo

    { StartInfo = startInfo
      Executable = executable
      Inspect = sprintf "`%s %s`" executableName arguments }

let resultOfExit (executableProcess: ExecutableProcess) (forward: 'a) =
  executableProcess.Executable.WaitForExit()

  match executableProcess.Executable.ExitCode with
  | 0 -> Ok(forward)
  | errorCode -> Error(sprintf "%s failed with error code %i" executableProcess.Inspect errorCode)

let executeTap (callback: (Process) -> 'a) (executableProcess: ExecutableProcess) =
  match executableProcess.Executable.Start() with
  | false -> Error(sprintf "%s failed to start" executableProcess.Inspect)
  | true ->
    callback (executableProcess.Executable) |> ignore
    resultOfExit executableProcess executableProcess

let executeCapture (callback: (Process) -> 'a) (executableProcess: ExecutableProcess) =
  match executableProcess.Executable.Start() with
  | false -> Error(sprintf "%s failed to start" executableProcess.Inspect)
  | true -> executableProcess.Executable |> callback |> resultOfExit executableProcess

let execute (executableProcess: ExecutableProcess) =
  match executableProcess.Executable.Start() with
  | false -> Error(sprintf "%s failed to start" executableProcess.Inspect)
  | true -> resultOfExit executableProcess executableProcess
