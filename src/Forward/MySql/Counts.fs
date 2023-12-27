module Forward.MySql.Counts

open Dapper.FSharp.MySQL
open dotenv.net
open System.Collections.Generic

open Forward.CommandContext
open Forward.MySql.Connection

type CountEntry = { TableName: string; Count: int64 }

type DotEnvWithTableCounts =
  { DotEnvName: string
    TableCounts: CountEntry seq }

[<CLIMutable>]
type Table =
  { table_name: string
    table_rows: int
    table_schema: string }

let informationSchemaTablesTable: QuerySource<Table> =
  table'<Table> "information_schema.tables"

let allTableCountsTask (conn: ConnectionWithConfig) =
  task {
    let schema: string = conn.Config.DbName

    let! (rawTableCounts: IEnumerable<Table>) =
      select {
        for t: Table in informationSchemaTablesTable do
          where (t.table_schema = schema)
          orderByDescending t.table_rows
      }
      |> conn.Conn.SelectAsync<Table>

    return
      rawTableCounts
      |> Seq.cast<Table>
      |> Seq.map (fun (t: Table) ->
        { TableName = t.table_name
          Count = t.table_rows })
  }

let fetchTableCountAsync (conn: MySql.Data.MySqlClient.MySqlConnection) (tableName) =
  async {
    try
      let table = table'<Table> tableName

      let! (enumWrappedCount: IEnumerable<{| Value: int64 |}>) =
        select {
          for t: Table in table do
            count "*" "Value"
        }
        |> conn.SelectAsync<{| Value: int64 |}>
        |> Async.AwaitTask

      let wrappedCount = enumWrappedCount |> Seq.toList

      return
        { TableName = tableName
          Count = wrappedCount.Head.Value }
    with :? System.AggregateException ->
      return { TableName = tableName; Count = -1 }
  }

/// Gathers the count of each table in the DB referenced by the DotEnv
/// file/project.
let collectDotEnvTableCountsTask (tables: string list) ((dotEnvName, connString): (string * string)) =
  async {
    use conn: MySql.Data.MySqlClient.MySqlConnection =
      new MySql.Data.MySqlClient.MySqlConnection(connString)

    let! _ = conn.OpenAsync() |> Async.AwaitTask
    let! (rawTableCounts: CountEntry array) = tables |> List.map (fetchTableCountAsync conn) |> Async.Sequential

    return
      { DotEnvName = dotEnvName
        TableCounts = rawTableCounts }
  }

/// Returns a list of table counts for each dotenv which provides DB_NAME.
let collectTableCountsPerDotEnvAsync (commandContext: FileCommandContext) (tables: string list) =
  async {
    let hasDbName (dotEnvFile: System.IO.FileSystemInfo) =
      let options: DotEnvOptions =
        new DotEnvOptions(envFilePaths = [ dotEnvFile.FullName ])

      DotEnv.Read(options).ContainsKey("DB_NAME")

    // Loading environment variables is not thread safe, so this doesn't load
    // DotEnv files into the environment. Instead, we load the DotEnv files into
    // Dicts and reference them directly.
    let! (envConnStringPairs: (string * string) array) =
      commandContext
      |> Forward.Project.listDotEnvs
      |> List.filter hasDbName
      |> prepareManyConnectionStringsAsync

    let spawnCountTask = collectDotEnvTableCountsTask tables

    // Wait for all connection strings to be extracted, and then kick off
    // extracting counts.
    return! envConnStringPairs |> Array.map spawnCountTask |> Async.Parallel
  }
