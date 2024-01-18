module Forward.Project.DotEnv

open dotenv.net

let readDotEnv (filePath: string) =
  let options: DotEnvOptions = new DotEnvOptions(envFilePaths = [ filePath ])
  DotEnv.Read(options)

let readDotEnvAsync (filePath: string) = async { return readDotEnv filePath }

/// Gets the variable from the DotEnv file; raises if it's not present.
let getVar (key: string) (dotEnvFile: System.IO.FileInfo) =
  dotEnvFile.FullName |> readDotEnv |> Dict.prop key

/// Tries to get the variable from a project dotenv file; returns `None` if the
/// key cannot be found.
let tryGetVar (key: string) (dotEnvFile: System.IO.FileInfo) =
  dotEnvFile.FullName |> readDotEnv |> Dict.tryProp key

/// Gets all variables; uses the result of invoking `getFallback` for any key
/// that cannot be found.
let getVarsOr (getFallback: (string) -> string) (keys: string seq) (dotEnvFile: System.IO.FileSystemInfo) =
  dotEnvFile.FullName |> readDotEnv |> Dict.propsOr getFallback keys
