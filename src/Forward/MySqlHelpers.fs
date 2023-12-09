module Forward.MySqlHelpers

open System.Diagnostics

// -- (Private) Context Utils
type BackupContext =
  { BackupPath: string
    BackupName: string
    CommandContext: FileHelpers.CommandFileContext option
    CompressedBackupPath: string
    CompressedBackupName: string
    DbName: string
    WorkingDirectory: string }

let private findOrCreateMysqlBackupsDirectory (commandContext: FileHelpers.CommandFileContext) =
  (commandContext.ProjectPath, "mysql_backups")
  |> System.IO.Path.Join
  |> System.IO.Directory.CreateDirectory
  |> (fun (dir: System.IO.DirectoryInfo) -> dir.FullName)

let private buildBackupContext (commandContext: FileHelpers.CommandFileContext) (dbName: string) =
  let backupName: string = sprintf "%s.dump.sql" dbName
  let compressedName: string = sprintf "%s.tar.gz" backupName
  let workingDirectory: string = findOrCreateMysqlBackupsDirectory commandContext

  {
    BackupPath = System.IO.Path.Join(workingDirectory, backupName)
    BackupName = backupName
    CommandContext = Some commandContext
    CompressedBackupPath = System.IO.Path.Join(workingDirectory, compressedName)
    CompressedBackupName = compressedName
    DbName = dbName
    WorkingDirectory = workingDirectory
  }

// -- (Private) Command Utils

let private buildStartInfo (executableName: string) (argumentString: string) =
  let startInfo: ProcessStartInfo = new ProcessStartInfo()
  startInfo.FileName <- executableName
  startInfo.Arguments <- argumentString
  startInfo

let private commandFailedMessage (startInfo: ProcessStartInfo) (errorCode: int) =
  sprintf "`%s %s` failed with ErrorCode=%i" startInfo.FileName startInfo.Arguments errorCode

let private waitForCommand (activeProcess: Process) =
  activeProcess.WaitForExit()

  match activeProcess.ExitCode with
  | 0 -> Ok(activeProcess)
  | (errorCode: int) -> Error(commandFailedMessage activeProcess.StartInfo errorCode)

// -- (Private) Mysql Backup Utils

let private mysqlDumpIntoIo (backupContext: BackupContext) =
  let startInfo: ProcessStartInfo = buildStartInfo "mysqldump" backupContext.DbName
  startInfo.UseShellExecute <- false
  startInfo.RedirectStandardOutput <- true

  let activeProcess: Process = new Process()
  activeProcess.StartInfo <- startInfo

  match activeProcess.Start() with
  | false -> Error("Unable to start `mysqldump`")
  | true ->
    let result: string = activeProcess.StandardOutput.ReadToEnd()

    activeProcess |> waitForCommand |> Result.bind (fun _ -> Ok(result))

let private writeMysqlDumpOut (backupContext: BackupContext) (io: string) =
  do
    use fs: System.IO.FileStream = System.IO.File.OpenWrite backupContext.BackupPath
    let info: byte array = System.Text.UTF8Encoding(true).GetBytes(io)
    fs.Write(info, 0, info.Length)

  Ok(backupContext)

let private compressMysqlDump (backupContext: BackupContext) =
  let arg: string =
    sprintf "-a -cf %s %s" backupContext.CompressedBackupName backupContext.BackupName

  let startInfo: ProcessStartInfo = buildStartInfo "tar" arg
  startInfo.WorkingDirectory <- backupContext.WorkingDirectory

  let activeProcess: Process = new Process()
  activeProcess.StartInfo <- startInfo

  match activeProcess.Start() with
  | false -> Error("Unable to start `tar -czf <out> <in>`")
  | true -> activeProcess |> waitForCommand |> Result.bind (fun _ -> Ok(backupContext))

let private removeIntermediateBackup (backupContext: BackupContext) =
  System.IO.File.Delete(backupContext.BackupPath)
  Ok(backupContext)

// -- (Private) DB Recovery Utils

let private decompressBackup (backupContext: BackupContext) =
  let startInfo: ProcessStartInfo = new ProcessStartInfo()
  startInfo.FileName <- "tar"
  startInfo.Arguments <- sprintf "-zxf %s" backupContext.CompressedBackupPath
  startInfo.WorkingDirectory <- backupContext.WorkingDirectory

  let activeProcess: Process = new Process()
  activeProcess.StartInfo <- startInfo

  match activeProcess.Start() with
  | false -> Error(sprintf "Unable to start `%s %s" startInfo.FileName startInfo.Arguments)
  | true -> activeProcess |> waitForCommand |> Result.bind (fun _ -> Ok(backupContext))

let private executeMysqlFromBackup (backupContext: BackupContext) =
  let startInfo: ProcessStartInfo = buildStartInfo "mysql" backupContext.DbName
  startInfo.WorkingDirectory <- backupContext.WorkingDirectory
  startInfo.RedirectStandardInput <- true

  let activeProcess: Process = new Process()
  activeProcess.StartInfo <- startInfo

  match activeProcess.Start() with
  | false -> Error(sprintf "Unable to start `%s %s`" startInfo.FileName startInfo.Arguments)
  | true ->
    activeProcess.StandardInput.Write(System.IO.File.ReadAllText(backupContext.BackupPath))
    activeProcess.StandardInput.Close()
    activeProcess |> waitForCommand |> Result.bind (fun _ -> Ok(backupContext))

// -- Public Interface

/// Backs up the database.
let backupDb (commandContext: FileHelpers.CommandFileContext) (dbName: string) =
  let backupContext: BackupContext = buildBackupContext commandContext dbName

  backupContext
  |> mysqlDumpIntoIo
  |> Result.bind (writeMysqlDumpOut backupContext)
  |> Result.bind compressMysqlDump
  |> Result.bind removeIntermediateBackup

/// Restores the DB.
let restoreDb (commandContext: FileHelpers.CommandFileContext) (dbName: string) =
  dbName
  |> buildBackupContext commandContext
  |> decompressBackup
  |> Result.bind executeMysqlFromBackup
  |> Result.bind removeIntermediateBackup
