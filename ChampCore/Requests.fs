module Champs.Requests

open Champs.Blockchain
open Champs.Db

open Champs.Core
open System.Net.Http


let private storage = SqliteStorage("Data Source=dcchamps1.sqlite; Cache=Shared;Foreign Keys = True")

let private processBattle (battle:Battle) =
    if storage.ChampExists battle.Winner.AssetId |> not then
        storage.AddOrUpdateChamp battle.Winner
    if storage.ChampExists battle.Loser.AssetId |> not then
        storage.AddOrUpdateChamp battle.Loser
    if storage.BattleExists battle.BattleNum |> not then
        storage.AddOrUpdateBattle battle

let addOrUpdateBattle(battle:Battle) =
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
                Champ = champ
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
                    Champ = champ
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
            Champ = ch
            Wins = w
            Loses = l
            Earned = earned
            Losed = losed
        })
    |> Seq.toList
    |> List.sortByDescending(fun ci -> ci.Profit, - int64 ci.Champ.AssetId)

let getFullLeaderboard() : LeaderBoard = 
    {
        Battles = LeaderboardRange.Full
        Leaderboard = storage.GetAllBattles() |> getLeaderBoard
    }

let getLeaderBoardForBattles(start:uint64 option, end': uint64 option) =
    let battles =
        storage.GetAllBattles()
        |> fun battles ->
            if start.IsSome then battles |> List.filter(fun b -> b.BattleNum >= start.Value)
            else battles
        |> fun battles ->
            if end'.IsSome then battles |> List.filter(fun b -> end'.Value >= b.BattleNum)
            else battles
    if battles.IsEmpty then { Battles = LeaderboardRange.EmptyOrInvalid; Leaderboard = [] }
    else
        match start, end' with
        | None, None -> { Battles = LeaderboardRange.EmptyOrInvalid; Leaderboard = [] }
        | Some x, Some y when x > y -> { Battles = LeaderboardRange.EmptyOrInvalid; Leaderboard = [] }
        | Some x, Some y ->
            { Battles = LeaderboardRange.Range(x, y); Leaderboard = getLeaderBoard battles }
        | Some x, None ->
            let y = battles |> List.maxBy(fun b -> b.BattleNum) |> fun b -> b.BattleNum
            { Battles = LeaderboardRange.Range(x, y); Leaderboard = getLeaderBoard battles }
        | None, Some y ->
            let x = battles |> List.minBy(fun b -> b.BattleNum) |> fun b -> b.BattleNum
            { Battles = LeaderboardRange.Range(x, y); Leaderboard = getLeaderBoard battles }

open System
let getActivity() =
    let today = DateTime.Now
    storage.GetAllBattles()
    |> List.fold(fun activity battle ->
        let activity' =
            match battle.UTCDateTime with
            | Some dt ->
                let localDT = dt.ToLocalTime()
                if localDT.Date = today.Date then
                    { activity with Today = activity.Today + 1 }
                elif localDT.Date = today.AddDays(-1).Date then
                    { activity with Yesterday = activity.Yesterday + 1 }
                elif localDT.Date > today.AddDays(-7).Date then
                    { activity with Week = activity.Week + 1 }
                elif localDT.Date > today.AddMonths(-1).Date then
                    { activity with Month = activity.Month + 1 }
                else activity
            | None -> { activity with Untracked = activity.Untracked + 1 }
        { activity' with Total = activity'.Total + 1 }) Activity.Empty

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

let refreshIPFS() =
    Blockchain.getLatestAcfgRoundForChamps()
    |> Option.iter(fun round ->
        let lastTracked = storage.GetLastTrackedTraitSwap() |> Option.bind Utils.toUInt64
        match lastTracked with
        | None ->
            storage.SetLastTrackedTraitSwap round
        | Some r when r = round ->
            ()
        | Some r ->
            getDCChampTransactions (System.Nullable(r))
            |> Seq.iter(fun (assetId, ipfs) ->
                storage.TryGetChamp assetId
                |> Option.iter(fun champ ->
                    { champ with Ipfs = Some ipfs }
                    |> storage.AddOrUpdateChamp
                ))
            storage.SetLastTrackedTraitSwap round
    )

let getAllChampsId() =
    getAccountCreatedAssets DarkCoinChampsCreator
    |> Seq.map(fun m -> m.Index)

let allChamps = getAllChampsId() |> Set.ofSeq

let getChampsForWallet(wallet:string) =
    getAssets wallet
    |> Seq.filter(fun m -> allChamps.Contains m.AssetId)
    |> Seq.choose(fun m ->
        if not <| storage.ChampExists m.AssetId then
            let ipfs = Blockchain.tryGetIpfs m.AssetId
            let name = Blockchain.getAssetName m.AssetId
            { Name = name; AssetId = m.AssetId; Ipfs = ipfs }
            |> storage.AddOrUpdateChamp
        getChampInfo m.AssetId)
    |> Seq.toList
    |> List.sortByDescending(fun c -> c.Profit)

let getAllChampsInfo() =
    storage.GetAllChamps()
    |> List.filter(fun c -> allChamps.Contains c.AssetId)
    |> List.choose(fun c -> getChampInfo c.AssetId)
    |> List.sortByDescending(fun c -> c.Profit)

let addOrUpdateChamp (champ:Champ) =
    champ |> storage.AddOrUpdateChamp

let getChampStats(champId:uint64) =
    if allChamps.Contains champId then
        if not <| storage.ChampExists champId then
            let ipfs = Blockchain.tryGetIpfs champId
            let name = Blockchain.getAssetName champId
            { Name = name; AssetId = champId; Ipfs = ipfs }
            |> addOrUpdateChamp
        calcChampStats champId
    else None

let getChampDetails(champId:uint64) =
    if allChamps.Contains champId then
        Blockchain.tryGetChampInfo champId
        |> Option.bind(fun cp ->
            // check that ipfs is up to date
            storage.TryGetChamp champId
            |> Option.iter(fun ch ->
                if ch.Ipfs.IsNone || ch.Ipfs.Value <> cp.Ipfs then
                    { ch with Ipfs = Some cp.Ipfs }
                    |> addOrUpdateChamp)
            getChampStats champId
            |> Option.map(fun cs -> { Stats = cs; Properties = cp }))
    else None

let getBattle(battleId:uint64) =
    if not <| storage.BattleExists battleId then
        Blockchain.getBattle battleId
        |> Option.iter processBattle
    storage.TryGetBattle battleId

let getBattles() = storage.GetAllBattles()
let battlesWithoutTimestamp() = storage.BattlesWithoutTimestamp()
let latestTrackedBattleDT() = storage.GetLastTrackedBattleDateTime()
let setLatestTrackedattleDT = storage.SetLastTrackedBattleDateTime

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
