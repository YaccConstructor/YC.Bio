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
open YC.Bio.Statistics

//let needChangeDirectory = 
//    (@"C:\Users\Artem Gorokhov\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14" = System.IO.Directory.GetCurrentDirectory())
//    || (@"C:\Users\artem\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14" = System.IO.Directory.GetCurrentDirectory())

//let grammar = if needChangeDirectory then @"C:\Code\YC.Bio" else @"..\..\.." + @"\src\YC.GrammarZOO\Bio\16s\R16S_19_27.yrd"
let grammar = @"../../../../src/YC.GrammarZOO/Bio/16s/R16S_19_27.yrd"
//let dataPath = @"..\..\..\data\16s\trainset15_092015.fa"
//let dataPath = @"..\..\tests\data\16s\HOMD_16S_rRNA_RefSeq_V14.5.fasta"
//let dataPath = @"..\..\..\data\16s\59127.fna"
//let dataPath = if needChangeDirectory then @"C:\Code\YC.Bio\tests" else @"..\..\.." + @"\data\16s\SILVA_128_SSURef_Nr99_tax_silva.fasta"

let isFasta = false

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

let extractName (meta : string) =
    meta.Split([|' '; ';'|]).[1]

let extractID (meta : string) =
    meta.Split([|' '; '>'|], System.StringSplitOptions.RemoveEmptyEntries).[0]

let parallelSearch16s blockSize partLength overlap min16sLength (genome: string) =
    let blockLength = partLength * blockSize
    let blocksNum = genome.Length / blockLength
    let realStartPos i j = i * blockLength + j * partLength
    let result = new ResizeArray<_>()

    let getPart blockNum partNum =
        let pos = realStartPos blockNum partNum
        let part = 
            if genome.Length - pos > partLength + overlap
            then genome.[pos .. pos + partLength + overlap]
            elif genome.Length - 1 > pos
            then genome.[pos ..]
            else ""  
        getLinearInputWithAllStartingPos part
  
    let selectRepresentatives (pairs: seq<_>) = 
        if Seq.length pairs = 0
        then []
        else
            let h = Seq.head pairs
            let _, _, res = 
                pairs                 
                |> Seq.filter (fun (x, y) -> y - x > min16sLength * 1<positionInInput>)
                |> Seq.fold    //select the longest result from each group
                       (fun (pos, max, acc) (x, y) -> 
                            if x - pos > 50<positionInInput>
                            then (x, (x, y), max :: acc)                        
                            elif y - x > snd max - fst max
                            then (pos, (x, y), acc)
                            else (pos, max, acc)) 
                       (fst h, h, [])
            List.rev res
                                               
    for i in 0 .. blocksNum do
        [| for j in 0 .. blockSize - 1 -> getPart i j |]
        |> Array.Parallel.map (getAllRangesForStartState parserSource)
        |> Array.iteri (fun j s ->
                            let pos = realStartPos i j
                            in selectRepresentatives s   
                               |> List.iter (fun (x, y) -> result.Add (pos + int x, pos + int y)))    
    Array.ofSeq result

let collectedResult = new Dictionary<_,int * int>()
let collectResult isParsed (name : string) =
    
    //printfn "name: %s" name
//    if collectedResult = null then printfn "collectedResult is null"
//    collectedResult.ContainsKey(name) |> printfn "Contains : %A"
//    let contains, (parsed, notParsed) = collectedResult.(name)
    if collectedResult.ContainsKey(name)
    then
        let (parsed, notParsed) = collectedResult.[name]
        collectedResult.[name] <- (parsed + (if isParsed then 1 else 0), notParsed + if isParsed then 0 else 1)
    else
        collectedResult.Add(name, ((if isParsed then 1 else 0), if isParsed then 0 else 1))

type Msg =
    | Str of string * string
    | Sum

