module Forward.Project.Core

open System.IO

open Forward.CommandContext

type ListArgs =
  { Column: Utils.ListColumn
    Direction: Utils.ListDirection
    Limit: int }

type SwitchMode =
  | Create
  | ReadOnly

/// Initializes a new project if one does't already exist. If one does exist,
/// this re-runs the init logic in a non-destructive manner.
let init (commandContext: FileCommandContext) : Result<string, string> =
  // <codebase>/.env → <artifacts>/.env.current → <artifacts>/dotenvs/.env.main
  let artifactsPath: string = commandContext.ProjectArtifactsPath
  let fwdLinkPath: string = File.combinePaths artifactsPath ".env.current"
  let dotEnvsPath: string = File.combinePaths artifactsPath "dotenvs"
  let mainDotEnvPath: string = File.combinePaths dotEnvsPath ".env.main"
  let projectDotEnvPath: string = File.combinePaths commandContext.ProjectPath ".env"

  let createDirectoryIfMissing (path: string) =
    FileHelpers.whenFileOrDirectoryIsMissing
      (fun _ -> Ok(sprintf "`%s` created" (File.createDirectory path).FullName))
      path

  let createSymLinkIfMissing (targetPath: string) (linkPath: string) =
    linkPath
    |> FileHelpers.createSymbolicLinkIfMissing targetPath
    |> Result.bind (fun (fileInfo: FileSystemInfo) -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  let createDotEnv (path: string) =
    // TODO: Copy .env if possible.
    use stream: StreamWriter = File.CreateText path
    stream.WriteLine("# Generated .env")
    Ok(sprintf "`%s` created" path)

  let createDotEnvIfMissing (path: string) =
    FileHelpers.whenFileOrDirectoryIsMissing createDotEnv path

  commandContext.ProjectArtifactsPath
  |> createDirectoryIfMissing
  |> Result.bind (fun _ -> createDirectoryIfMissing dotEnvsPath)
  |> Result.bind (fun _ -> createDotEnvIfMissing mainDotEnvPath)
  |> Result.bind (fun _ -> createSymLinkIfMissing fwdLinkPath mainDotEnvPath)
  |> Result.bind (fun _ -> createSymLinkIfMissing projectDotEnvPath fwdLinkPath)

let buildListArgs (limit: int) (sortColString: string) (sortDirString: string) : ListArgs =
  let sortDir: Utils.ListDirection =
    match sortDirString.ToLower() with
    | "desc" -> Utils.Desc
    | _ -> Utils.Asc

  let sortCol: Utils.ListColumn =
    match sortColString.ToLower() with
    | "created" -> Utils.Created
    | "updated" -> Utils.Updated
    | "accessed" -> Utils.Accessed
    | _ -> Utils.Name

  { Column = sortCol
    Direction = sortDir
    Limit = limit }

/// Lists dotenv files for the forward operating base with support for sorting.
/// The "current" dotenv is highlighted.
let list (commandContext: FileCommandContext) (listParams: ListArgs) =
  try
    let fileList: FileSystemInfo list = Utils.listDotEnvs commandContext
    let comparer = Utils.makeCompareFun (listParams.Column, listParams.Direction)

    fileList
    |> List.map (Utils.asDotEnv commandContext)
    |> List.sortWith comparer
    |> List.take (Math.clamp 0 fileList.Length listParams.Limit)
    |> Ok
  with :? DirectoryNotFoundException ->
    Error(sprintf "Project `%s` not found; run fwd init or provide a project name." commandContext.ProjectName)

/// Switch the dotenv file, either to an existing one or by creating a new one.
let switch (commandContext: FileCommandContext) (name: string) (mode: SwitchMode) =
  let currentPath: string =
    FileHelpers.projectPathTo commandContext [ ".env.current" ]

  let targetPath: string = FileHelpers.dotenvPath commandContext name

  let touchExisting _ =
    match FileHelpers.actualPathToCurrentEnv commandContext with
    | Ok path -> System.IO.File.SetLastWriteTimeUtc(path.FullName, System.DateTime.UtcNow)
    | Error _ -> ()

  let replaceInternalSymLink _ =
    touchExisting () |> ignore
    currentPath |> File.deleteFile |> ignore
    System.IO.File.SetLastWriteTimeUtc(targetPath, System.DateTime.UtcNow)

    FileHelpers.createSymbolicLinkIfMissing currentPath targetPath
    |> Result.bind (fun fileInfo -> Ok(sprintf "backing symlink now `%s` → `%s`" fileInfo.FullName targetPath))

  let createDotEnvAndReplaceInternalSymLink _ =
    let currentContent: string = System.IO.File.ReadAllText currentPath
    use newFile: System.IO.StreamWriter = System.IO.File.CreateText targetPath
    newFile.Write(currentContent)

    replaceInternalSymLink ()
    |> Result.bind (fun (msg: string) -> Ok(sprintf "created DotEnv; %s" msg))

  let finalizeSuccess =
    Result.bind (fun (msg: string) -> Ok(sprintf "Switched; %s" msg))

  match mode, File.exists targetPath with
  | Create, true -> Error(sprintf "Cannot create `%s`; it already exists" name)
  | Create, false -> createDotEnvAndReplaceInternalSymLink () |> finalizeSuccess
  | ReadOnly, true -> replaceInternalSymLink () |> finalizeSuccess
  | ReadOnly, false -> Error(sprintf "Cannot switch to `%s`; it does not exist" name)

/// Removes the dotenv file from the project.
let remove (commandContext: FileCommandContext) (dotEnvName: string) =
  let fullPath: string = FileHelpers.dotenvPath commandContext dotEnvName
  let exists: bool = File.exists fullPath
  let isCurrent = FileHelpers.isCurrentEnvByPath commandContext fullPath

  match (exists, isCurrent) with
  | (_, true) -> Error(sprintf "Cannot remove `%s` since it is the current dotenv." dotEnvName)
  | (false, _) -> Error(sprintf "Unable to find dotenv `%s` in the current project" dotEnvName)
  | (true, _) -> File.deleteFile fullPath

let explain (commandContext: FileCommandContext) : Result<DotEnvCommandContext, string> =
  let getDotEnvPath = FileHelpers.actualPathToCurrentEnv >> Result.map (_.FullName)
  let dotEnvPath: string option = commandContext |> getDotEnvPath |> Result.toOption

  Ok
    { RootPath = commandContext.RootPath
      ProjectName = commandContext.ProjectName
      ProjectArtifactsPath = commandContext.ProjectArtifactsPath
      DotEnvSymLinkPath = FileHelpers.tryProjectPathTo commandContext [ ".env.current" ]
      DotEnvPath = dotEnvPath }
