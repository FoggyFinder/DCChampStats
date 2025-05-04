module UI

open Falco.Markup
open Plotly.NET

let [<Literal>] IPFS = "https://ipfs.dark-coin.io/ipfs/"

[<RequireQualifiedAccess>]
module Route =
    let [<Literal>] index = "/"
    let [<Literal>] notFound = "/not-found"
    let [<Literal>] faq = "/faq"

    let [<Literal>] wallet = "/wallets/{wallet}"
    let [<Literal>] champ = "/champs/{champ}"
    let [<Literal>] champs = "/champs"

    let [<Literal>] battle = "/battles/{battle}"
    let [<Literal>] battles = "/battles"

    let [<Literal>] leaderboard = "/leaderboard"
    let [<Literal>] leaderboardRange = "/leaderboard/{range}"

    let [<Literal>] stats = "/stats"

    let [<Literal>] levels = "/levels"

[<RequireQualifiedAccess>]
module Uri =
    let champ (champ:uint64) = Route.champ.Replace("{champ}", champ.ToString())
    let battle (battle:uint64) = Route.battle.Replace("{battle}", battle.ToString())
    //let wallet (wallet:string) = Route.wallet.Replace("{wallet}", wallet)
    //let leaderboard (range:string) = Route.leaderboardRange.Replace("{range}", range)

[<RequireQualifiedAccess>]
module UiUtils =
    open Champs.Core
    let linkToChamp (champ:Champ) =
        Elem.a [ Attr.href (Uri.champ champ.AssetId) ] [
            Text.raw $"{champ.Name}"
        ]

    let linkToBattle (b:Battle) =
        Elem.a [ Attr.href (Uri.battle b.BattleNum) ] [ 
            Text.raw $"{b.BattleNum}"
        ]

let private fullLayout (usePlotly:bool) (title:string) (content : XmlNode list) =
    Elem.html [ Attr.lang "en"; ] [
        Elem.head [] [
            if usePlotly then yield Elem.script [ Attr.src $"https://cdn.plot.ly/plotly-{Globals.PLOTLYJS_VERSION}.min.js"; Attr.charset "utf-8"] []
            yield Elem.meta  [ Attr.charset "UTF-8" ]
            yield Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
            yield Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]
            
            yield Elem.title [] [ Text.raw title ]
            
            yield Elem.link  [ Attr.href "/styles.css"; Attr.rel "stylesheet" ]

            yield Elem.link [ Attr.rel "shortcut icon"; Attr.href "/favicon.ico"; Attr.type' "image/x-icon" ]

            yield Elem.link [ Attr.rel "icon"; Attr.href "/favicon.ico"; Attr.type' "image/x-icon" ]
        ]

        Elem.body [ ] [
            Elem.header [ ] [
                Elem.nav [] [
                    Elem.a [ Attr.href Route.index ]
                        [ Text.raw "Home" ]

                    Elem.a [ Attr.href Route.faq ]
                        [ Text.raw "FAQ" ]

                    Elem.a [ Attr.href Route.leaderboard ]
                        [ Text.raw "Leaderboard" ]
                        
                    Elem.a [ Attr.href Route.stats ]
                        [ Text.raw "Stats" ]

                    Elem.a [ Attr.href Route.levels ]
                        [ Text.raw "Levels" ]
                ]
            ]
            Elem.main [] content
            Elem.footer [] [
                Elem.a [ Attr.href "https://github.com/FoggyFinder/DCChampStats"; Attr.targetBlank ]
                    [ Text.raw "Source code" ]
            ]
        ]
    ]

let layout = fullLayout false
let chart = fullLayout true

let getIpfsImg (className: string) (ipfso: string option) =
     ipfso |> Option.map(fun ipfs ->
        Elem.img [
            Attr.class' className
            Attr.src (IPFS + ipfs)
        ])
    |> Option.defaultWith (fun () -> Text.raw "")

let getChampInfoTable (champs:Champs.Core.ChampInfo list) =
    let header =
        Elem.tr [ ] [
            Elem.th [ ] [ Text.raw "" ]
            Elem.th [ ] [ Text.raw "Icon" ]
            Elem.th [ ] [ Text.raw "Name" ]
            Elem.th [ ] [ Text.raw "Wins" ]
            Elem.th [ ] [ Text.raw "Loses" ]
            Elem.th [ ] [ Text.raw "Fights" ]
            Elem.th [ ] [ Text.raw "Profit" ]
        ]
    let items = 
        champs |> List.mapi(fun i ch ->
            Elem.tr [] [
                Elem.td [ ] [ Text.raw $"{i + 1}" ]
                Elem.td [ ] [ ch.Champ.Ipfs |> getIpfsImg "champImgSmall" ]
                Elem.td [ ] [ UiUtils.linkToChamp ch.Champ ]
                Elem.td [ ] [ Text.raw $"{ch.Wins}" ]
                Elem.td [ ] [ Text.raw $"{ch.Loses}" ]
                Elem.td [ ] [ Text.raw $"{ch.Fights}" ]
                Elem.td [ ] [ Text.raw $"{ch.Profit}" ]
        ])
    Elem.table [ ] [
        yield header
        yield! items
    ]
