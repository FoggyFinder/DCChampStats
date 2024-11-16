module Champs.Requests

open Champs.Blockchain
open Champs.Db

open Champs.Core
open System.Net.Http


let private storage = SqliteStorage("Data Source=dcchamps1.sqlite; Cache=Shared;Foreign Keys = True")

let private processBattle (battle:Battle) =
    if storage.ChampExists battle.Winner.AssetId |> not then
        storage.AddOrInsertChamp battle.Winner
    if storage.ChampExists battle.Loser.AssetId |> not then
        storage.AddOrInsertChamp battle.Loser
    if storage.BattleExists battle.BattleNum |> not then
        storage.AddOrUpdateBattle battle

let private getChampInfo (assetId: uint64) : ChampInfo option =
    storage.TryGetChamp assetId
    |> Option.map(fun champ ->
        storage.GetAllBattles()
        |> Seq.fold(fun acc b ->
            if b.Loser.AssetId = assetId then
                { 
                    acc with
                        Loses = acc.Loses + 1
                        Losed = acc.Losed + b.Wager
                }
            elif b.Winner.AssetId = assetId then
                {
                    acc with
                     Wins = acc.Wins + 1
                     Earned = acc.Earned + b.Wager
                }
            else acc) {
            AssetId = assetId
            Name = champ.Name
            Wins = 0
            Loses = 0
            Earned = 0M
            Losed = 0M
        })

let private calcChampStats (champId: uint64) : ChampStats option =
    storage.TryGetChamp champId
    |> Option.map(fun champ ->
        let champBattles =
            storage.GetAllBattles()
            |> Seq.filter(fun b -> b.Loser.AssetId = champId || b.Winner.AssetId = champId)
            |> Seq.sortByDescending (fun b -> b.BattleNum)
            |> Seq.toList

        let info =
            champBattles
            |> Seq.fold(fun acc b ->
                if b.Winner.AssetId = champId then
                    { 
                        acc with
                            Wins = acc.Wins + 1
                            Earned = acc.Earned + b.Wager
                    }
                elif b.Loser.AssetId = champId then
                    {
                        acc with
                         Loses = acc.Loses + 1
                         Losed = acc.Losed + b.Wager
                    }
                else acc) {
                    AssetId = champId
                    Name = champ.Name
                    Wins = 0
                    Loses = 0
                    Earned = 0M
                    Losed = 0M
                }
        let opponents =
            champBattles
            |> List.groupBy(fun b ->
                if b.Winner.AssetId = champId then b.Loser
                else b.Winner)
            |> List.map(fun (opponent, fights) ->
                opponent,
                fights |> List.fold(fun (w, l) b ->
                    if b.Loser.AssetId = champId then
                        (w, l + 1)
                    elif b.Winner.AssetId = champId then
                        (w + 1, l)
                    else (w, l)
                ) (0, 0)
            )
        let mostFightsAgainst, mostWinsAgainst, mostLosesAgainst  =
            if opponents.IsEmpty then
                None, None, None
            else 
                opponents |> Seq.maxBy (fun (o, (w, l)) -> w + l) |> fst |> Some,
                opponents |> Seq.maxBy (fun (o, (w, l)) -> w) |> fst |> Some,
                opponents |> Seq.maxBy (fun (o, (w, l)) -> l) |> fst |> Some
        {
            Info = info
            Battles = champBattles
            MostFightsWith = mostFightsAgainst
            MostLosesAgainst = mostLosesAgainst
            MostWinsAgainst = mostWinsAgainst
        })   

