﻿module Bio.TreeExport
open AbstractAnalysis.Common

open Yard.Generators.GLL
//open Yard.Generators.GLL.Parser
open Yard.Generators
open Microsoft.FSharp.Collections

open YC.API
open Yard.Generators.GLL.ParserCommon
open System.Collections.Generic
open Yard.Generators.GLL.AbstractParser
open Yard.Frontends.YardFrontend
open Yard.Generators.GLL
open Yard.Core.Conversions.ExpandMeta

open QuickGraph
open QuickGraph.Graphviz

open Yard.Generators.Common.ASTGLLFSA
open Yard.Generators.Common.FSA

open System.Drawing

open System
open System.Collections.Generic
open System.Windows.Forms

let grammar = @"../../../YC.GrammarZOO/Bio/16s/R16S_19_27.yrd"

let getData dataPath isFasta = 
    if not isFasta
    then
        let d = new ResizeArray<_>()
        
        System.IO.File.ReadAllLines(dataPath)
        |> Array.pairwise
        |> Array.iteri(fun i x -> if i % 2 = 0 then d.Add(x))
        d.ToArray()
    else
        let d = new System.Text.StringBuilder()
        let meta = ref ""
        let lst = new ResizeArray<_>()
        for s in System.IO.File.ReadAllLines(dataPath) do
            if s.[0] = '>'
            then
                if !meta <> ""
                then 
                    lst.Add(!meta, d.ToString())
                    d.Clear() |> ignore
                    meta := s
                else
                    meta := s
            else
                d.Append s |> ignore
        lst.Add(!meta, d.ToString())
        lst.ToArray()

let parserSource = 
    let fe = new YardFrontend()
    let gen = new GLL()
    let conv = seq{yield new ExpandMeta()}
    printfn "Generating parser"
    generate grammar
             fe gen 
             None
             conv
             [|""|]
             //[ "ExpandEbnf"; "ExpandMeta"; "ExpandInnerAlt"; "AddDefaultAC"; "Linearize"]
             [] :?> ParserSourceGLL

let getTokens (line : string) =
    line.ToCharArray()

let tokenizer (x : char) =
    System.Char.ToUpper x
    |> (fun x ->
        match x with 
        | 'A' | 'C' | 'G' ->  x.ToString()
        | _ -> "U")
    |> parserSource.StringToToken

let getLinearInputWithAllStartingPos line = 
    let tokens = 
        getTokens line
        |> Array.map tokenizer

    //printfn "%A" tokens
    let startPoss = 
        tokens
        |> Array.mapi(fun i x -> i* 1<positionInInput>)
    //new LinearInput(startPoss,tokens)
    new LinearInput([|0<positionInInput>|],tokens)
    
let toDot fileName (graph : AdjacencyGraph<string*int, TaggedEdge<string*int, bool>>)  =
    let printer = GraphvizAlgorithm(graph)
    printer.CommonVertexFormat.Shape <- Dot.GraphvizVertexShape.Ellipse
    printer.FormatEdge.Add(fun (e:FormatEdgeEventArgs<_,_>) -> e.EdgeFormatter.Style <- if e.Edge.Tag then Dot.GraphvizEdgeStyle.Bold else Dot.GraphvizEdgeStyle.Unspecified)
    printer.FormatEdge.Add(fun (e:FormatEdgeEventArgs<_,_>) -> e.EdgeFormatter.StrokeColor <- if e.Edge.Tag then System.Drawing.Color.Blue else System.Drawing.Color.Black)//if e.Edge.Tag then 5 else 10)
//    printer.FormatEdge.Add(fun (e:FormatEdgeEventArgs<_,_>) -> 
//        e.EdgeFormatter.Comment <- if e.Edge.Tag then "qwe" else "")
//            if e.Edge.Tag
//            then
//                let x = new Dot.GraphvizEdgeLabel()
//                let y = new Dictionary<_,_>()
//                y.Add("Color", "Green")
//                x.AddParameters(y)
//                x
//            else new Dot.GraphvizEdgeLabel())//if e.Edge.Tag then 5 else 10)
    printer.FormatVertex.Add(fun (v:FormatVertexEventArgs<_>) -> v.VertexFormatter.Label <- sprintf "%s %i" (fst v.Vertex) (snd v.Vertex))  
    let str = printer.Generate()        
            
    System.IO.File.WriteAllText(fileName, str)

let treeToGraph tree = 
    let graph = new AdjacencyGraph<string*int, TaggedEdge<string*int, bool>>()
    let stemConnections = new ResizeArray<_>()
    let rec getVerts = function
        | SPPFNonterminal (l, name) ->
            let verts = 
                l
                |> Array.collect (fun x -> getVerts x)
            if name.Contains "toCount" && l.Length > 1
            then
                stemConnections.Add(verts.[0], verts.[verts.Length-1])
            verts
        | SPPFTerminal (name,left,right) ->
            [|name,left|]

    let verts = getVerts tree
    
    printfn "Stem connections: %i" stemConnections.Count

    verts
    |> graph.AddVertexRange |> ignore
    
    verts
    |> Array.pairwise
    |> Array.iter (fun (source,target) ->
        graph.AddEdge(new TaggedEdge<_,_>(source, target, false)) |> ignore)
    
    stemConnections
    |> Seq.iter(fun (source,target) ->
        graph.AddEdge(new TaggedEdge<_,_>(source, target, true)) |> ignore)

    graph

