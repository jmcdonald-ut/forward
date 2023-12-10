module Forward.Result

let tee (func: ('ok) -> 'ok0) (result: Result<'ok, 'err>) : Result<'ok, 'err> =
  match result with
  | Ok(value) ->
    func value |> ignore
    Ok(value)
  | Error(reason) -> Error(reason)

let teeResult (func: ('ok) -> 'ok0) (result: Result<'ok, 'err>) : Result<(unit -> unit), 'err> =
  match result with
  | Ok(value) -> Ok(fun () -> value |> func |> ignore)
  | Error(reason) -> Error(reason)
