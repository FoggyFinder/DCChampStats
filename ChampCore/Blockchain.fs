module Champs.Blockchain

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
let searchApi = SearchApi(httpClient)

let [<Literal>] ArenaContract = 1053328572UL
let [<Literal>] DragonsHordeApp = 1870514811UL
let [<Literal>] DarkCoinChampsCreator = "L6VIKAHGH4D7XNH3CYCWKWWOHYPS3WYQM6HMIPNBVSYZWPNQ6OTS5VERQY"

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

let getApplAccountTransactions(applId:uint64, afterTimeOpt:DateTime option) =
    let afterTimeStr = afterTimeOpt |> Option.map(fun dt -> dt.ToString("yyyy-MM-dd")) |> Option.defaultValue ""
    let rec getTransactions (next:string) acc = 
        async {
            let! r = searchApi.searchForTransactionsAsync(applicationId=Nullable(applId),next=next,txType="appl", afterTime=afterTimeStr) |> Async.AwaitTask
            let acc' = acc |> Seq.append r.Transactions
            if System.String.IsNullOrWhiteSpace(r.NextToken) then
                return acc'
            else
                return! getTransactions r.NextToken acc'
        }
    getTransactions null Seq.empty |> Async.RunSynchronously

let getBattlesDateTimes(afterTimeOpt:DateTime option) =
    getApplAccountTransactions(ArenaContract, afterTimeOpt)
    |> Seq.choose(fun tx ->
        try
            let args = tx.ApplicationTransaction.ApplicationArgs |> Seq.toArray
            if args.Length > 0 then
                let txT = args[0] |> System.Text.ASCIIEncoding.ASCII.GetString
                match txT with
                | "writeBattle" -> args[1] |> System.Text.ASCIIEncoding.ASCII.GetString |> Some
                | "fight" when args.Length = 5 -> args[2] |> System.Text.ASCIIEncoding.ASCII.GetString |> Some
                | _ -> None
                |> Option.bind(fun battleStr ->
                    battleStr.Replace("Battle", "")
                    |> Utils.toUInt64
                    |> Option.map(fun v -> v, convertRoundNumberToDateTime tx.ConfirmedRound.Value))
            else None
        with exp ->
            printfn "%A" exp
            None)
    |> Seq.groupBy fst
    |> Seq.map(fun (k, gr)->
        let group = gr |> Seq.map snd |> Seq.toArray
        let max = gr |> Seq.max
        k, max)

let client = new HttpClient()
let readJson (uri:string) =
    async { 
        use request = new System.Net.Http.HttpRequestMessage()
        request.Method <- System.Net.Http.HttpMethod.Get
        request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"))
        request.RequestUri <- System.Uri(uri)
        let! response = client.SendAsync(request) |> Async.AwaitTask
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return content
    } |> Async.RunSynchronously

let getBattle (battleNum: uint64) =
    let uri = $"https://mainnet-idx.algonode.cloud/v2/applications/{ArenaContract}/box?name=str:Battle{battleNum}"
    try 
        readJson uri
        |> Utils.battleFromString battleNum
    with _ -> None

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
open System.Buffers.Binary

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

let getHordeBoxUnsafe (boxName:string) =
    let bn = $"b64:{Uri.EscapeDataString(boxName)}"
    let json = 
        $"https://mainnet-idx.algonode.cloud/v2/applications/{DragonsHordeApp}/box?name={bn}"
        |> readJson
    let jObj = JObject.Parse(json)
    jObj.SelectToken("value").Value<string>()
    |> System.Convert.FromBase64String
    |> BinaryPrimitives.ReadUInt64BigEndian

let getAllXpsBoxes() =
    let xpKey = System.Text.ASCIIEncoding.ASCII.GetBytes("xp")
    try
        let json =
            $"https://mainnet-idx.algonode.cloud/v2/applications/{DragonsHordeApp}/boxes"
            |> readJson
        let jObj = JObject.Parse(json)
        jObj.SelectTokens($"$..name")
        |> Seq.map(fun t -> t.Value<string>())
        |> Seq.choose(fun name ->
            try
                let bytes = name |> System.Convert.FromBase64String
                let assetId, key = Array.splitAt (bytes.Length - 2) bytes
                if key = xpKey then
                    Some(assetId |> BinaryPrimitives.ReadUInt64BigEndian, getHordeBoxUnsafe name)
                else None
            with _ ->
                None)
        |> Seq.toList
        |> Some
    with _ ->
        None

let xpKey = System.Text.ASCIIEncoding.ASCII.GetBytes("xp")
let getAssetsLevelsAfter(afterTimeOpt:DateTime option) =
    getApplAccountTransactions(DragonsHordeApp, afterTimeOpt)
    |> Seq.choose(fun tx ->
        try
            let args = tx.ApplicationTransaction.ApplicationArgs |> Seq.toArray
            if args.Length > 0 then
                let txT = args[0] |> System.Text.ASCIIEncoding.ASCII.GetString
                match txT with
                | "grantXp" ->
                    let account = tx.ApplicationTransaction.ForeignAssets |> Seq.head
                    let buffer = Array.zeroCreate 8
                    let span = new Span<byte>(buffer)
                    BinaryPrimitives.WriteUInt64BigEndian(span, account)
                    let resultArr = Array.append buffer xpKey
                    let boxName = resultArr |> System.Convert.ToBase64String
                    (account, getHordeBoxUnsafe boxName)
                    |> Some
                | _ -> None
            else None
        with exp ->
            printfn "%A" exp
            None)
    |> Seq.groupBy fst
    |> Seq.map(fun (k, gr)->
        let group = gr |> Seq.map snd |> Seq.toArray
        let maxLvl = group |> Seq.max
        k, maxLvl)