let treeToMsaglGraph tree = 
    let graph = new Microsoft.Msagl.Drawing.Graph("graph")
    let stemConnections = new ResizeArray<_>()
    let rec getVerts = function
        | SPPFNonterminal (l, name) ->
            let verts = 
                l
                |> Array.collect (fun x -> getVerts x)
            if name.Contains "toCount" && l.Length > 1
            then
                stemConnections.Add(verts.[0], verts.[verts.Length-1])
            verts
        | SPPFTerminal (name,left,right) ->
            [|name,left|]

    let verts = getVerts tree
    
    printfn "Stem connections: %i" stemConnections.Count

    verts
    |> Array.pairwise
    |> Array.iter (fun (source,target) ->
        graph.AddEdge(source.ToString(),target.ToString()) |> ignore)
    
    stemConnections
    |> Seq.iter(fun (source,target) ->
        graph.AddEdge(source.ToString(),target.ToString()).Attr.Length <- 0.3)

    graph

//let treeToMsaglGraph tree = 
//    let graph = new Microsoft.Msagl.Drawing.Graph("graph")
//    let stemConnections = new ResizeArray<_>()
//    let rec getVerts = function
//        | SPPFNonterminal (l, name) ->
//            let verts = 
//                l
//                |> Array.collect (fun x -> getVerts x)
//            if name.Contains "toCount" && l.Length > 1
//            then
//                stemConnections.Add(verts.[0], verts.[verts.Length-1])
//            verts
//        | SPPFTerminal (name,left,right) ->
//            [|name,left|]
//
//    let verts = getVerts tree
//    
//    printfn "Stem connections: %i" stemConnections.Count
//
//    let dict = new Dictionary<_,_>()
//
//    stemConnections
//    |> Seq.iter(fun (source,target) ->
//        dict.Add(source.ToString(),sprintf "%A---%A" source target)
//        dict.Add(target.ToString(),sprintf "%A---%A" source target))
//
//    verts
//    |> Array.pairwise
//    |> Array.iter (fun (source,target) ->
//        if dict.ContainsKey(source.ToString()) && dict.ContainsKey(target.ToString())
//        then
//            graph.AddEdge(dict.[source.ToString()],dict.[target.ToString()]) |> ignore
//        elif dict.ContainsKey(source.ToString())
//        then
//            graph.AddEdge(dict.[source.ToString()],target.ToString()) |> ignore
//        elif dict.ContainsKey(target.ToString())
//        then
//            graph.AddEdge(source.ToString(),dict.[target.ToString()]) |> ignore
//        else
//            graph.AddEdge(source.ToString(),target.ToString()) |> ignore)
//    
//    
////    stemConnections
////    |> Seq.iter(fun (source,target) ->
////        graph.AddEdge(source.ToString(),target.ToString()).Attr.Length <- 0.3)
//
//    graph

let showTree tree = 
    let form = new System.Windows.Forms.Form()

    let form = new System.Windows.Forms.Form()
    let viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer()
    let graph = treeToMsaglGraph tree

    viewer.Graph <- graph
    form.SuspendLayout()
    viewer.Dock <- System.Windows.Forms.DockStyle.Fill
    form.Controls.Add(viewer)
    form.ResumeLayout()
    form.ShowDialog() |> ignore    

let treeToSecondaryStructureAndSeq tree =
    let stemConnections = new ResizeArray<_>()
    let rec getSecondaryStructure = function
        | SPPFNonterminal (l, name) ->
            let verts = 
                l
                |> Array.collect (fun x -> getSecondaryStructure x)
            if name.Contains "toCount" && l.Length > 1
            then
                verts.[0] <- '('
                verts.[verts.Length-1] <- ')'
            verts
        | SPPFTerminal (name,left,right) ->
            [|'.'|]

    let rec getSequence : _ -> string []= function
        | SPPFNonterminal (l, name) ->
            l
            |> Array.collect (fun x -> getSequence x)
        | SPPFTerminal (name,left,right) ->
            [|name|]

    let secondaryStructure = getSecondaryStructure tree
    let sequence = getSequence tree
    let sb = new System.Text.StringBuilder()
    for i in secondaryStructure do
        sb.Append(i) |> ignore
    let s1 = sb.ToString()
    sb.Clear() |> ignore
    for i in sequence do
        sb.Append(i.[0]) |> ignore
    s1, sb.ToString()

let exportStructureToFile fileName (tree : Tree<_>) =
    let ss, s = tree.GetBestTree() |> treeToSecondaryStructureAndSeq
    System.IO.File.WriteAllLines(fileName,
                                 [">name";
                                  s;
                                  ss])
[<EntryPoint>]
let main argv = 
    printfn "Reading data"
    let data = (getData @"../../../../tests/data/16s/SILVA_128_SSURef_Nr99_tax_silva_first_500k_lines.fasta" true)
    //[[|a.[157]; a.[161]; a.[183]; a.[184]; a.[188]|] ; a.[190..196] ; a.[200..201] ; a.[241..242] ; [|a.[269]; a.[333]; a.[372]; a.[386]|]]
    //|> Array.concat
    //data.[800..900]
    let line = (snd data.[2]).Substring(406, 350)
    let input  = getLinearInputWithAllStartingPos line
    printfn "Parsing"
    let tree = buildAst parserSource input
    //let res = getAllRangesForStartStateWithLength parserSource input
    printfn "PostProcessing"
    let ast = tree.GetBestTree()
    printfn "To graph"
    //let graph = treeToGraph ast
    //printfn "To dot"
    //toDot "parsed16s.dot" graph
    //showTree ast
    let ss, s = treeToSecondaryStructureAndSeq ast
    System.IO.File.WriteAllLines("toDraw.txt",
                                 [">name";
                                  s;
                                  ss])
    //tree.AstToDot parserSource.IntToString "sppf.dot"
    
//    res
//    |> Seq.filter(fun (x,y,len) -> (int len) >= 260)
//    |> Seq.sortBy(fun (x,y,len) -> len )
//    |> Seq.iter(fun x -> printfn "%A" x)

    0 // return an integer exit code