module Forward.CommandContext

type FileCommandContext =
  { RootPath: string
    ProjectName: string
    ProjectPath: string }

type DotEnvCommandContext =
  { RootPath: string
    ProjectName: string
    ProjectPath: string
    DotEnvSymLinkPath: option<string>
    DotEnvPath: option<string> }

/// The full path where fwd stores projects and other artifacts. Precedence is
/// given to the argument. Falls back to $FORWARD_ROOT_PATH if possible,
/// otherwise $HOME/.forward.
let getRootPathOpt (maybePath: string option) =
  maybePath
  |> Option.orElseWith (fun () -> Environment.getEnvironmentVariableOpt "FORWARD_ROOT_PATH")
  |> Option.orElseWith (fun () ->
    match Environment.getEnvironmentVariableOpt "HOME" with
    | Some path -> Some(File.combinePaths path ".forward")
    | None -> None)

/// Name of the current project; prefers passed value. Falls back to environment
/// variable, or if unavailable, the basename of the current directory.
let getProjectNameOpt (projectNameOpt: string option) =
  projectNameOpt
  |> Option.orElseWith (fun () -> Environment.getEnvironmentVariableOpt "FORWARD_PROJECT_NAME")
  |> Option.orElseWith (fun () -> Environment.currentDirectory () |> File.fileName |> Some)

/// Build the file context of a command for the given arguments.
let buildFileCommandContext (maybeRootPath: string option) (maybeProjectName: string option) =
  let rootPathOpt: string option = getRootPathOpt maybeRootPath
  let projectNameOpt: string option = getProjectNameOpt maybeProjectName

  match rootPathOpt, projectNameOpt with
  | (Some rootPath), (Some projectName) ->
    Ok
      { RootPath = rootPath
        ProjectName = projectName
        ProjectPath = File.combinePaths rootPath projectName }
  | None, None -> Error "Unable to determine root path and object name."
  | None, _ -> Error "Cannot determine a root path."
  | _, None -> Error "Cannot determine project name."
