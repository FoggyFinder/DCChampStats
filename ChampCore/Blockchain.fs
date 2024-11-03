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
    seq { for i in [start..end' - 1UL] -> getBattle i }

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