[<TestFixture>]
module ``reference tests`` =
//    let testData = 
//        (getData @"..\..\..\tests\data\16s\HOMD_16S_rRNA_RefSeq_V14.5.fasta" false).[..99]
//        |> Seq.mapi(fun i x -> TestCaseData(i, Str x))

    let semenData =
        let data = (getData @"../../../data/16s/SILVA_128_SSURef_Nr99_tax_silva_first_500k_lines.fasta" true)
        //[[|a.[157]; a.[161]; a.[183]; a.[184]; a.[188]|] ; a.[190..196] ; a.[200..201] ; a.[241..242] ; [|a.[269]; a.[333]; a.[372]; a.[386]|]]
        //|> Array.concat
        //data.[800..900]
        [data.[533]; data.[614];data.[714]; data.[715]]
        |> Seq.mapi(fun i x -> TestCaseData(i, Str x))
    
    let completeGenData =
        getData @"../../../data/complete_genome/ATCC_35405.txt" true
        |> Seq.map (fun x -> TestCaseData(Str x))
        //|> Seq.map(fun (x, y) -> TestCaseData(Str (x, y.[610000 .. 620000])))
        
    let writeSummary = [|TestCaseData(0, Sum)|]

    [<TestCaseSource("completeGenData")>]
    [<Ignore("too loooong")>]
    let ``Identify 16s in complete genome`` = function
        | Str (meta, genome) ->
            let stopWatch = System.Diagnostics.Stopwatch.StartNew()
            printfn "%i" genome.Length
            let result = parallelSearch16s 4 5000 600 250 genome
            stopWatch.Stop()

            let report = new OrganismReport(meta, result)
            System.IO.File.WriteAllText("report.txt", report.ToString())
            printfn "correct: %i" report.CorrectResults.Length
            printfn "incorrect: %i" report.IncorrectResults.Length
            //result |> Array.iter (fun (x, y) -> printfn "%i : %i" x y)
            printfn "%f" stopWatch.Elapsed.TotalSeconds 
            
        | _ -> failwith ""
                   
    //[<TestCaseSource("testData")>]
    [<TestCaseSource("semenData")>]
    [<TestCaseSource("writeSummary")>]
    let  ``Check sequence`` =  function 
        | i, Str (meta, line) -> 
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
                            |> Seq.maxBy(fun (x,y) -> y)
                        if leftmost = rightmost
                        then 
                            false
                            , sprintf "%i - %i, %i" (fst leftmost) (snd leftmost) (snd leftmost - fst leftmost) 
                        else
                            false
                            , sprintf "Leftmost: %i - %i, %i\n" (fst leftmost) (snd leftmost) (snd leftmost - fst leftmost)
                            + sprintf "    Rightmost: %i - %i, %i" (fst rightmost) (snd rightmost) (snd rightmost - fst rightmost)
                else
                    true, ""

    //            res
    //            |> Seq.iter (fun (x,y) -> printfn "%i - %i : %i" x y (y - x))
            let name = extractName meta 
            let id = extractID meta
             
            collectResult isParsed name

            Assert.AreEqual(isParsed || not (name = "bacteria"), true, sprintf "Line %i( %s ) wasn't parsed:\n%s" (i+1) id reason)
            //if not isParsed then printfn "Line %i( %s ) wasn't parsed:\n    %s" (i+1) id reason
            //else printfn "Line %i parsed" (i + 1)
            if (name = "bacteria") && not isParsed
            then
                printfn "Line %i( %s ) wasn't parsed:\n    %s" (i+1) id reason
        | _, Sum -> 
            printfn "\nTests summary:"
            printfn "Name      parsed notParsed"
            for kvp in collectedResult do
                let name,(parsed, notParsed) = kvp.Key, kvp.Value
                printf "%s" name
                for i in 1..(10 - name.Length) do
                    printf " "
                parsed.ToString() |> printf "%s" 
                for i in 1..(7 - parsed.ToString().Length) do
                    printf " "
                notParsed.ToString() |> printf "%s\n"

//[<EntryPointAttribute>]
//let main arg = 
//    let line = (getData @"..\..\..\tests\data\16s\VKM_Ac-1815D.fa" true).[0]
//    let before = System.DateTime.Now
//
//    let input  = getLinearInputWithAllStartingPos line
//    let res = getAllRangesForStartState parserSource input
//    System.IO.File.WriteAllLines("resultOfGenomeParsing.txt", res |> Seq.map(fun x -> x.ToString()))
//    printfn "Time: %A" (System.DateTime.Now - before)
//    0