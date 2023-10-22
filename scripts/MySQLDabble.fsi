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
      table_rows: int };;

let tablesTable = table'<Table> "tables";;

task {
  let! allTables =
    select {
      for t in tablesTable do
        selectAll
    }
    |> conn.SelectAsync<Table>

  printfn "Names: "

  for t in allTables do
    printfn $"\t%s{t.table_name}"
}
|> Async.AwaitTask
|> Async.RunSynchronously;;
