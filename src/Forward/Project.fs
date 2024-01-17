module Forward.Project

open dotenv.net
open System.IO

type ListDirection =
  | Asc
  | Desc

type ListColumn =
  | Name
  | Created
  | Updated
  | Accessed

type ListOrder = ListColumn * ListDirection

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

/// Maps a FileSystemInfo object into a ListEntry based on the current command
/// context; the ListEntry is a subset of interesting fields *plus* a flag
/// indicating whether the file is the current env.
let asDotEnv (context: CommandContext.FileCommandContext) (info: FileSystemInfo) =
  { ListEntry.Name = (File.fileName info.Name).Replace(".env.", "")
    ListEntry.FullName = info.FullName
    ListEntry.IsCurrent = FileHelpers.isCurrentEnvByPath context info.FullName
    ListEntry.CreationTime = info.CreationTime
    ListEntry.LastAccessTime = info.LastAccessTime
    ListEntry.LastWriteTime = info.LastWriteTime }

/// Makes a "comparer" function which returns an integer indicating the order of
/// the left hand side relative to the right hand side.
let makeCompareFun (colDir: ListOrder) =
  let (column, direction) = colDir
  let flip = fun func (lhs: 'b) (rhs: 'a) -> func rhs lhs

  let colFunc =
    match column with
    | Created -> (fun (l: ListEntry) (r: ListEntry) -> compare l.CreationTime r.CreationTime)
    | Updated -> (fun (l: ListEntry) (r: ListEntry) -> compare l.LastWriteTime r.LastWriteTime)
    | Accessed -> (fun (l: ListEntry) (r: ListEntry) -> compare l.LastAccessTime r.LastAccessTime)
    | Name -> (fun (l: ListEntry) (r: ListEntry) -> compare l.Name r.Name)

  match direction with
  | Asc -> colFunc
  | Desc -> flip colFunc

let listDotEnvs (commandContext: CommandContext.FileCommandContext) =
  let comparer = makeCompareFun (Updated, Desc)

  let lift (fn: (ListEntry) -> (ListEntry) -> 'a) (lhs: FileSystemInfo) (rhs: FileSystemInfo) =
    fn (asDotEnv commandContext lhs) (asDotEnv commandContext rhs)

  [ "dotenvs" ]
  |> FileHelpers.projectPathTo commandContext
  |> File.directoryFileInfos
  |> List.sortWith (lift comparer)

let loadCurrentDotEnv (commandContext: CommandContext.FileCommandContext) =
  match FileHelpers.actualPathToCurrentEnv commandContext with
  | Ok(path: System.IO.FileSystemInfo) -> DotEnv.Load(new DotEnvOptions(envFilePaths = [ path.FullName ]))
  | Error(_) -> ()

let readDotEnvAsync (filePath: string) =
  async {
    let options: DotEnvOptions = new DotEnvOptions(envFilePaths = [ filePath ])
    return DotEnv.Read(options)
  }

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
  try
    let fileList: FileSystemInfo list = listDotEnvs commandContext
    let comparer = makeCompareFun (listParams.Column, listParams.Direction)

    fileList
    |> List.map (asDotEnv commandContext)
    |> List.sortWith comparer
    |> List.take (Math.clamp 0 fileList.Length listParams.Limit)
    |> Ok
  with :? DirectoryNotFoundException ->
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
let remove (commandContext: CommandContext.FileCommandContext) (dotEnvName: string) =
  let fullPath: string = FileHelpers.dotenvPath commandContext dotEnvName
  let exists: bool = File.exists fullPath
  let isCurrent = FileHelpers.isCurrentEnvByPath commandContext fullPath

  match (exists, isCurrent) with
  | (_, true) -> Error(sprintf "Cannot remove `%s` since it is the current dotenv." dotEnvName)
  | (false, _) -> Error(sprintf "Unable to find dotenv `%s` in the current project" dotEnvName)
  | (true, _) -> File.deleteFile fullPath

let explain (commandContext: CommandContext.FileCommandContext) : Result<CommandContext.DotEnvCommandContext, string> =
  let getDotEnvPath = FileHelpers.actualPathToCurrentEnv >> Result.map (_.FullName)
  let dotEnvPath: string option = commandContext |> getDotEnvPath |> Result.toOption

  let maybePathToSymLink: string =
    FileHelpers.projectPathTo commandContext [ ".env.current" ]

  let pathToSymLink: string option =
    match File.exists maybePathToSymLink with
    | true -> Some maybePathToSymLink
    | false -> None

  Ok
    { RootPath = commandContext.RootPath
      ProjectName = commandContext.ProjectName
      ProjectArtifactsPath = commandContext.ProjectArtifactsPath
      DotEnvSymLinkPath = pathToSymLink
      DotEnvPath = dotEnvPath }
