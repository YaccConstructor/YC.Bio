module YC.Bio.Statistics

open System.Text

type OrganismReport(meta: string, time: System.TimeSpan, searchResults: (int * int)[]) =   
    let metaParts = meta.Split(',') |> Array.map (fun s -> s.Trim())
    
    let ncbiId = metaParts.[0].[1 ..] 
    let name = metaParts.[1]
    let level = metaParts.[2]
    let intervals16s = 
        metaParts.[3].Split()
        |> Array.map (fun s -> let p = s.Split(':') in (int p.[0], int p.[1])) 
        |> Array.toList
    
    let truePositive, covered =
        let tp, cv = new ResizeArray<_>(), new ResizeArray<_>()
        for (i, j) in searchResults do
            match List.tryFind (fun (x, y) -> (x <= i && j <= y)) intervals16s with
            | Some p -> tp.Add (i, j); cv.Add p
            | None -> ()
        List.ofSeq tp, cv |> set |> List.ofSeq |> List.sortBy fst 
    let falsePositive = List.ofSeq (set searchResults - set truePositive)
    
    let printIntervals =
        let builder = new StringBuilder()
        [intervals16s; covered; truePositive; falsePositive]
        |> List.iter (fun a -> 
                          a |> List.iter (fun (x, y) -> builder.Append(sprintf "%i:%i " x y) |> ignore)
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
    member val CorrectResults = truePositive with get
    member val CorrectCount = truePositive.Length with get
    member val IncorrectResults = falsePositive with get
    member val IncorrectCount = falsePositive.Length with get
    member val Time = time with get

    override this.ToString() = 
        let metaWithout16s = sprintf "%-15s, %-55s, %-15s" ncbiId name level
        sprintf
            "%s, %6i, %6i, %6i, %6i, %s"
            metaWithout16s
            this.ExpectedCount
            this.CoveredCount
            this.CorrectCount
            this.IncorrectCount
            printIntervals

type TotalReport(reports: OrganismReport list) =    
    let expected, covered, truePos, falsePos =
        reports
        |> List.fold 
               (fun (e, c, t, f) r -> 
                    (e + r.ExpectedCount, c + r.CoveredCount, t + r.CorrectCount, f + r.IncorrectCount)) 
               (0, 0, 0, 0)
    let totalTime = reports |> List.sumBy (fun r -> r.Time.TotalMinutes)
    
    member this.PrintSummary() =
        printfn "Parsed: %i" reports.Length
        printfn "Total time(min): %f" totalTime
        printfn 
            "e: %i, cv: %i, tp: %i, fp: %i"
            expected
            covered
            truePos
            falsePos

    member this.PrintToFile(file: string) =
        use reportFile = new System.IO.StreamWriter(file)
        let title = 
            sprintf 
                "%-15s %-55s %-17s %6s %6s %6s %6s %5s %s %s %s" 
                "id" " name" " level" " e_num" " cv_num" " tp_num" " fp_num" "exp" "cv" "tp" "fp"
        reportFile.WriteLine(title)
        for r in reports do
            reportFile.WriteLine(r.ToString())