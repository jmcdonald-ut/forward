module ForwardCli.List

open Argu
open Spectre.Console

type SortDir = Forward.Project.ListDirection

type SortCol =
  | [<CustomCommandLine("name")>] Name
  | [<CustomCommandLine("accessed")>] Accessed
  | [<CustomCommandLine("created")>] Created
  | [<CustomCommandLine("updated")>] Updated

type ListArgs =
  | [<CustomCommandLine("--sort-dir")>] SortDirection of SortDir
  | [<CustomCommandLine("--sort-col")>] SortColumn of SortCol
  | [<CustomCommandLine("-l")>] Limit of limit: int
  | [<CustomCommandLine("-t")>] Terse

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | SortDirection _ -> "sort direction (asc|desc); defaults to desc"
      | SortColumn _ -> "sort column (name|created|accessed|updated); defaults to updated"
      | Limit _ -> sprintf "result limit (default=%s)" (System.String.Format("{0:#,0}", System.Int32.MaxValue))
      | Terse -> sprintf "display name only"

// SUBCOMMAND: fwd list
// ****************************************************************************

let handleListCommand (commandContext: Forward.Project.CommandContext) (listArgs: ParseResults<ListArgs>) =
  let limit: int = listArgs.GetResult(Limit, System.Int32.MaxValue)

  let sortCol: string =
    match listArgs.GetResult(SortColumn, defaultValue = Updated) with
    | Accessed -> "accessed"
    | Created -> "created"
    | Updated -> "updated"
    | _ -> "name"

  let sortDir: string =
    match listArgs.GetResult(SortDirection, defaultValue = SortDir.Desc) with
    | SortDir.Desc -> "desc"
    | SortDir.Asc -> "asc"

  let asFormattedDateTime (dateTime: System.DateTime) =
    dateTime.ToString("dd MMM yyyy @ HH:mm:ss")

  let handleListCommandSuccess (listResult: Forward.Project.ListEntry list) =
    let rowLabel (item: Forward.Project.ListEntry) =
      match item with
      | ({ IsCurrent = true; Name = name }) -> sprintf "[green]%s[/]" name
      | ({ Name = name }) -> sprintf "[blue]%s[/]" name

    let addRow (table: Table) (item: Forward.Project.ListEntry) =
      let indicator: string = if item.IsCurrent then "[green]»[/]" else ""
      let label: string = rowLabel item
      let updatedAt: string = asFormattedDateTime item.LastWriteTime
      table.AddRow(indicator, label, updatedAt)

    match listArgs.Contains(Terse) with
    | false ->
      let initTable: Table =
        (new Table()).AddColumns([| " "; "Environment"; "Last Updated" |])

      listResult |> (List.fold addRow initTable) |> AnsiConsole.Write
    | true ->
      listResult
      |> (List.iter (fun (l: Forward.Project.ListEntry) -> printfn "%s" l.Name))

  sortDir
  |> Forward.Project.buildListArgs limit sortCol
  |> Forward.Project.list commandContext
  |> Forward.Result.teeResult handleListCommandSuccess