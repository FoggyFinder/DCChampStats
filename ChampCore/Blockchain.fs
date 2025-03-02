module Champs.Blockchain

// https://github.com/FrankSzendzielarz/dotnet-algorand-sdk
open Algorand
open System
open Algorand.Indexer
open System.Net.Http
open Newtonsoft.Json.Linq
open Champs.Core

// https://algonode.io/api/#free-as-in--algorand-api-access
let ALGOD_API_ADDR = "https://mainnet-idx.algonode.cloud"
let ALGOD_API_TOKEN = ""
let httpClient = HttpClientConfigurator.ConfigureHttpClient(ALGOD_API_ADDR, ALGOD_API_TOKEN)

let lookUpApi = LookupApi(httpClient)
let [<Literal>] ArenaContract = 1053328572UL
let [<Literal>] DarkCoinChampsCreator = "L6VIKAHGH4D7XNH3CYCWKWWOHYPS3WYQM6HMIPNBVSYZWPNQ6OTS5VERQY"
let [<Literal>] ArenaCreator = "762FFO2SIDJG2H7SXU5BQLQJ4Q5BQPGKKJGS2LEDQSJ7N5EMB2VVZMSMXM"

let getApp() = 
    async { 
        return! lookUpApi.lookupApplicationByIDAsync(ArenaContract) |> Async.AwaitTask
    } |> Async.RunSynchronously

let getBattleNum() = 
    getApp().Application.Params.GlobalState
    |> Seq.pick(fun g -> 
        let key = 
            System.Convert.FromBase64String(g.Key)
            |> System.Text.ASCIIEncoding.ASCII.GetString
        match key with
        | "battleNum" -> Some g.Value.Uint
        | _ -> None)

let convertRoundNumberToDateTime(roundNumber:uint64) =
    async {
        let! block = lookUpApi.lookupBlockAsync(roundNumber) |> Async.AwaitTask
        return DateTimeOffset.FromUnixTimeSeconds(int64 block.Timestamp).UtcDateTime
    } |> Async.RunSynchronously
// 1996-12-19T16:39:57-08:00
let getApplAccountTransactions(address:string, afterTimeOpt:DateTime option) =
    let afterTimeStr = afterTimeOpt |> Option.map(fun dt -> dt.ToString("yyyy-MM-dd")) |> Option.defaultValue ""
    printfn "Get txs after %s" afterTimeStr
    let rec getTransactions (next:string) acc = 
        async {
            let! r = lookUpApi.lookupAccountTransactionsAsync(address,next=next,txType="appl", afterTime=afterTimeStr) |> Async.AwaitTask
            let acc' = acc |> Seq.append r.Transactions
            if System.String.IsNullOrWhiteSpace(r.NextToken) then
                return acc'
            else
                return! getTransactions r.NextToken acc'
        }
    getTransactions null Seq.empty |> Async.RunSynchronously

let getBattlesDateTimes(afterTimeOpt:DateTime option) =
    getApplAccountTransactions(ArenaCreator, afterTimeOpt)
    |> Seq.filter(fun tx -> tx.ApplicationTransaction <> null && tx.ApplicationTransaction.ApplicationId = ArenaContract)
    |> Seq.choose(fun tx ->
        try
            let args = tx.ApplicationTransaction.ApplicationArgs |> Seq.toArray
            let txT = args[0] |> System.Text.ASCIIEncoding.ASCII.GetString
            match txT with
            | "writeBattle" ->
                let battleStr = args[1] |> System.Text.ASCIIEncoding.ASCII.GetString
                let battleNum = battleStr.Replace("Battle", "") |> Utils.toUInt64
                let dt = convertRoundNumberToDateTime tx.ConfirmedRound.Value
                battleNum |> Option.map(fun v -> v, dt)
            | _ -> None
        with exp ->
            printfn "%A" exp
            None)
    |> Seq.groupBy fst
    |> Seq.map(fun (k, gr)->
        let group = gr |> Seq.map snd |> Seq.toArray
        let max = gr |> Seq.max
        k, max)

let client = new HttpClient()
let getBattle (battleNum: uint64) =
    async {
        try
            let uri = $"https://mainnet-idx.algonode.cloud/v2/applications/{ArenaContract}/box?name=str:Battle{battleNum}"
            use request = new System.Net.Http.HttpRequestMessage()
            request.Method <- System.Net.Http.HttpMethod.Get
            request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"))
            request.RequestUri <- System.Uri(uri)
            let! response = client.SendAsync(request) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Utils.battleFromString battleNum content
        with exp ->
            return None
    } |> Async.RunSynchronously

