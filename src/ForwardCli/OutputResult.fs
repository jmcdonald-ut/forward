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
    Rows: seq<'row> }

type CommandResult<'record> =
  | SeqResult of seq<string>
  | StringResult of string
  | ErrorResult of string
  | RecordResult of 'record
  | TableResult of TableResult<'record>

type SerializableError = { Message: string; Error: bool }

let makeTableResult (columns: string array) (folder: Table -> 'row -> Table) (rows: seq<'row>) =
  { Columns = columns
    Folder = folder
    Rows = rows }

let recordResultOf (result: Result<'a, string>) =
  match result with
  | Ok(a) -> RecordResult(a)
  | Error(reason) -> ErrorResult(reason)

let stringResultOf (result: Result<string, string>) =
  match result with
  | Ok(a) -> StringResult(a)
  | Error(reason) -> ErrorResult(reason)

let formatAndPrintJson (value: 'a) =
  value |> JsonSerializer.Serialize |> printfn "%s"

let printTableStandardFormat (table: TableResult<'row>) =
  new Table()
  |> _.AddColumns(table.Columns)
  |> (fun (spectreTable: Table) -> Seq.fold table.Folder spectreTable table.Rows)
  |> AnsiConsole.Write

let formatAndPrintResult (format: OutputFormat) (result: CommandResult<'record>) =
  match format, result with
  | Json, SeqResult(value) -> formatAndPrintJson value
  | Json, StringResult(value) -> formatAndPrintJson value
  | Json, TableResult(value) -> formatAndPrintJson value.Rows
  | Json, RecordResult(value) -> formatAndPrintJson value
  | Json, ErrorResult(value) -> formatAndPrintJson { Message = value; Error = true }
  | Standard, SeqResult(sequence) -> Seq.iter (fun (str: string) -> printfn "%s" str) sequence
  | Standard, StringResult(string) -> printfn "%s" string
  | Standard, TableResult(table) -> printTableStandardFormat table
  | Standard, RecordResult(record) -> printfn "%O" record
  | Standard, ErrorResult(error) -> AnsiConsole.MarkupLine(sprintf "[red]%s[/]" error)

let printAndExit (format: OutputFormat) (squelchError: bool) (result: CommandResult<'record>) =
  match squelchError, result with
  | true, ErrorResult(_) -> 1
  | false, (ErrorResult(_) as errorResult) ->
    formatAndPrintResult format errorResult
    1
  | _, result ->
    formatAndPrintResult format result
    0
