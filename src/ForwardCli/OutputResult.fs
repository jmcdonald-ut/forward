/// Provides utilities and types so commands can return structured data which
/// the root command can format.
module ForwardCli.OutputResult

open Spectre.Console
open System.Text.Json

type OutputFormat =
  | Standard
  | Json

type TableResult<'row> =
  { Columns: string array
    Folder: Table -> 'row -> Table
    Rows: 'row list }

type CommandResult<'row, 'record> =
  | ListResult of string list
  | StringResult of string
  | ErrorResult of string
  | RecordResult of 'record
  | TableResult of TableResult<'row>

type private SerializableError = { Message: string; Error: bool }

let makeTableResult (columns: string array) (folder: Table -> 'row -> Table) (rows: 'row list) =
  { Columns = columns
    Folder = folder
    Rows = rows }

let formatAndPrintList (format: OutputFormat) (list: string list) =
  match format with
  | Json -> list |> JsonSerializer.Serialize |> printfn "%s"
  | Standard -> List.iter (fun (str: string) -> printfn "%s" str) list

let formatAndPrintTable (format: OutputFormat) (table: TableResult<'row>) =
  match format with
  | Json -> table.Rows |> JsonSerializer.Serialize |> printfn "%s"
  | Standard ->
    new Table()
    |> _.AddColumns(table.Columns)
    |> (fun (spectreTable: Table) -> List.fold table.Folder spectreTable table.Rows)
    |> AnsiConsole.Write

let formatAndPrintRecord (format: OutputFormat) (record: 'record) =
  match format with
  | Json -> record |> JsonSerializer.Serialize |> printfn "%s"
  | Standard -> printfn "%O" record

let formatAndPrintString (format: OutputFormat) (string: string) =
  match format with
  | Json -> string |> JsonSerializer.Serialize |> printfn "%s"
  | Standard -> printfn "%s" string

let formatAndPrintError (format: OutputFormat) (error: string) =
  match format with
  | Json -> { Message = error; Error = true } |> JsonSerializer.Serialize |> printfn "%s"
  | Standard -> AnsiConsole.MarkupLine(sprintf "[red]%s[/]" error)

let formatAndPrintResult (format: OutputFormat) (result: CommandResult<'row, 'record>) =
  match result with
  | ListResult(list) -> formatAndPrintList format list
  | StringResult(string) -> formatAndPrintString format string
  | TableResult(table) -> formatAndPrintTable format table
  | RecordResult(record) -> formatAndPrintRecord format record
  | ErrorResult(error) -> formatAndPrintError format error

let formatPrintAndClassify (format: OutputFormat) (result: CommandResult<'row, 'record>) =
  match result with
  | ErrorResult(_) as errorResult ->
    formatAndPrintResult format errorResult
    1
  | result ->
    formatAndPrintResult format result
    0
