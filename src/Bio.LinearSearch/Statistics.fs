module YC.Bio.Statistics

open System.Text
open System.IO
open System.Threading
open System.Collections.Generic

type OrganismReport(meta: string, time: System.TimeSpan, searchResults: (int * int)[]) =   
    let metaParts = meta.Split(',') |> Array.map (fun s -> s.Trim())
    
    let ncbiId = metaParts.[0].[1 ..] 
    let name = metaParts.[1]
    let level = metaParts.[2]
    let intervals16s = 
        metaParts.[3].Split()
        |> Array.map (fun s -> let p = s.Split(':') in (int p.[0], int p.[1]))         

    let uniteIntervals (intervals: (int * int)[]) =
        if Array.isEmpty intervals 
        then [||]
        else
            let curStart = ref (fst intervals.[0])
            let curEnd = ref (snd intervals.[0])
            let result = new ResizeArray<_>()
            for (s, e) in intervals do
                if s <= !curEnd
                then curEnd := e
                else
                    result.Add((!curStart, !curEnd))
                    curStart := s
                    curEnd := e
            result.ToArray()
    
    let lengthAndDeviation (intervals: (int * int)[]) =
        if Array.isEmpty intervals
        then 0.0, 0.0
        else
            let lengths = intervals |> Array.map (fun (x, y) -> float (y - x))
            let avgLength = lengths |> Array.average
            let sd = sqrt ((lengths |> Array.sumBy (fun l -> (l - avgLength) ** 2.0)) / float lengths.Length)
            avgLength, sd

    let getTruePosStatistics (intervals: (int * int)[]) =
        let covered, tpUnions, offsets = new ResizeArray<_>(), new ResizeArray<_>(), new ResizeArray<_>()
        for (i, j) in intervals do
            let cvOpt = intervals16s |> Array.tryFind (fun (s, e) -> min j e > max i s)  
            match cvOpt with
            | Some (s, e) -> 
                covered.Add((s, e))
                tpUnions.Add((i, j))
                offsets.Add(s - i)
            | None -> ()
        covered |> set |> Array.ofSeq |> Array.sortBy fst, tpUnions.ToArray(), offsets.ToArray()

    let intervalUnions = uniteIntervals searchResults
    let covered, truePosUnions, leftEndOffset = getTruePosStatistics intervalUnions        
    let falsePosUnions = Array.ofSeq (set intervalUnions - set truePosUnions)    
    let truePosLength, truePosSD = lengthAndDeviation truePosUnions
    let falsePosLength, falsePosSD = lengthAndDeviation falsePosUnions

    let printIntervals =
        let builder = new StringBuilder()
        [intervals16s; searchResults; intervalUnions]
        |> List.iter (fun a -> 
                          a |> Array.iter (fun (x, y) -> builder.Append(sprintf "%i:%i " x y) |> ignore)
                          builder.Append(", ") |> ignore)
        builder.Remove(builder.Length - 2 , 2).ToString()

    member val Id = ncbiId with get
    member val Name = name with get
    member val AssemblyLevel = level with get
    member val Expected = intervals16s with get
    member val ExpectedCount = intervals16s.Length with get
    member val AllResults = searchResults with get
    member val Covered = covered with get
    member val CoveredCount = covered.Length with get
    member val TPIntervals = truePosUnions with get
    member val TPIntervalsCount = truePosUnions.Length with get
    member val TPAverageLength = truePosLength with get
    member val TPLengthDeviation = truePosSD with get
    member val TPOffsets = leftEndOffset with get
    member val FPIntervals = falsePosUnions with get
    member val FPIntervalsCount = falsePosUnions.Length with get
    member val FPAverageLength = falsePosLength with get
    member val FPLengthDeviation = falsePosSD with get
    member val Time = time with get

    override this.ToString() = 
        let metaWithout16s = sprintf "%-15s, %-55s, %-17s" ncbiId name level
        sprintf
            "%s, %6i, %6i, %6i, %6.1f, %6.1f, %6i, %6.1f, %6.1f, %s, %s"
            metaWithout16s
            this.ExpectedCount
            this.CoveredCount
            this.TPIntervalsCount
            this.TPAverageLength
            this.TPLengthDeviation
            this.FPIntervalsCount
            this.FPAverageLength
            this.FPLengthDeviation
            (this.TPOffsets |> Array.map (fun i -> i.ToString()) |> String.concat " ")
            printIntervals

type TotalReport(reports: OrganismReport list) =    
    let expected, covered, tpIntervals, fpIntervals =
        reports
        |> List.fold 
               (fun (e, c, t, f) r -> 
                    (e + r.ExpectedCount, c + r.CoveredCount, t + r.TPIntervalsCount, f + r.FPIntervalsCount)) 
               (0, 0, 0, 0)
    let totalTime = reports |> List.sumBy (fun r -> r.Time.TotalMinutes)
    
    member this.PrintSummary() =
        printfn "Parsed: %i" reports.Length
        printfn "Total time(min): %f" totalTime
        printfn 
            "e: %i, cv: %i, tp_intervals: %i, fp_intervals: %i"
            expected
            covered
            tpIntervals
            fpIntervals

    member this.PrintToFile(file: string) =
        use reportFile = new System.IO.StreamWriter(file)
        let title = 
            sprintf 
                "%-15s, %-55s, %-17s, %6s, %6s, %6s, %6s, %6s, %6s, %6s, %6s, %6s, %s, %s, %s" 
                "id" "name" "level" "e_num" "cv_num" "tp_i" "tp_l" "tp_sd" "fp_i" "fp_l" "fp_sd" "tp_of" "exp" "fnd" "union"
        reportFile.WriteLine(title)
        for r in reports do
            reportFile.WriteLine(r.ToString())