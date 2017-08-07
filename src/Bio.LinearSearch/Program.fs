open System.IO

open YC.Bio.BioParser
open YC.Bio.Statistics
type Domain = Head | Middle

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
                meta:= s
            else
                d.Append s |> ignore
        lst.Add(!meta, d.ToString())
        lst.ToArray()

let identify16sInGenomes domain =
    let parser = new BioParser(grammar)
    let genomeFiles = 
        Directory.GetFiles("../../../../tests/data/complete_genome/", "*.txt", SearchOption.AllDirectories)
    
    let reports = new ResizeArray<_>()

    for f in genomeFiles do
        let meta, genome = (getData f true).[0]
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()
        let result = 
            match domain with
            | Head -> parser.ParseParallel(4, 2000, 600, 470, genome)
            | Middle -> parser.ParseParallel(4, 5000, 600, 250, genome.[0 .. 1000])
        stopWatch.Stop()
        
        let report = new OrganismReport(meta, stopWatch.Elapsed, result)
        reports.Add report
        printfn "%s %s" report.Id report.Name
        printfn "length: %i" genome.Length
        printfn "time: %f" stopWatch.Elapsed.TotalMinutes
        printfn 
            "e: %i; cv: %i; tp_intervals: %i; fp_intervals: %i"
            report.ExpectedCount
            report.CoveredCount
            report.TPIntervalsCount
            report.FPIntervalsCount
        printfn ""
                    
    let totalReport = new TotalReport(List.ofSeq reports)
    totalReport.PrintSummary()
    totalReport.PrintToFile("../../full_report.txt")

[<EntryPoint>]
let main argv =
    identify16sInGenomes Middle
    0 