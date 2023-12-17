module Math

let clamp (min: 'a) (max: 'a) (actual: 'a) =
  match actual with
  | actual when actual < min -> min
  | actual when actual > max -> max
  | actual -> actual
