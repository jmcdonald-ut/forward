/// Provides utility functions for working with the file system.
module Forward.FileHelpers

// System File/Path/Environment Wrappers
// ****************************************************************************

/// The full path where fwd stores projects and other artifacts. Precedence is
/// given to the argument. Falls back to $FORWARD_ROOT_PATH if possible,
/// otherwise $HOME/.forward.
let getRootPathOpt = CommandContext.getRootPathOpt

/// Path to the given parts nested within the project.
let projectPathTo (context: CommandContext.FileCommandContext) (parts: string list) =
  context.ProjectPath :: parts |> List.toArray |> System.IO.Path.Join

/// Returns a full path to the dotenv file.
let dotenvPath (context: CommandContext.FileCommandContext) (name: string) =
  projectPathTo context [ "dotenvs"; ".env." + name ]

/// The path to the parts within the current directory.
let currentPathTo (parts: string list) =
  Environment.currentDirectory () :: parts |> List.toArray |> System.IO.Path.Join

/// File info of the underlying dotenv file as a result.
let actualPathToCurrentEnv (context: CommandContext.FileCommandContext) =
  let info: System.IO.FileSystemInfo =
    [ ".env.current" ] |> projectPathTo context |> File.fileInfo

  match info.Exists with
  | true -> File.fileInfo info.LinkTarget |> Ok
  | false -> Error "No environment file found"

/// Is the current environment file the given file path?
let isCurrentEnvByPath (context: CommandContext.FileCommandContext) (path: string) =
  match actualPathToCurrentEnv context with
  | Ok info -> info.FullName = path
  | Error _ -> false

/// Apply func to path when it does not already exist.
let whenFileOrDirectoryIsMissing func (path: string) =
  match File.exists path with
  | true -> Ok(sprintf "`%s` exists" path)
  | false -> func path

/// Create a symbolic link at path that points to the target path.
let createSymbolicLinkIfMissing (path: string) (targetPath: string) =
  match File.exists path with
  | true -> path |> File.fileInfo |> Ok
  | false -> targetPath |> File.createSymbolicLink path |> Ok
