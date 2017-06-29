// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
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

[<EntryPoint>]
let main argv = 
    let data = (getData @"../../../../tests/data/16s/SILVA_128_SSURef_Nr99_tax_silva_first_500k_lines.fasta" true)
    //[[|a.[157]; a.[161]; a.[183]; a.[184]; a.[188]|] ; a.[190..196] ; a.[200..201] ; a.[241..242] ; [|a.[269]; a.[333]; a.[372]; a.[386]|]]
    //|> Array.concat
    //data.[800..900]
    let line = (snd data.[2]).Substring(406, 350)
    let input  = getLinearInputWithAllStartingPos line
    let tree = buildAst parserSource input
    //let res = getAllRangesForStartStateWithLength parserSource input

    tree.AstToDot parserSource.IntToString "sppf.dot"
    
//    res
//    |> Seq.filter(fun (x,y,len) -> (int len) >= 260)
//    |> Seq.sortBy(fun (x,y,len) -> len )
//    |> Seq.iter(fun x -> printfn "%A" x)

    0 // return an integer exit code
