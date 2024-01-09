module Dict

open System.Collections.Generic

/// Retrieves the prop; raises if the prop isn't found.
let prop (key: 'key) (dict: IDictionary<'key, 'value>) = dict[key]

/// Retrieves the prop and returns `Some value`; returns `None` if the prop
/// isn't found.
let tryProp (key: 'key) (dict: IDictionary<'key, 'value>) =
  if dict.ContainsKey(key) then
    dict |> prop key |> Some
  else
    None

/// Retrieves the prop; returns `defaultValue` if not found.
let propOr (defaultValue: 'value) (key: 'key) (dict: IDictionary<'key, 'value>) =
  match tryProp key dict with
  | Some(value) -> value
  | None -> defaultValue

/// Retrieves the prop; returns the result of invoking `getFallback` with the
/// prop if it isn't found.
let lazyPropOr (getFallback: ('key) -> 'value) (key: 'key) (dict: IDictionary<'key, 'value>) =
  match tryProp key dict with
  | Some(value) -> value
  | None -> getFallback key

/// Retrieves each prop; invokes getFallback to get the fallback value of each
/// prop that isn't found.
let propsOr (getFallback: ('key) -> 'value) (keys: 'key seq) (dict: IDictionary<'key, 'value>) =
  Seq.map (fun (key: 'key) -> lazyPropOr getFallback key dict) keys
