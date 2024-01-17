module Forward.Tests.LibTest.TempEnvSetUp

open System.IO

let createTempDir (prefix: string) =
  Directory.CreateTempSubdirectory(prefix)

type TempTestEnv() =
  let forwardRoot: DirectoryInfo = createTempDir "fwd_root"
  let projectsRoot: DirectoryInfo = createTempDir "fwd_projects"
  let projectRoot: DirectoryInfo = createTempDir "fwd_codebase"
  let priorRoot: string = Environment.getEnvironmentVariable "FORWARD_ROOT_PATH"

  let forwardProjectRoot =
    "fwd_codebase/dotenvs"
    |> File.joinPaths projectsRoot.FullName
    |> File.createDirectory

  let priorProjectName: string =
    Environment.getEnvironmentVariable "FORWARD_PROJECT_NAME"

  do
    let initEnvPath = File.joinPaths forwardProjectRoot.FullName ".env.init"
    File.writeText "VAR=value" initEnvPath

    let symPath =
      ".env.current" |> File.combinePaths3 projectsRoot.FullName "fwd_codebase"

    File.createSymbolicLink symPath initEnvPath |> ignore

    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" forwardRoot.FullName
    Environment.setEnvironmentVariable "FORWARD_PROJECT_NAME" projectRoot.Name

  member this.ForwardRoot = forwardRoot
  member this.ProjectsRoot = projectsRoot
  member this.ProjectRoot = projectRoot
  member this.PriorRoot = priorRoot
  member this.PriorProjectName = priorProjectName

  member this.TearDown() =
    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" this.PriorRoot
    Environment.setEnvironmentVariable "FORWARD_PROJECT_NAME" this.PriorProjectName

    Directory.Delete(this.ForwardRoot.FullName, true)
    Directory.Delete(this.ProjectsRoot.FullName, true)
    Directory.Delete(this.ProjectRoot.FullName, true)

  member this.GetPathToDotEnvCurrent() =
    File.joinPaths projectsRoot.FullName "fwd_codebase/.env.current"

  member this.GetPathToDotEnv(name: string) =
    File.joinPaths forwardProjectRoot.FullName (sprintf ".env.%s" name)

  member this.CreateDotEnv(name: string, contents: string) =
    File.writeText contents (File.joinPaths forwardProjectRoot.FullName name)

  member this.CommandContext() : Forward.CommandContext.FileCommandContext =
    { ProjectName = "fwd_codebase"
      ProjectArtifactsPath = Path.Join([| this.ProjectsRoot.FullName; "fwd_codebase" |])
      RootPath = this.ForwardRoot.FullName
      ProjectPath = this.ProjectRoot.FullName }