let getBoxBattles (start: uint64) (end': uint64) = 
    seq { for i in [start..end'] -> getBattle i }

let getAllBattles() =
    let count = getBattleNum()
    getBoxBattles 1UL count |> Seq.choose id

let getAssetMetadata(assetId:uint64) =
    async { 
        return! lookUpApi.lookupAssetByIDAsync(assetId) |> Async.AwaitTask
    } |> Async.RunSynchronously

let getAssetName(assetId:uint64) =
    getAssetMetadata(assetId).Asset.Params.Name

let getAssets(wallet:string) =
    let rec getAssets (next:string) acc = 
        async { 
            let! r = lookUpApi.lookupAccountAssetsAsync(wallet, next=next) |> Async.AwaitTask
            let acc' = acc |> Seq.append r.Assets
            if System.String.IsNullOrWhiteSpace(r.NextToken) then
                return acc'
            else
                return! getAssets r.NextToken acc'
        }
    getAssets null Seq.empty |> Async.RunSynchronously

let getAccountCreatedAssets(wallet:string) =
    let rec getAssets (next:string) acc = 
        async { 
            let! r = lookUpApi.lookupAccountCreatedAssetsAsync(wallet, next=next) |> Async.AwaitTask
            let acc' = acc |> Seq.append r.Assets
            if System.String.IsNullOrWhiteSpace(r.NextToken) then
                return acc'
            else
                return! getAssets r.NextToken acc'
        }
    getAssets null Seq.empty |> Async.RunSynchronously

open Newtonsoft.Json.Linq
open Ipfs

let private ipfsFromACFG(acfg:Model.TransactionAssetConfig) =
    let addr = Algorand.Address(acfg.Params.Reserve)
    let cid = Cid(ContentType = "dag-pb", Version = 0, Hash = MultiHash("sha2-256", addr.Bytes))
    cid.ToString()

let tryGetIpfs (assetId: uint64) =
    async {
        let! d = lookUpApi.lookupAssetByIDAsync(assetId) |> Async.AwaitTask
        let! tr = lookUpApi.lookupAssetTransactionsAsync(assetId, txType = "acfg") |> Async.AwaitTask
        return
            tr.Transactions
            |> Seq.tryLast
            |> Option.map(fun tx -> ipfsFromACFG tx.AssetConfigTransaction)
    } |> Async.RunSynchronously

let tryGetChampInfo(assetId:uint64) =
    async {
        let! d = lookUpApi.lookupAssetByIDAsync(assetId) |> Async.AwaitTask
        let! tr = lookUpApi.lookupAssetTransactionsAsync(assetId, txType = "acfg") |> Async.AwaitTask
        return
            tr.Transactions
            |> Seq.tryLast
            |> Option.map(fun tx ->
                let json = tx.Note |> System.Text.ASCIIEncoding.ASCII.GetString |> JObject.Parse
                let properties = json.["properties"]
                {
                    Armour = properties.Value<string>("Armour")
                    Background = properties.Value<string>("Background")
                    Extra = properties.Value<string>("Extra")
                    Head = properties.Value<string>("Head")
                    Magic = properties.Value<string>("Magic")
                    Skin = properties.Value<string>("Skin")
                    Weapon = properties.Value<string>("Weapon")
                    Ipfs = ipfsFromACFG tx.AssetConfigTransaction
                }
            )
    } |> Async.RunSynchronously

let getDCChampTransactions(minRound:Nullable<uint64>) = 
    let rec getTransactions (next:string) acc = 
        async {
            let! r = lookUpApi.lookupAccountTransactionsAsync(DarkCoinChampsCreator, next = next, txType = "acfg", minRound=minRound) |> Async.AwaitTask
            let acc' = acc |> Seq.append r.Transactions
            if System.String.IsNullOrWhiteSpace(r.NextToken) then
                return acc'
            else
                return! getTransactions r.NextToken acc'
        }
    getTransactions null Seq.empty |> Async.RunSynchronously
    |> Seq.choose(fun tx ->
        tx.AssetConfigTransaction.AssetId |> Option.ofNullable |> Option.map(fun assetId ->
            assetId, ipfsFromACFG tx.AssetConfigTransaction))

let getLatestAcfgRoundForChamps() = 
    let rec getTransactions (next:string) acc = 
        async {
            let! r = lookUpApi.lookupAccountTransactionsAsync(DarkCoinChampsCreator, txType = "acfg") |> Async.AwaitTask
            let trx =
                r.Transactions
                |> Seq.choose(fun tr -> Option.ofNullable tr.ConfirmedRound)
                |> Seq.toArray
            return
                if trx.Length = 0 then None
                else trx |> Array.max |> Some
        }
    getTransactions null Seq.empty |> Async.RunSynchronously
