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

let needChangeDirectory = 
    (@"C:\Users\Artem Gorokhov\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14" = System.IO.Directory.GetCurrentDirectory())
    || (@"C:\Users\artem\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14" = System.IO.Directory.GetCurrentDirectory())

let grammar = if needChangeDirectory then @"C:\Code\YC.Bio" else @"..\..\..\.." + @"\src\YC.GrammarZOO\Bio\16s\R16S_19_27.yrd"

//let dataPath = @"..\..\..\data\16s\trainset15_092015.fa"
let dataPath = @"..\..\..\data\16s\HOMD_16S_rRNA_RefSeq_V14.5.fasta"
//let dataPath = @"..\..\..\data\16s\59127.fna"
//let dataPath = if needChangeDirectory then @"C:\Code\YC.Bio\tests" else @"..\..\.." + @"\data\16s\SILVA_128_SSURef_Nr99_tax_silva.fasta"

let isFasta = false

let data = 
    if not isFasta
    then
        let d = new ResizeArray<_>()
        System.IO.File.ReadAllLines(dataPath)
        |> Array.iteri(fun i x -> if i % 2 <> 0 then d.Add(x))
        d.ToArray()
    else
        let d = new System.Text.StringBuilder()
        let flag = ref false
        let lst = new ResizeArray<_>()
        for s in System.IO.File.ReadAllLines(dataPath).[..500] do
            if s.[0] = '>'
            then
                if !flag
                then 
                    lst.Add(d.ToString())
                    d.Clear() |> ignore
                else
                    flag := true
            else
                d.Append s |> ignore
        lst.ToArray().[..20]


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

type str =
    | Str of string

[<TestFixture>]
module ``reference tests`` =
    let testData = 
        data.[..29]
        |> Seq.mapi(fun i s -> TestCaseData(i, Str s))

    [<TestCaseSource("testData")>]
    let ``Check first 30 sequences`` = function 
        | i, Str line -> 
            let input  = getLinearInputWithAllStartingPos line
            //let tree = buildAst parserSource input
            let res = getAllRangesForStartState parserSource input
    //        let gss = getGSS parserSource input
    //        gss.Vertices
    //        |> Seq.filter(fun v -> v.P.SetP.Count <> 0)
    //        |> Seq.collect(fun x -> x.P.SetP |> Seq.map(fun p -> x.Nonterm, x.PositionInInput, p.posInInput, match p.data with |Length l -> l | _ -> failwith "qwe"))
    //        |> Seq.sortBy(fun (x, y, z, len) -> len)
    //        |> Seq.iter(fun (x, y, z, len) -> printfn "Nonterm:%s, %i - %i, len: %i" (parserSource.IntToString.[int x]) y z len)
            let isParsed, reason = 
                if res |> Seq.isEmpty then false, "Nothing found." else
                let is =
                    res
                    |> Seq.exists(fun (x,y) -> y - x >= 260<positionInInput> &&
                                        (*y - x >= 260<positionInInput> && y - x <= 290<positionInInput> && *)x > 400<positionInInput> && y < 900<positionInInput>)
                if not is
                then
                    let filtered =
                        res
                        |> Seq.filter(fun (x,y) -> y - x >= 260<positionInInput>)

                    if filtered |> Seq.isEmpty
                    then
                        false, res |> Seq.maxBy(fun (x,y) -> y - x) |> (fun (x,y) -> y - x) |> sprintf "Max length is %i"
                    else
                        let leftmost = 
                            filtered
                            |> Seq.minBy(fun (x,y) -> x)
                        let rightmost = 
                            filtered
                            |> Seq.minBy(fun (x,y) -> y)
                        if leftmost = rightmost
                        then 
                            false
                            , sprintf "%i - %i, %i" (fst leftmost) (snd leftmost) (snd leftmost - fst leftmost) 
                        else
                            false
                            , sprintf "Leftmost: %i - %i, %i\n" (fst leftmost) (snd leftmost) (snd leftmost - fst leftmost)
                            + sprintf "Rightmost: %i - %i, %i" (fst rightmost) (snd rightmost) (snd rightmost - fst rightmost)
                else
                    true, ""

    //            res
    //            |> Seq.iter (fun (x,y) -> printfn "%i - %i : %i" x y (y - x))
              
            Assert.AreEqual(isParsed, true, sprintf "Line %i wasn't parsed:\n    %s" (i+1) reason)
    //        if not isParsed then printfn "Line %i wasn't parsed:\n    %s" (i+1) reason
    //        else printfn "Line %i parsed" (i + 1)