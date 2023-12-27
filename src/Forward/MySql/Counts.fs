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

/// Gathers the count of each table in the DB referenced by the DotEnv
/// file/project.
let collectDotEnvTableCountsTask (commandContext: FileCommandContext) (dotEnvName: string) (tables: string list) =
  task {
    // Load environment variables for the given dotenv file. I'm making an
    // assumption here that this is safe despite being in a task.
    Forward.Project.loadDotEnv commandContext dotEnvName

    let getVariable = Environment.getEnvironmentVariableOpt
    let! (conn: ConnectionWithConfig) = buildConnectionTask getVariable optionFiles
    let schema: string = conn.Config.DbName

    let! (rawTableCounts: IEnumerable<Table>) =
      select {
        for t: Table in informationSchemaTablesTable do
          where (t.table_schema = schema && isIn t.table_name tables)
          orderByDescending t.table_rows
      }
      |> conn.Conn.SelectAsync<Table>

    let tableCounts =
      rawTableCounts
      |> Seq.cast<Table>
      |> Seq.map (fun (t: Table) ->
        { TableName = t.table_name
          Count = t.table_rows })

    return
      { DotEnvName = dotEnvName
        TableCounts = tableCounts }
  }

/// Returns a list of table counts for each dotenv which provides DB_NAME.
let collectTableCountsPerDotEnvAsync (commandContext: FileCommandContext) (tables: string list) =
  async {
    let hasDbName (dotEnvFile: System.IO.FileSystemInfo) =
      let options: DotEnvOptions =
        new DotEnvOptions(envFilePaths = [ dotEnvFile.FullName ])

      DotEnv.Read(options).ContainsKey("DB_NAME")

    let kickOffCountTask (dotEnvFile: System.IO.FileSystemInfo) =
      tables
      |> collectDotEnvTableCountsTask commandContext dotEnvFile.Name
      |> Async.AwaitTask

    return!
      commandContext
      |> Forward.Project.listDotEnvs
      |> List.filter hasDbName
      |> List.map kickOffCountTask
      |> Async.Parallel
  }
