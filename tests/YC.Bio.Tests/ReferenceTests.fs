module YC.Bio.Tests

open AbstractAnalysis.Common

open Yard.Generators.GLL
//open Yard.Generators.GLL.Parser 
open NUnit.Framework
open Yard.Generators
open Microsoft.FSharp.Collections

open YC.API
open Yard.Generators.GLL.ParserCommon
open System.Collections.Generic
open Yard.Generators.GLL.AbstractParser
open Yard.Frontends.YardFrontend
open Yard.Generators.GLL
open Yard.Core.Conversions.ExpandMeta


let grammar = @"..\..\..\..\src\YC.GrammarZOO\Bio\16s\R16S_19_27.yrd"

//let dataPath = @"..\..\..\data\16s\trainset15_092015.fa"
let dataPath = @"..\..\..\data\16s\HOMD_16S_rRNA_RefSeq_V14.5.fasta"
let completeGenomePath = @"..\..\..\data\16s\59127.fna"

let data = 
    if true
    then
        let d = new ResizeArray<_>()
        System.IO.File.ReadAllLines(dataPath)
        |> Array.iteri(fun i x -> if i % 2 <> 0 then d.Add(x))
        d.ToArray()
    else
        let d = new System.Text.StringBuilder()
        System.IO.File.ReadAllLines(dataPath).[1..]
        |> Array.iter(fun x -> d.Append x |> ignore)
        [|d.ToString()|]


//let inputFilesPath = 
//    if needChangeDirectory
//    then @"C:/Code/YaccConstructor/tests/data/GLL/"
//    else @"./data/GLL/"
//
//let grammarFilesPath = 
//    if needChangeDirectory
//    then @"C:/Code/YaccConstructor/tests/GLLParser.Simple.Tests/"
//    else @"./GLLParser.Simple.Tests/"

let getTokens (line : string) =
    line.ToCharArray()

//let getLinearInput line (stringToToken : string -> int<token>) = 
//    new LinearInput(
//            getTokens line
//            |> Array.map stringToToken
//            )

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
    new LinearInput(startPoss,tokens)

let isParsed = 
    Yard.Generators.GLL.AbstractParser.isParsed

[<TestFixture>]
type ``reference tests`` () =
    [<Test>]
    member test.``chech first 30 sequences`` ()  =
        data.[0..29]
        |> Array.iteri (fun i line -> 
            let input  = getLinearInputWithAllStartingPos line
            //let tree = buildAst parserSource input
            let res = getAllRangesForStartState parserSource input
    //        let gss = getGSS parserSource input
    //        gss.Vertices
    //        |> Seq.filter(fun v -> v.P.SetP.Count <> 0)
    //        |> Seq.collect(fun x -> x.P.SetP |> Seq.map(fun p -> x.Nonterm, x.PositionInInput, p.posInInput, match p.data with |Length l -> l | _ -> failwith "qwe"))
    //        |> Seq.sortBy(fun (x, y, z, len) -> len)
    //        |> Seq.iter(fun (x, y, z, len) -> printfn "Nonterm:%s, %i - %i, len: %i" (parserSource.IntToString.[int x]) y z len)
            let isParsed = 
                if res |> Seq.isEmpty then false
                else
                    res
                    |> Seq.exists(fun (x,y) -> y - x >= 300<positionInInput> && y - x <= 330<positionInInput>)
            Assert.AreEqual(isParsed, true, sprintf "Line %i wasn't parsed\n%s" (i*2+1) line))