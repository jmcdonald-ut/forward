module File

open System

/// Create a symbolic link to path at the target path.
let createSymbolicLink (path: string) (targetPath: string) =
  IO.File.CreateSymbolicLink(path, targetPath)

/// Check whether a file exists for the given path.
let fileExists (path: string) = IO.File.Exists path

/// Check whether a directory exists for the given path.
let directoryExists (path: string) = IO.Directory.Exists path

/// Check whether a file *or* directory exists for the given path.
let exists (path: string) =
  (fileExists path) || (directoryExists path)

/// Returns the file name of the given path.
let fileName (path: string) = IO.Path.GetFileName(path)

/// File info of the given path.
let fileInfo (path: string) : IO.FileSystemInfo = new IO.FileInfo(path)

/// Combines two path names into one.
let combinePaths (path1: string) (path2: string) = IO.Path.Combine(path1, path2)

let combinePaths3 (path1: string) (path2: string) (path3: string) =
  path3 |> combinePaths path2 |> combinePaths path1

/// Gets directory info of the given path.
let directoryInfo (path: string) : IO.DirectoryInfo = new IO.DirectoryInfo(path)

/// Gets a sequence of file infos at the given path.
let directoryFileInfos (path: string) =
  seq { yield! (directoryInfo path).GetFileSystemInfos() }

let joinPaths (path1: string) (path2: string) = IO.Path.Join(path1, path2)

/// Creates a directory
let createDirectory (path: string) = IO.Directory.CreateDirectory path

let deleteFile (path: string) =
  try
    IO.File.Delete path
    Ok path
  with :? SystemException as ex ->
    Error ex.Message

let readFileIn (path: string) = IO.File.ReadAllText path
let readFileLinesIn (path: string) = IO.File.ReadAllLines path

let writeText (text: string) (path: string) = IO.File.WriteAllText(path, text)
