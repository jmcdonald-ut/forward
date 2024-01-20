module Tree

open System.Collections.Generic

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

type private SeqIndex<'T, 'K when 'K: equality>
  (getNodeKey: ('T) -> 'K, tryGetParentNodeKey: ('T) -> option<'K>, nodes: seq<'T>) =
  let state: IDictionary<option<'K>, seq<'T>> =
    nodes |> Seq.groupBy tryGetParentNodeKey |> dict

  member index.Roots() = Dict.propOr (seq []) None state

  member index.TryGetNodeDescendants(node: 'T) =
    match Dict.tryProp (Some(getNodeKey node)) state with
    | None -> None
    | Some(empty) when Seq.isEmpty empty -> None
    | Some(descendants) -> Some(descendants)

[<TailCall>]
let rec private assembleDescendants (index: SeqIndex<'T, 'K>) k nodes acc =
  let recurse = assembleDescendants index

  match Seq.tryHead nodes with
  | None -> k (Seq.rev acc)
  | Some(nodeValue) ->
    match index.TryGetNodeDescendants(nodeValue) with
    | None -> recurse k (Seq.tail nodes) (Seq.insertAt 0 (LeafNode nodeValue) acc)
    | Some(descendants) ->
      recurse
        (fun (descendants: seq<Node<'T>>) ->
          recurse k (Seq.tail nodes) (Seq.insertAt 0 (BranchNode(nodeValue, descendants)) acc))
        descendants
        (seq [])

let ofSeq (getNodeKey: ('T) -> 'K) (tryGetParentNodeKey: ('T) -> option<'K>) (nodes: seq<'T>) =
  let index: SeqIndex<'T, 'K> =
    new SeqIndex<'T, 'K>(getNodeKey, tryGetParentNodeKey, nodes)

  assembleDescendants index (fun (roots: seq<Node<'T>>) -> RootNode(roots)) (index.Roots()) (seq [])

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

let mapTree mapper (root: Node<'a>) =
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
  let l: seq<SampleData> =
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

  ofSeq (_.Id) (_.ParentId) l
