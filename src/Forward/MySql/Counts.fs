module Forward.MySql.Counts

open Dapper.FSharp.MySQL
open Forward.MySql.Connection
open System.Collections.Generic

type CountEntry = { TableName: string; Count: int64 }

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
