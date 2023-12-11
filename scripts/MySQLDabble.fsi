// See https://blog.tunaxor.me/blog/2021-11-12-Data-Access-In-Fsharp.html
// See https://github.com/Dzoukr/Dapper.FSharp
// See https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html
#r "nuget: Dapper.FSharp";;
#r "nuget: MySql.Data";;

open System;;
open Dapper.FSharp;;
open Dapper.FSharp.MySQL;;
open MySql.Data.MySqlClient;;
Dapper.FSharp.MySQL.OptionTypes.register();;

let connstring = "Host=127.0.0.1;Username=<REPLACE_ME>;Password=<REPLACE_ME>;Database=information_schema";;
let conn = new MySqlConnection(connstring);;
[<CLIMutable>]
type Table =
    { table_name: string
      table_rows: int
      table_schema: string };;

let tablesTable = table'<Table> "tables";;

task {
  let! allTables =
    select {
      for t in tablesTable do
      where (t.table_schema = "<REPLACE_ME>")
      orderByDescending t.table_rows
    }
    |> conn.SelectAsync<Table>

  printfn "| %-40s | %8s |" "Table" "Row #"
  printfn "| %-40s | %8s |" "----------------------------------------" "--------"

  for t in allTables do
    printfn "| %-40s | %8d |" t.table_name t.table_rows
}
|> Async.AwaitTask
|> Async.RunSynchronously;;