let private getLeaderBoard (battles:Battle list) =
    let leaderboard =
        battles
        |> List.collect(fun b -> [b.Winner; b.Loser])
        |> List.distinct
        |> List.map(fun ch -> (ch, (0, 0, 0M, 0M)))
        |> Map.ofList
    battles |> List.fold(fun leaderboard b ->
        leaderboard
        |> Map.change b.Winner (Option.map(fun(w, l, earned, losed) -> w+1, l, earned + b.Wager, losed))
        |> Map.change b.Loser (Option.map(fun(w, l, earned, losed) -> w, l+1, earned, losed + b.Wager))) leaderboard
    |> Seq.map(fun kv ->
        let ch = kv.Key
        let (w, l, earned, losed) = kv.Value
        {
            AssetId = ch.AssetId
            Name = ch.Name
            Wins = w
            Loses = l
            Earned = earned
            Losed = losed
        })
    |> Seq.toList
    |> List.sortByDescending(fun ci -> ci.Profit, - int64 ci.AssetId)

let getFullLeaderboard() = storage.GetAllBattles() |> getLeaderBoard

let getLeaderBoardForBattles(start:uint64 option, end': uint64 option) =
    storage.GetAllBattles()
    |> fun battles ->
        if start.IsSome then battles |> List.filter(fun b -> b.BattleNum >= start.Value)
        else battles
    |> fun battles ->
        if end'.IsSome then battles |> List.filter(fun b -> end'.Value >= b.BattleNum)
        else battles
    |> getLeaderBoard

let refresh() =
    let lastTracked = storage.GetLastTrackedBattle() |> Option.bind Utils.toUInt64 |> Option.defaultValue 0UL
    let currentBattle = Blockchain.getBattleNum()
    if currentBattle > lastTracked then
        Blockchain.getBoxBattles lastTracked currentBattle
        |> Seq.choose id
        |> Seq.iter processBattle

        let latestFilled = 
            let battles = storage.GetAllBattles()
            if battles.IsEmpty then 0UL
            else (battles |> List.maxBy(fun b -> b.BattleNum)).BattleNum

        let minB = min latestFilled currentBattle
        storage.SetLastTrackedBattle minB

let getAllChampsId() =
    getAccountCreatedAssets DarkCoinChampsCreator
    |> Seq.map(fun m -> m.Index)

let allChamps = getAllChampsId() |> Set.ofSeq

let getChampsForWallet(wallet:string) =
    getAssets wallet
    |> Seq.filter(fun m -> allChamps.Contains m.AssetId)
    |> Seq.choose(fun m ->
        if not <| storage.ChampExists m.AssetId then
            { Name = Blockchain.getAssetName m.AssetId; AssetId = m.AssetId }
            |> storage.AddOrInsertChamp
        getChampInfo m.AssetId)
    |> Seq.toList

let getChampStats(champId:uint64) =
    if allChamps.Contains champId then
        if not <| storage.ChampExists champId then
            { Name = Blockchain.getAssetName champId; AssetId = champId }
            |> storage.AddOrInsertChamp
        calcChampStats champId
    else None

let getChampDetails(champId:uint64) =
    if allChamps.Contains champId then
        Blockchain.tryGetChampInfo champId
        |> Option.bind(fun cp ->
            getChampStats champId
            |> Option.map(fun cs -> { Stats = cs; Properties = cp }))
    else None

let getBattle(battleId:uint64) =
    if not <| storage.BattleExists battleId then
        Blockchain.getBattle battleId
        |> Option.iter processBattle
    storage.TryGetBattle battleId

let getBattles() = storage.GetAllBattles()
let client = new HttpClient()
let getContributors() =
    async {
        try
            let uri = $"https://api.github.com/repos/FoggyFinder/DCChampStats/contributors?anon=1"
            use request = new System.Net.Http.HttpRequestMessage()
            request.Method <- System.Net.Http.HttpMethod.Get
            request.Headers.UserAgent.Add(Headers.ProductInfoHeaderValue(Headers.ProductHeaderValue("DCChampStats")))
            request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"))
            request.RequestUri <- System.Uri(uri)
            let! response = client.SendAsync(request) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Utils.getListOfContributors content
        with exp ->
            return []
    } |> Async.RunSynchronously
