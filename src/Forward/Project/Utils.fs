module Forward.Project.Utils

open dotenv.net
open System.IO

open Forward
open Forward.CommandContext

type ListDirection =
  | Asc
  | Desc

type ListColumn =
  | Name
  | Created
  | Updated
  | Accessed

type ListOrder = ListColumn * ListDirection

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
let asDotEnv (context: FileCommandContext) (info: FileSystemInfo) =
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

let listDotEnvs (commandContext: FileCommandContext) =
  let comparer = makeCompareFun (Updated, Desc)

  let lift (fn: (ListEntry) -> (ListEntry) -> 'a) (lhs: FileSystemInfo) (rhs: FileSystemInfo) =
    fn (asDotEnv commandContext lhs) (asDotEnv commandContext rhs)

  [ "dotenvs" ]
  |> FileHelpers.projectPathTo commandContext
  |> File.directoryFileInfos
  |> List.sortWith (lift comparer)
