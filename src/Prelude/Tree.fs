module Tree

/// The Tree type comprises a discriminated union with three types:
///
/// - A branch node that both encodes data and subtrees.
/// - A leaf node that only encodes data.
/// - A root node that holds one or more root subtrees.
///
/// A leaf node is represented as a distinct type for clarity (it would suffice
/// to represent it with a branch node holding an empty subtree). There's an
/// assumption that each node has up to one parent.
type Node<'T> =
  | BranchNode of 'T * seq<Node<'T>>
  | LeafNode of 'T
  | RootNode of seq<Node<'T>>

let ofSeq (getNodeKey: ('T) -> 'K) (tryGetParentNodeKey: ('T) -> option<'K>) (nodes: seq<'T>) =
  let index = nodes |> Seq.groupBy tryGetParentNodeKey |> dict

  let rootChildren = (Dict.propOr (seq []) None index) |> Seq.toList

  let rec assembleDescendants produceCont (nodes: seq<'T>) acc =
    match Seq.toList nodes with
    | [] -> produceCont (List.rev acc)
    | nodeValue :: tail ->
      match Dict.tryProp (Some(getNodeKey nodeValue)) index with
      | None -> assembleDescendants produceCont tail (LeafNode(nodeValue) :: acc)
      | Some(children) ->
        assembleDescendants
          (fun descendants -> assembleDescendants produceCont tail (BranchNode(nodeValue, descendants) :: acc))
          children
          []

  assembleDescendants (fun roots -> RootNode(roots)) rootChildren []

[<TailCall>]
let rec private mapTreeCont cont mapper nodes acc =
  match nodes with
  | [] -> cont (List.rev acc)
  | LeafNode(value) :: nodes -> mapTreeCont cont mapper nodes (LeafNode(mapper value) :: acc)
  | BranchNode(value, descendants) :: nodes ->
    mapTreeCont
      (fun descendants -> mapTreeCont cont mapper nodes (BranchNode(mapper value, descendants) :: acc))
      mapper
      (Seq.toList descendants)
      []
  | RootNode(_) :: _ -> invalidArg "nodes" "Private func should never receive a root"

let mapTree mapper root =
  match root with
  | LeafNode(value) -> LeafNode(mapper value)
  | BranchNode(value, descendants) ->
    mapTreeCont (fun descendants -> BranchNode(mapper value, descendants)) mapper (Seq.toList descendants) []
  | RootNode(descendants) -> mapTreeCont (fun descendants -> RootNode(descendants)) mapper (Seq.toList descendants) []

// TMP
// TMP

type SampleData =
  { Id: int
    ParentId: option<int>
    Label: string }

let makeSample () =
  [ { Id = 1
      ParentId = None
      Label = "Root" }
    { Id = 2
      ParentId = Some(1)
      Label = "Root -> L1(2)" }
    { Id = 3
      ParentId = Some(1)
      Label = "Root -> L1(3)" }
    { Id = 4
      ParentId = Some(2)
      Label = "root -> L1(2) -> L2(4)" } ]
  |> ofSeq (_.Id) (_.ParentId)
