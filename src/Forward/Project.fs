module Forward.Project

/// This context is shared by the primary public functions in this module.
type CommandContext = FileHelpers.CommandFileContext

/// Initializes a new project if one does't already exist. If one does exist,
/// this re-runs the init logic in a non-destructive manner.
let init (commandContext: CommandContext) : Result<string, string> =
  let createDirectory _ =
    commandContext.ProjectPath
    |> FileHelpers.whenFileOrDirectoryIsMissing (fun (path: string) ->
      path |> System.IO.Directory.CreateDirectory |> ignore
      Ok(sprintf "`%s` created" path))

  let createDotEnvsDirectory _ =
    (commandContext.ProjectPath, "dotenvs")
    |> System.IO.Path.Combine
    |> FileHelpers.whenFileOrDirectoryIsMissing (fun (fullPath: string) ->
      fullPath |> System.IO.Directory.CreateDirectory |> ignore
      Ok(sprintf "`%s` created" fullPath))

  let createDotEnv _ =
    (commandContext.ProjectPath, "dotenvs", ".env.main")
    |> System.IO.Path.Combine
    |> FileHelpers.whenFileOrDirectoryIsMissing (fun (fullPath: string) ->
      // TODO: Copy .env if possible.
      use stream: System.IO.StreamWriter = System.IO.File.CreateText fullPath
      stream.WriteLine("# Generated .env")
      Ok(sprintf "`%s` created" fullPath))

  let createInternalSymLink _ =
    let path: string = FileHelpers.projectPathTo commandContext [ ".env.current" ]

    let targetPath: string =
      FileHelpers.projectPathTo commandContext [ "dotenvs"; ".env.main" ]

    FileHelpers.createSymbolicLinkIfMissing path targetPath
    |> Result.bind (fun (fileInfo: System.IO.FileSystemInfo) -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  let createSymLinkInProject _ =
    let path: string = FileHelpers.currentPathTo [ ".env" ]
    let targetPath: string = FileHelpers.projectPathTo commandContext [ ".env.current" ]

    FileHelpers.createSymbolicLinkIfMissing path targetPath
    |> Result.bind (fun (fileInfo: System.IO.FileSystemInfo) -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  commandContext.ProjectPath
  |> createDirectory
  |> Result.bind createDotEnvsDirectory
  |> Result.bind createDotEnv
  |> Result.bind createInternalSymLink
  |> Result.bind createSymLinkInProject

type ListDirection =
  | Asc
  | Desc

type ListColumn =
  | Name
  | Created
  | Updated
  | Accessed

type ListArgs = {
  Column: ListColumn
  Direction: ListDirection
}

type ListEntry = {
  Name: string
  FullName: string
  IsCurrent: bool
  CreationTime: System.DateTime
  LastAccessTime: System.DateTime
  LastWriteTime: System.DateTime
}

let buildListArgs (sortColString: string) (sortDirString: string) : ListArgs =
  let sortDir: ListDirection =
    match sortDirString.ToLower() with
    | "desc" -> Desc
    | _ -> Asc

  let sortCol: ListColumn =
    match sortColString.ToLower() with
    | "created" -> Created
    | "updated" -> Updated
    | "accessed" -> Accessed
    | _ -> Name

  {
    Column = sortCol
    Direction = sortDir
  }

/// Lists dotenv files for the forward operating base with support for sorting.
/// The "current" dotenv is highlighted.
let list (commandContext: CommandContext) (listParams: ListArgs) =
  let flip = fun func (lhs: 'b) (rhs: 'a) -> func rhs lhs

  let colFunc =
    match listParams.Column with
    | Created -> (fun (l: ListEntry) (r: ListEntry) -> compare l.CreationTime r.CreationTime)
    | Updated -> (fun (l: ListEntry) (r: ListEntry) -> compare l.LastWriteTime r.LastWriteTime)
    | Accessed -> (fun (l: ListEntry) (r: ListEntry) -> compare l.LastAccessTime r.LastAccessTime)
    | Name -> (fun (l: ListEntry) (r: ListEntry) -> compare l.Name r.Name)

  let compareFunc =
    match listParams.Direction with
    | Asc -> colFunc
    | Desc -> flip colFunc

  let asDotenv (context: FileHelpers.CommandFileContext) (info: System.IO.FileSystemInfo) = {
    ListEntry.Name = FileHelpers.asEnvName info.Name
    ListEntry.FullName = info.FullName
    ListEntry.IsCurrent = FileHelpers.isCurrentEnvByPath context info.FullName
    ListEntry.CreationTime = info.CreationTime
    ListEntry.LastAccessTime = info.LastAccessTime
    ListEntry.LastWriteTime = info.LastWriteTime
  }

  try
    [ "dotenvs" ]
    |> FileHelpers.projectPathTo commandContext
    |> FileHelpers.getFileInfos
    |> List.map (asDotenv commandContext)
    |> List.sortWith compareFunc
    |> Ok
  with :? System.IO.DirectoryNotFoundException ->
    Error(sprintf "Project `%s` not found; run fwd init or provide a project name." commandContext.ProjectName)

type SwitchMode =
  | Create
  | ReadOnly

type SwitchArgs = { Name: string; Mode: SwitchMode }

/// Switch the dotenv file, either to an existing one or by creating a new one.
let switch (commandContext: CommandContext) (switchArgs: SwitchArgs) =
  let currentPath: string =
    FileHelpers.projectPathTo commandContext [ ".env.current" ]

  let targetPath: string = FileHelpers.dotenvPath commandContext switchArgs.Name

  let touchExisting _ =
    match FileHelpers.actualPathToCurrentEnv commandContext with
    | Ok path -> System.IO.File.SetLastWriteTimeUtc(path.FullName, System.DateTime.UtcNow)
    | Error _ -> ()

  let replaceInternalSymLink _ =
    touchExisting () |> ignore
    System.IO.File.Delete(currentPath)
    System.IO.File.SetLastWriteTimeUtc(targetPath, System.DateTime.UtcNow)

    FileHelpers.createSymbolicLinkIfMissing currentPath targetPath
    |> Result.bind (fun fileInfo -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  let createDotEnvAndReplaceInternalSymLink _ =
    let currentContent: string = System.IO.File.ReadAllText currentPath
    use newFile: System.IO.StreamWriter = System.IO.File.CreateText targetPath
    newFile.Write(currentContent)

    replaceInternalSymLink ()

  match switchArgs.Mode, FileHelpers.fileOrDirectoryExists targetPath with
  | Create, true -> Error(sprintf "Cannot create `%s`; it already exists" switchArgs.Name)
  | Create, false -> createDotEnvAndReplaceInternalSymLink ()
  | ReadOnly, true -> replaceInternalSymLink ()
  | ReadOnly, false -> Error(sprintf "Cannot switch to `%s`; it does not exist" switchArgs.Name)

type RemoveArgs = { Name: string }

/// Removes the dotenv file from the project.
let remove (commandContext: CommandContext) (removeArgs: RemoveArgs) =
  let fullPath: string = FileHelpers.dotenvPath commandContext removeArgs.Name
  let exists: bool = FileHelpers.fileOrDirectoryExists fullPath
  let isCurrent = FileHelpers.isCurrentEnvByPath commandContext fullPath

  match (exists, isCurrent) with
  | (_, true) -> Error(sprintf "Cannot remove `%s` since it is the current dotenv." removeArgs.Name)
  | (false, _) -> Error(sprintf "Unable to find dotenv `%s` in the current project" removeArgs.Name)
  | (true, _) ->
    System.IO.File.Delete fullPath
    Ok fullPath

type ExplainOutput = {
  RootPath: string
  ProjectName: string
  ProjectPath: string
  DotEnvSymLinkPath: option<string>
  DotEnvPath: option<string>
}

let explain (commandContext: CommandContext) =
  let maybePathToSymLink = FileHelpers.projectPathTo commandContext [ ".env.current" ]

  let pathToSymLink =
    match System.IO.File.Exists maybePathToSymLink with
    | true -> Some maybePathToSymLink
    | false -> None

  let actualPathToCurrentDotEnv =
    match FileHelpers.actualPathToCurrentEnv commandContext with
    | Ok path -> Some path.FullName
    | Error _ -> None

  Ok
    {
      RootPath = commandContext.RootPath
      ProjectName = commandContext.ProjectName
      ProjectPath = commandContext.ProjectPath
      DotEnvSymLinkPath = pathToSymLink
      DotEnvPath = actualPathToCurrentDotEnv
    }

type MoveArgs = { name: string; nextName: string }

let move (moveArgs: MoveArgs) = Error "Not implemented"
