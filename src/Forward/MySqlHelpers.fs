module Forward.MySqlHelpers

open Forward.Processes
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

  { BackupPath = System.IO.Path.Join(workingDirectory, backupName)
    BackupName = backupName
    CommandContext = Some commandContext
    CompressedBackupPath = System.IO.Path.Join(workingDirectory, compressedName)
    CompressedBackupName = compressedName
    DbName = dbName
    WorkingDirectory = workingDirectory }

// -- (Private) Command Utils

type RunProcessArg =
  { StartInfo: ProcessStartInfo
    BeforeWait: (Process -> Process) option }

// -- (Private) Mysql Backup Utils

let private mysqlDumpIntoIo (backupContext: BackupContext) =
  let executableProcess: ExecutableProcess =
    ExecutableProcess.Build(
      executableName = "mysqldump",
      arguments = backupContext.DbName,
      useShellExecute = false,
      redirectStandardOutput = true
    )

  ExecutableProcess.ExecuteCapture(_.StandardOutput.ReadToEnd(), executableProcess)

let private writeMysqlDumpOut (backupContext: BackupContext) (io: string) =
  do
    use fs: System.IO.FileStream = System.IO.File.OpenWrite backupContext.BackupPath
    let info: byte array = System.Text.UTF8Encoding(true).GetBytes(io)
    fs.Write(info, 0, info.Length)

  Ok(backupContext)

let private compressMysqlDump (backupContext: BackupContext) =
  let executableProcess: ExecutableProcess =
    ExecutableProcess.Build(
      executableName = "tar",
      arguments = sprintf "-a -cf %s %s" backupContext.CompressedBackupName backupContext.BackupName,
      workingDirectory = backupContext.WorkingDirectory
    )

  executableProcess
  |> ExecutableProcess.Execute
  |> Result.bind (fun _ -> Ok(backupContext))

let private removeIntermediateBackup (backupContext: BackupContext) =
  System.IO.File.Delete(backupContext.BackupPath)
  Ok(backupContext)

// -- (Private) DB Recovery Utils

let private decompressBackup (backupContext: BackupContext) =
  let executableProcess: ExecutableProcess =
    ExecutableProcess.Build(
      executableName = "tar",
      arguments = sprintf "-zxf %s" backupContext.CompressedBackupPath,
      workingDirectory = backupContext.WorkingDirectory
    )

  executableProcess
  |> ExecutableProcess.Execute
  |> Result.bind (fun _ -> Ok(backupContext))

let private executeMysqlFromBackup (backupContext: BackupContext) =
  let executableProcess: ExecutableProcess =
    ExecutableProcess.Build(
      executableName = "mysql",
      arguments = backupContext.DbName,
      workingDirectory = backupContext.WorkingDirectory,
      redirectStandardInput = true
    )

  let writeAndClose (activeProcess: Process) =
    activeProcess.StandardInput.Write(System.IO.File.ReadAllText(backupContext.BackupPath))
    activeProcess.StandardInput.Close()

  (writeAndClose, executableProcess)
  |> ExecutableProcess.ExecuteTap
  |> Result.bind (fun _ -> Ok(backupContext))

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
