module YC.Bio.Statistics

open System.Text

type OrganismReport(meta: string, searchResults: (int * int)[]) =
    
    let metaParts = meta.Split(',') |> Array.map (fun s -> s.Trim())
    
    let ncbiId = metaParts.[0].[1 ..] 
    let name = metaParts.[1]
    let assemblyLevel = metaParts.[2]
    let intervals16s = 
        metaParts.[3].Split()
        |> Array.map (fun s -> let p = s.Split(':') in (int p.[0], int p.[1])) 
    
    let truePositive = 
        searchResults 
        |> Array.filter (fun (x, y) -> intervals16s |> Array.exists (fun (i, j) -> i <= x && y <= j))
    let falsePositive = Array.ofSeq (set searchResults - set truePositive)

    let printResults =
        let builder = new StringBuilder()
        [truePositive; falsePositive]
        |> List.iter (fun a -> 
                          a |> Array.iter (fun (x, y) -> builder.Append(sprintf "%i:%i " x y) |> ignore)
                          builder.Append(", ") |> ignore)
        builder.Remove(builder.Length - 2 , 2).ToString()

    member val Id = ncbiId with get
    member val Name = name with get
    member val AssemblyLevel = assemblyLevel with get
    member val Expected = intervals16s with get
    member val AllResults = searchResults with get
    member val CorrectResults = truePositive with get
    member val IncorrectResults = falsePositive with get

    override this.ToString() = meta + ", " + printResults

type TotalReport() =
    let x = 42