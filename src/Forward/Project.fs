module Forward.Project

/// Initializes a new project if one does't already exist. If one does exist,
/// this re-runs the init logic in a non-destructive manner.
let init (commandContext: CommandContext.FileCommandContext) : Result<string, string> =
  let createDirectory _ =
    commandContext.ProjectArtifactsPath
    |> FileHelpers.whenFileOrDirectoryIsMissing (fun (path: string) ->
      Ok(sprintf "`%s` created" (File.createDirectory path).FullName))

  let createDotEnvsDirectory _ =
    "dotenvs"
    |> File.combinePaths commandContext.ProjectArtifactsPath
    |> FileHelpers.whenFileOrDirectoryIsMissing (fun (fullPath: string) ->
      Ok(sprintf "`%s` created" (File.createDirectory fullPath).FullName))

  let createDotEnv _ =
    ".env.main"
    |> File.combinePaths3 commandContext.ProjectArtifactsPath "dotenvs"
    |> FileHelpers.whenFileOrDirectoryIsMissing (fun (fullPath: string) ->
      // TODO: Copy .env if possible.
      use stream: System.IO.StreamWriter = System.IO.File.CreateText fullPath
      stream.WriteLine("# Generated .env")
      Ok(sprintf "`%s` created" fullPath))

  let createInternalSymLink _ =
    let targetPath: string =
      FileHelpers.projectPathTo commandContext [ "dotenvs"; ".env.main" ]

    [ ".env.current" ]
    |> FileHelpers.projectPathTo commandContext
    |> FileHelpers.createSymbolicLinkIfMissing targetPath
    |> Result.bind (fun (fileInfo: System.IO.FileSystemInfo) -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  let createSymLinkInProject _ =
    let path: string = File.combinePaths commandContext.ProjectPath ".env"
    let targetPath: string = FileHelpers.projectPathTo commandContext [ ".env.current" ]

    FileHelpers.createSymbolicLinkIfMissing path targetPath
    |> Result.bind (fun (fileInfo: System.IO.FileSystemInfo) -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  commandContext.ProjectArtifactsPath
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

type ListArgs =
  { Column: ListColumn
    Direction: ListDirection
    Limit: int }

type ListEntry =
  { Name: string
    FullName: string
    IsCurrent: bool
    CreationTime: System.DateTime
    LastAccessTime: System.DateTime
    LastWriteTime: System.DateTime }

let buildListArgs (limit: int) (sortColString: string) (sortDirString: string) : ListArgs =
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

  { Column = sortCol
    Direction = sortDir
    Limit = limit }

/// Lists dotenv files for the forward operating base with support for sorting.
/// The "current" dotenv is highlighted.
let list (commandContext: CommandContext.FileCommandContext) (listParams: ListArgs) =
  let flip = fun func (lhs: 'b) (rhs: 'a) -> func rhs lhs

  let colFunc =
    match listParams.Column with
    | Created -> (fun (l: ListEntry) (r: ListEntry) -> compare l.CreationTime r.CreationTime)
    | Updated -> (fun (l: ListEntry) (r: ListEntry) -> compare l.LastWriteTime r.LastWriteTime)
    | Accessed -> (fun (l: ListEntry) (r: ListEntry) -> compare l.LastAccessTime r.LastAccessTime)
    | Name -> (fun (l: ListEntry) (r: ListEntry) -> compare l.Name r.Name)

  let comparer =
    match listParams.Direction with
    | Asc -> colFunc
    | Desc -> flip colFunc

  let asDotenv (context: CommandContext.FileCommandContext) (info: System.IO.FileSystemInfo) =
    { ListEntry.Name = (File.fileName info.Name).Replace(".env.", "")
      ListEntry.FullName = info.FullName
      ListEntry.IsCurrent = FileHelpers.isCurrentEnvByPath context info.FullName
      ListEntry.CreationTime = info.CreationTime
      ListEntry.LastAccessTime = info.LastAccessTime
      ListEntry.LastWriteTime = info.LastWriteTime }

  try
    let fileList: System.IO.FileSystemInfo list =
      [ "dotenvs" ]
      |> FileHelpers.projectPathTo commandContext
      |> File.directoryFileInfos

    fileList
    |> List.map (asDotenv commandContext)
    |> List.sortWith comparer
    |> List.take (Math.clamp 0 fileList.Length listParams.Limit)
    |> Ok
  with :? System.IO.DirectoryNotFoundException ->
    Error(sprintf "Project `%s` not found; run fwd init or provide a project name." commandContext.ProjectName)

type SwitchMode =
  | Create
  | ReadOnly

/// Switch the dotenv file, either to an existing one or by creating a new one.
let switch (commandContext: CommandContext.FileCommandContext) (name: string) (mode: SwitchMode) =
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
    |> Result.bind (fun fileInfo -> Ok(sprintf "`%s` symlink created" fileInfo.FullName))

  let createDotEnvAndReplaceInternalSymLink _ =
    let currentContent: string = System.IO.File.ReadAllText currentPath
    use newFile: System.IO.StreamWriter = System.IO.File.CreateText targetPath
    newFile.Write(currentContent)

    replaceInternalSymLink ()

  match mode, File.exists targetPath with
  | Create, true -> Error(sprintf "Cannot create `%s`; it already exists" name)
  | Create, false -> createDotEnvAndReplaceInternalSymLink ()
  | ReadOnly, true -> replaceInternalSymLink ()
  | ReadOnly, false -> Error(sprintf "Cannot switch to `%s`; it does not exist" name)

/// Removes the dotenv file from the project.
let remove (commandContext: CommandContext.FileCommandContext) (dotEnvName: string) =
  let fullPath: string = FileHelpers.dotenvPath commandContext dotEnvName
  let exists: bool = File.exists fullPath
  let isCurrent = FileHelpers.isCurrentEnvByPath commandContext fullPath

  match (exists, isCurrent) with
  | (_, true) -> Error(sprintf "Cannot remove `%s` since it is the current dotenv." dotEnvName)
  | (false, _) -> Error(sprintf "Unable to find dotenv `%s` in the current project" dotEnvName)
  | (true, _) -> File.deleteFile fullPath

let explain (commandContext: CommandContext.FileCommandContext) : Result<CommandContext.DotEnvCommandContext, string> =
  let maybePathToSymLink = FileHelpers.projectPathTo commandContext [ ".env.current" ]

  let pathToSymLink =
    match File.exists maybePathToSymLink with
    | true -> Some maybePathToSymLink
    | false -> None

  let actualPathToCurrentDotEnv =
    match FileHelpers.actualPathToCurrentEnv commandContext with
    | Ok path -> Some path.FullName
    | Error _ -> None

  Ok
    { RootPath = commandContext.RootPath
      ProjectName = commandContext.ProjectName
      ProjectArtifactsPath = commandContext.ProjectArtifactsPath
      DotEnvSymLinkPath = pathToSymLink
      DotEnvPath = actualPathToCurrentDotEnv }
