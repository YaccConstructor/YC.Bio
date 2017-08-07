module YC.Bio.BioParser

open AbstractAnalysis.Common

open YC.API
open Yard.Frontends.YardFrontend
open Yard.Core.Conversions.ExpandMeta

open Yard.Generators
open Yard.Generators.GLL
open Yard.Generators.GLL.ParserCommon
open Yard.Generators.GLL.AbstractParser

type BioParser(grammar) =
    
    let getTokens (line: string) =
        line.ToCharArray()
    
    let parserSource = 
        let fe = new YardFrontend()
        let gen = new GLL()
        let conv = seq{yield new ExpandMeta()}
        generate grammar
                 fe gen 
                 None
                 conv
                 [|""|]
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
        let startPoss = 
            tokens
            |> Array.mapi(fun i x -> i * 1<positionInInput>)
        new LinearInput(startPoss,tokens)
    
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
            
    member this.Parse(input) = 
        getAllRangesForStartState parserSource (getLinearInputWithAllStartingPos input)
    
    member this.ParseParallel(threadNum, partLength, overlap, min16sLength, input) =
        parallelSearch16s threadNum partLength overlap min16sLength input