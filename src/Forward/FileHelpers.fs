/// Provides utility functions for working with the file system.
module Forward.FileHelpers

let private stringMaybe (stringOrNull: string) =
  match stringOrNull with
  | null
  | "" -> None
  | _ -> Some stringOrNull

let private orMaybe f (res: 'a option) =
  match res with
  | Some _ as something -> something
  | None -> f ()

// System File/Path/Environment Wrappers
// ****************************************************************************

let currentDirectory () = System.Environment.CurrentDirectory

let getEnvironmentVariable (key: string) =
  System.Environment.GetEnvironmentVariable(key)

let createSymbolicLink (path: string) (targetPath: string) =
  System.IO.File.CreateSymbolicLink(path, targetPath)

let getFileName (filePath: string) = System.IO.Path.GetFileName(filePath)

let getEnvironmentVariableOpt (key: string) =
  key |> getEnvironmentVariable |> stringMaybe

let asEnvName (dotenvFilePath: string) =
  getFileName(dotenvFilePath).Replace(".env.", "")

let asDotenvFileName (name: string) = ".env." + name

/// The full path where fwd stores projects and other artifacts. Precedence is
/// given to the argument. Falls back to $FORWARD_ROOT_PATH if possible,
/// otherwise $HOME/.forward.
let getRootPathOpt (maybePath: string option) =
  maybePath
  |> orMaybe (fun () -> getEnvironmentVariableOpt "FORWARD_ROOT_PATH") // Try environment var
  |> orMaybe (fun () ->
    match getEnvironmentVariableOpt "HOME" with
    | Some path -> Some(System.IO.Path.Combine(path, ".forward"))
    | None -> None)

/// Name of the current project; prefers passed value. Falls back to environment
/// variable, or if unavailable, the basename of the current directory.
let getProjectNameOpt (projectNameOpt: string option) =
  projectNameOpt
  |> orMaybe (fun () -> getEnvironmentVariableOpt "FORWARD_PROJECT_NAME")
  |> orMaybe (fun () -> currentDirectory () |> getFileName |> stringMaybe)

/// The context for generating file paths.
type CommandFileContext =
  { RootPath: string
    ProjectName: string
    ProjectPath: string }

/// Build the file context of a command for the given arguments.
let buildCommandContext (maybeRootPath: string option) (maybeProjectName: string option) =
  let rootPathOpt: string option = getRootPathOpt maybeRootPath
  let projectNameOpt: string option = getProjectNameOpt maybeProjectName

  match rootPathOpt, projectNameOpt with
  | (Some rootPath), (Some projectName) ->
    Ok
      { RootPath = rootPath
        ProjectName = projectName
        ProjectPath = System.IO.Path.Combine(rootPath, projectName) }
  | None, None -> Error "Unable to determine root path and object name."
  | None, _ -> Error "Cannot determine a root path."
  | _, None -> Error "Cannot determine project name."

/// Path to the given parts nested within the project.
let projectPathTo (context: CommandFileContext) (parts: string list) =
  context.ProjectPath :: parts |> List.toArray |> System.IO.Path.Join

/// Returns a full path to the dotenv file.
let dotenvPath (context: CommandFileContext) (name: string) =
  projectPathTo context [ "dotenvs"; ".env." + name ]

/// The path to the parts within the current directory.
let currentPathTo (parts: string list) =
  currentDirectory () :: parts |> List.toArray |> System.IO.Path.Join

/// File info of the given path.
let fileInfo (path: string) : System.IO.FileSystemInfo = new System.IO.FileInfo(path)

/// File info of the underlying dotenv file as a result.
let actualPathToCurrentEnv (context: CommandFileContext) =
  let info: System.IO.FileSystemInfo =
    [ ".env.current" ] |> projectPathTo context |> fileInfo

  match info.Exists with
  | true -> fileInfo info.LinkTarget |> Ok
  | false -> Error "No environment file found"

/// Is the current environment file the given file path?
let isCurrentEnvByPath (context: CommandFileContext) (path: string) =
  match actualPathToCurrentEnv context with
  | Ok info -> info.FullName = path
  | Error _ -> false

/// List of System.IO.FileSystemInfo for each file residing in the given path.
let getFileInfos (path: string) =
  let directoryInfo: System.IO.DirectoryInfo = new System.IO.DirectoryInfo(path)
  directoryInfo.GetFileSystemInfos() |> List.ofArray

/// Check whether a file *or* directory exists for the given path.
let fileOrDirectoryExists (path: string) =
  match System.IO.File.Exists path with
  | true -> true
  | false -> System.IO.Directory.Exists path

/// Apply func to path when it does not already exist.
let whenFileOrDirectoryIsMissing func (path: string) =
  match fileOrDirectoryExists path with
  | true -> Ok(sprintf "`%s` exists" path)
  | false -> func path

/// Create a symbolic link at path that points to the target path.
let createSymbolicLinkIfMissing (path: string) (targetPath: string) =
  match fileOrDirectoryExists path with
  | true -> path |> fileInfo |> Ok
  | false -> targetPath |> createSymbolicLink (path) |> Ok
