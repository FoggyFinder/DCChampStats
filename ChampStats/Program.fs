// https://www.falcoframework.com/docs/get-started.html
// https://www.monsterasp.net/#signup

open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.HttpOverrides
open System
open System.Collections.Generic
open Champs.Core

let updateBattles =
    let cacheDict = Dictionary<uint64, (uint64 * DateTime)>()
    let updateCache() =
        let afterDT =
            if cacheDict.Count = 0 then None
            else Champs.Requests.latestTrackedBattleDT()
        let newValues = Champs.Blockchain.getBattlesDateTimes afterDT
        newValues
        |> Seq.iter(fun (key, v) ->
            if cacheDict.ContainsKey key then
                cacheDict.Remove key |> ignore
            cacheDict.Add(key, v))

    async {
        let xs = Champs.Requests.battlesWithoutTimestamp()
        let cacheNeedsUpdating = xs |> List.exists(cacheDict.ContainsKey >> not)
        if cacheNeedsUpdating then
            cacheDict.Clear()
            updateCache()
        let mutable hasChanged = false
        for battleNum in xs do
            try
                match Champs.Requests.getBattle battleNum with
                | Some battle ->
                    match cacheDict.TryGetValue battleNum with
                    | true, (_, dt) ->
                        { battle with UTCDateTime = Some(dt) }
                        |> Champs.Requests.addOrUpdateBattle
                        hasChanged <- true
                        do! Async.Sleep(System.TimeSpan.FromSeconds(5.0))
                    | false, _ -> ()
                | None -> ()
            with _ -> ()
        if hasChanged then
            Champs.Requests.getBattles()
            |> List.sortBy(fun b -> b.BattleNum)
            |> List.takeWhile(fun b -> b.UTCDateTime.IsSome)
            |> List.tryLast
            |> Option.iter(fun b -> Champs.Requests.setLatestTrackedBattleDT b.UTCDateTime.Value)
    }

let ct = new CancellationTokenSource()
let refresh =
    async {
        while true do
            try
                Champs.Requests.refresh()
            with _ -> ()
            do! Async.Sleep (System.TimeSpan.FromMinutes(2.0))
            try
                do! updateBattles
            with _ -> ()
            do! Async.Sleep (System.TimeSpan.FromMinutes(2.0))
            try
                Champs.Requests.refreshHordeLevels()
            with _ -> ()
            do! Async.Sleep (System.TimeSpan.FromMinutes(6.0))
            try
                Champs.Requests.refreshIPFS()
            with _ -> ()
            do! Async.Sleep (System.TimeSpan.FromMinutes(10.0))
    }
Async.Start(refresh, ct.Token)

[<RequireQualifiedAccess>]
module Route =
    let [<Literal>] index = "/"
    let [<Literal>] notFound = "/not-found"
    let [<Literal>] faq = "/faq"

    let [<Literal>] wallet = "/wallets/{wallet}"
    let [<Literal>] champ = "champs/{champ}"
    let [<Literal>] champs = "champs"

    let [<Literal>] battle = "battles/{battle}"
    let [<Literal>] battles = "battles"

    let [<Literal>] leaderboard = "leaderboard"
    let [<Literal>] leaderboardRange = "leaderboard/{range}"

    let [<Literal>] stats = "/stats"

    let [<Literal>] levels = "/levels"

let endpoints =
    [
        get Route.index (fun ctx ->
            Champs.Requests.getContributors()
            |> Champs.Pages.Home.homePage
            |> UI.layout "Home"
            |> fun html -> Response.ofHtml html ctx)
        
        get Route.faq (fun ctx ->
            Champs.Pages.FAQ.faqPage()
            |> UI.layout "FAQ"
            |> fun html -> Response.ofHtml html ctx)

        get Route.wallet (fun ctx ->
            let route = Request.getRoute ctx
            let wallet = route.GetString "wallet"
            Champs.Requests.getChampsForWallet wallet
            |> Champs.Pages.Wallet.walletPage wallet
            |> UI.layout $"{wallet} info"
            |> fun html -> Response.ofHtml html ctx)

        get Route.champ (fun ctx ->
            let route = Request.getRoute ctx
            let champId = uint64 <| route.GetInt64 "champ"
            Champs.Requests.getChampDetails champId
            |> Champs.Pages.Champs.champPage
            |> UI.layout "Champ info"
            |> fun html -> Response.ofHtml html ctx)
        
        get Route.champs (fun ctx ->
            Champs.Requests.getAllChampsInfo()
            |> Champs.Pages.Champs.champsPage
            |> UI.layout "Champs info"
            |> fun html -> Response.ofHtml html ctx)

        get Route.battle (fun ctx ->
            let route = Request.getRoute ctx
            let battleId = uint64 <| route.GetInt64 "battle"
            Champs.Requests.getBattle battleId
            |> Champs.Pages.Battle.battlePage
            |> UI.layout "Battle Info"
            |> fun html -> Response.ofHtml html ctx)

        get Route.battles (fun ctx ->
            let route = Request.getRoute ctx
            Champs.Requests.getBattles()
            |> Champs.Pages.Battle.battlesPage
            |> UI.layout "Battles"
            |> fun html -> Response.ofHtml html ctx)

        get Route.leaderboard (fun ctx ->
            Champs.Requests.getFullLeaderboard()
            |> Champs.Pages.Leaderboard.leaderBoardPage
            |> UI.layout "Leaderboard"
            |> fun html -> Response.ofHtml html ctx)

        get Route.leaderboardRange (fun ctx ->
            let route = Request.getRoute ctx
            let rawRange = route.GetString "range"
            let range = Champs.Core.Utils.parseRange rawRange
            Champs.Requests.getLeaderBoardForBattles range
            |> Champs.Pages.Leaderboard.leaderBoardPage
            |> UI.layout "Leaderboard"
            |> fun html -> Response.ofHtml html ctx)

        get Route.stats (fun ctx ->
            let route = Request.getRoute ctx
            Champs.Requests.getActivityReport()
            |> Champs.Pages.Stats.statsPage
            |> UI.chart "Activity"
            |> fun html -> Response.ofHtml html ctx)

        get Route.levels (fun ctx ->
            Champs.Requests.getAllChampsHorde()
            |> Champs.Pages.Champs.champsHordePage
            |> UI.layout "Champs levels"
            |> fun html -> Response.ofHtml html ctx)
    ]

let wapp = WebApplication.Create()

wapp.UseForwardedHeaders(ForwardedHeadersOptions(ForwardedHeaders = (ForwardedHeaders.XForwardedFor ||| ForwardedHeaders.XForwardedProto))) |> ignore
wapp.UseHsts() |> ignore

wapp.UseStaticFiles() |> ignore

wapp.UseRouting() |> ignore

wapp
    .UseFalco(endpoints)
    // ^-- activate Falco endpoint source
    .Run()

ct.Cancel()