// https://www.falcoframework.com/docs/get-started.html
// https://www.monsterasp.net/#signup

open Falco
open Falco.Routing
open Falco.HostBuilder
open System.Threading

async {
    Champs.Requests.refresh()
} |> Async.Start

let ct = new CancellationTokenSource()
let refresh =
    async {
        while true do
            do! Async.Sleep (System.TimeSpan.FromMinutes(15.0))
            try
                Champs.Requests.refresh()
            with _ -> ()
    }
Async.Start(refresh, ct.Token)

[<RequireQualifiedAccess>]
module Route =
    let [<Literal>] index = "/"
    let [<Literal>] notFound = "/not-found"

    let [<Literal>] wallet = "/wallets/{wallet}"
    let [<Literal>] champ = "champs/{champ}"
    let [<Literal>] champs = "champs"

    let [<Literal>] battle = "battles/{battle}"
    let [<Literal>] battles = "battles"

    let [<Literal>] leaderboard = "leaderboard"
    let [<Literal>] leaderboardRange = "leaderboard/{range}"

webHost [||] {
    use_caching
    use_compression
    use_static_files
    

    endpoints [
        get Route.index (fun ctx ->
            Champs.Requests.getContributors()
            |> Champs.Pages.Home.homePage
            |> UI.layout "Home"
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
            let title = "Full Leaderboard"
            Champs.Requests.getFullLeaderboard()
            |> Champs.Pages.Leaderboard.leaderBoardPage title
            |> UI.layout title
            |> fun html -> Response.ofHtml html ctx)

        get Route.leaderboardRange (fun ctx ->
            let route = Request.getRoute ctx
            let rawRange = route.GetString "range"
            let range = Champs.Core.Utils.parseRange rawRange
            let title =
                match range with
                | Some x, Some y -> $"({x}..{y})"
                | Some x, None -> $"({x}..)"
                | None, Some y -> $"(..{y})"
                | None, None -> ""
                |> fun s -> "Leaderboard " + s
            Champs.Requests.getLeaderBoardForBattles range
            |> Champs.Pages.Leaderboard.leaderBoardPage title
            |> UI.layout title
            |> fun html -> Response.ofHtml html ctx)
    ]
}

ct.Cancel()