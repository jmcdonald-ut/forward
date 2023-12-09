module Forward.Processes

open System.Diagnostics

let private unwrapOrFalse = Option.defaultValue false

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
    startInfo.UseShellExecute <- unwrapOrFalse useShellExecute
    startInfo.RedirectStandardInput <- unwrapOrFalse redirectStandardInput
    startInfo.RedirectStandardOutput <- unwrapOrFalse redirectStandardOutput
    Option.iter (fun (x: string) -> startInfo.WorkingDirectory <- x) workingDirectory

    let executable: Process = new Process()
    executable.StartInfo <- startInfo

    { StartInfo = startInfo
      Executable = executable
      Inspect = sprintf "`%s %s`" executableName arguments }

  static member ExecuteTap(callback: ((Process) -> 'a), executableProcess: ExecutableProcess) =
    match executableProcess.Executable.Start() with
    | false -> Error(sprintf "%s failed to start" executableProcess.Inspect)
    | true ->
      callback (executableProcess.Executable) |> ignore
      executableProcess.Executable.WaitForExit()

      match executableProcess.Executable.ExitCode with
      | 0 -> Ok(executableProcess)
      | errorCode -> Error(sprintf "%s failed with error code %i" executableProcess.Inspect errorCode)

  static member ExecuteCapture(callback: ((Process) -> 'a), executableProcess: ExecutableProcess) =
    match executableProcess.Executable.Start() with
    | false -> Error(sprintf "%s failed to start" executableProcess.Inspect)
    | true ->
      let result: 'a = callback (executableProcess.Executable)
      executableProcess.Executable.WaitForExit()

      match executableProcess.Executable.ExitCode with
      | 0 -> Ok(result)
      | errorCode -> Error(sprintf "%s failed with error code %i" executableProcess.Inspect errorCode)

  static member Execute(executableProcess: ExecutableProcess) =
    match executableProcess.Executable.Start() with
    | false -> Error(sprintf "%s failed to start" executableProcess.Inspect)
    | true ->
      executableProcess.Executable.WaitForExit()

      match executableProcess.Executable.ExitCode with
      | 0 -> Ok(executableProcess)
      | errorCode -> Error(sprintf "%s failed with error code %i" executableProcess.Inspect errorCode)
