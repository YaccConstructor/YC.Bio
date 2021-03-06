module YC.Bio.Tests

open System.IO
open System.Collections.Generic
open Microsoft.FSharp.Collections

open AbstractAnalysis.Common
open YC.Bio.BioParser
open YC.Bio.Statistics

open NUnit.Framework

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

let extractRoot (meta : string) =
    meta.Split([|' '; ';'|]).[1]

let extractFamily (meta: string) =
    let parts = meta.Split(';')
    parts.[parts.Length - 2]

let extractID (meta : string) =
    meta.Split([|' '; '>'|], System.StringSplitOptions.RemoveEmptyEntries).[0]

let collectedResult = new Dictionary<string, Dictionary<string, int * int>>()
let collectResult isParsed root family =
    let addResult (x, y) = (x + (if isParsed then 1 else 0), y + if isParsed then 0 else 1)
    if collectedResult.ContainsKey(root)
    then
        let families = collectedResult.[root]
        if families.ContainsKey(family)
        then families.[family] <- addResult families.[family]
        else families.Add(family, addResult (0, 0))
    else
        let families = new Dictionary<_, _>()
        families.Add(family, (addResult (0, 0)))
        collectedResult.Add(root, families)

type Msg =
    | Str of string * string
    | Sum

[<TestFixture>]
module ``reference tests`` =
//    let testData = 
//        (getData @"..\..\..\tests\data\16s\HOMD_16S_rRNA_RefSeq_V14.5.fasta" false).[..99]
//        |> Seq.mapi(fun i x -> TestCaseData(i, Str x))
    
    let parser = new BioParser(grammar)
    let headFilter (x, y) = y - x >= 470<positionInInput> && x > 0<positionInInput> && y < 800<positionInInput>
    let middleFilter (x, y) = y - x >= 260<positionInInput> && x > 400<positionInInput> && y < 900<positionInInput>

    let semyonData =
        let data = getData @"../../../data/16s/SILVA_128_SSURef_Nr99_tax_silva_first_500k_lines.fasta" true
        //[[|a.[157]; a.[161]; a.[183]; a.[184]; a.[188]|] ; a.[190..196] ; a.[200..201] ; a.[241..242] ; [|a.[269]; a.[333]; a.[372]; a.[386]|]]
        //|> Array.concat
        //data.[800..900]
        [data.[533]; data.[614];data.[714]; data.[715]]
        //|> Seq.filter (fun (m, _) -> m.Contains("Bacteria;"))
        |> Seq.mapi(fun i x -> TestCaseData(i, Str x))   
        
    let writeSummary = [|TestCaseData(0, Sum)|]

    //[<TestCaseSource("testData")>]
    [<TestCaseSource("semyonData")>]
    [<TestCaseSource("writeSummary")>]
    let  ``Check sequence`` =  function 
        | i, Str (meta, line) -> 
            let res = parser.Parse(line)            
            //let tree = buildAst parserSource input
    //        let gss = getGSS parserSource input
    //        gss.Vertices
    //        |> Seq.filter(fun v -> v.P.SetP.Count <> 0)
    //        |> Seq.collect(fun x -> x.P.SetP |> Seq.map(fun p -> x.Nonterm, x.PositionInInput, p.posInInput, match p.data with |Length l -> l | _ -> failwith "qwe"))
    //        |> Seq.sortBy(fun (x, y, z, len) -> len)
    //        |> Seq.iter(fun (x, y, z, len) -> printfn "Nonterm:%s, %i - %i, len: %i" (parserSource.IntToString.[int x]) y z len)
            let isParsed, reason = 
                if res |> Seq.isEmpty then false, "Nothing found." else
                let is = res |> Seq.exists middleFilter
                if not is
                then
                    let filtered =
                        res
                        |> Seq.filter(fun (x, y) -> y - x >= 260<positionInInput>)
                    if filtered |> Seq.isEmpty
                    then
                        false, res |> Seq.maxBy(fun (x, y) -> y - x) |> (fun (x, y) -> y - x) |> sprintf "Max length is %i"
                    else
                        let leftmost = 
                            filtered
                            |> Seq.minBy(fun (x, y) -> x)
                        let rightmost = 
                            filtered
                            |> Seq.maxBy(fun (x, y) -> y)
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
            let root = extractRoot meta 
            let id = extractID meta
             
            collectResult isParsed root (extractFamily meta)

            Assert.AreEqual(isParsed || not (root = "bacteria"), true, sprintf "Line %i( %s ) wasn't parsed:\n%s" (i+1) id reason)
            if not isParsed then printfn "Line %i( %s ) wasn't parsed:\n    %s" (i + 1) id reason
            else printfn "Line %i parsed" (i + 1)
            if (root = "bacteria") && not isParsed
            then
                printfn "Line %i( %s ) wasn't parsed:\n    %s" (i+1) id reason
        | _, Sum -> 
            let output = new StreamWriter("../../16s_report.txt")
            output.WriteLine(sprintf "%-10s %-30s  %5s  %5s" "Root" "Family" "P" "Np")
            printfn "\nTests summary:"
            printfn "%-10s %-9s %-9s" "Name" "parsed" "notParsed"
            for kvp in collectedResult do
                let root, families = kvp.Key, kvp.Value
                let parsed, notParsed = ref 0, ref 0                
                for f in families do
                    let name, (p, np) = f.Key, f.Value
                    parsed := !parsed + p
                    notParsed := !notParsed + np
                    output.WriteLine(sprintf "%-10s; %-30s; %5i; %5i" root name p np)
                printfn "%-10s %-9i %-9i" root !parsed !notParsed 
            output.Close()

    [<Test>]
    let ``Statistics correctness``() =
        let data = (getData "../../../data/statistics/16s_gen.txt" true).[0]
        let result =
            parser.Parse (snd data)
            |> Seq.filter middleFilter
            |> Seq.map (fun (x, y) -> (int x, int y)) 
            |> Seq.toArray
        let report = new OrganismReport((fst data), new System.TimeSpan(), result)
        Assert.IsTrue
            (report.ExpectedCount = 1 
             && report.CoveredCount = 1
             && report.TPIntervalsCount >= 1
             && report.FPIntervalsCount = 0
             && report.TPOffsets.Length >= 1
             && report.TPOffsets |> Array.forall (fun x -> x < 0))