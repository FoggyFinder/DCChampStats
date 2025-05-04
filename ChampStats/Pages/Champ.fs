module Champs.Pages.Champs

open Falco.Markup
open Champs.Core
open UI

let champPage (champDetailedO: ChampDetailed option) =
    [
        match champDetailedO with
        | None -> yield Text.raw "Champ either not found or request can't be evaluated at the moment. Verify id and try later"
        | Some chd  ->
            let chst = chd.Stats
            yield Text.h1 $"{chst.Info.Champ.Name} stats"
            yield Elem.img [
                Attr.class' "champImgBig"
                Attr.src (UI.IPFS + chd.Properties.Ipfs)
            ]
            let propertiesTableHeader =
                Elem.tr [] [
                    Elem.th [] [ Text.raw "Property" ]
                    Elem.th [] [ Text.raw "Value" ]
                ]
                    
            let propertiesTableItems = [
                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Battles" ]
                    Elem.td [] [ Text.raw $"{chst.Info.Fights}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Victories" ]
                    Elem.td [] [ Text.raw $"{chst.Info.Wins}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Defeats" ]
                    Elem.td [] [ Text.raw $"{chst.Info.Loses}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Profit" ]
                    Elem.td [] [ Text.raw $"{chst.Info.Profit}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Most fights against" ]
                    Elem.td [] [
                        match chst.MostFightsWith with
                        | Some champ -> UiUtils.linkToChamp champ
                        | None -> Text.raw ""
                    ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Most wins against" ]
                    Elem.td [] [
                        match chst.MostWinsAgainst with
                        | Some champ -> UiUtils.linkToChamp champ
                        | None -> Text.raw ""
                    ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Most losses against" ]
                    Elem.td [] [ 
                        match chst.MostLosesAgainst with
                        | Some champ -> UiUtils.linkToChamp champ
                        | None -> Text.raw ""
                    ]
                ]
            ]
            let column1 = 
                Elem.table [] [
                    yield propertiesTableHeader
                    yield! propertiesTableItems
                ]


            let characteristicsTableHeader =
                Elem.tr [] [
                    Elem.th [] [ Text.raw "Property" ]
                    Elem.th [] [ Text.raw "Value" ]
                ]

            let characteristics = chd.Properties     
            let characteristicsTableItems = [
                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Armour" ]
                    Elem.td [] [ Text.raw $"{characteristics.Armour}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Background" ]
                    Elem.td [] [ Text.raw $"{characteristics.Background}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Extra" ]
                    Elem.td [] [ Text.raw $"{characteristics.Extra}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Head" ]
                    Elem.td [] [ Text.raw $"{characteristics.Head}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Magic" ]
                    Elem.td [] [ Text.raw $"{characteristics.Magic}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Skin" ]
                    Elem.td [] [ Text.raw $"{characteristics.Skin}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Weapon" ]
                    Elem.td [] [ Text.raw $"{characteristics.Weapon}" ]
                ]
            ]

            let column2 = 
                Elem.table [] [
                    yield characteristicsTableHeader
                    yield! characteristicsTableItems
                ]

            yield Elem.div [ Attr.class' "row" ] [
                yield Elem.div [ Attr.class' "column" ] [ yield column1 ]
                yield Elem.div [ Attr.class' "column" ] [ yield column2 ]
            ]

            yield Text.h1 $"Battles"

            let battlesTableHeader =
                Elem.tr [] [
                    Elem.th [] [ Text.raw "Number" ]
                    Elem.th [] [ Text.raw "Opponnent" ]
                    Elem.th [] [ Text.raw "Wager" ]
                    Elem.th [] [ Text.raw "Result" ]
                ]

            let battlesTableItems =
                chst.Battles
                |> List.mapi(fun i b ->
                    let opponent, bres =
                        if b.Winner.AssetId <> chst.Info.Champ.AssetId then b.Winner, "-"
                        else b.Loser, "+"
                    [
                        Elem.tr [] [
                            Elem.td [] [ UiUtils.linkToBattle b ]
                            Elem.td [] [ UiUtils.linkToChamp opponent ]
                            Elem.td [] [ Text.raw $"{b.Wager}" ]
                            Elem.td [] [ Text.raw $"{bres}" ]
                        ]
                        if i <= 10 then        
                            Elem.tr [] [
                                Elem.td [ Attr.colspan "4" ] [ Text.raw $"{b.Description}" ]
                            ]
                    ])
                |> List.concat
            yield Elem.table [] [
                yield battlesTableHeader
                yield! battlesTableItems
            ]
     ]

let champsPage (champs: ChampInfo list) =
    [
        yield Text.p $"All champs info"
        yield UI.getChampInfoTable champs
    ]

let champsHordePage (champs:ChampHorde list) =
    let getChampLevelsTable (champs:ChampHorde list) =
        let header =
            Elem.tr [ ] [
                Elem.th [ ] [ Text.raw "" ]
                Elem.th [ ] [ Text.raw "Icon" ]
                Elem.th [ ] [ Text.raw "Name" ]
                Elem.th [ ] [ Text.raw "Level" ]
                Elem.th [ ] [ ]
                Elem.th [ ] [ Text.raw "Progress" ]
            ]
        let items = 
            champs
            |> List.sortByDescending(fun ch -> ch.Xp)
            |> List.mapi(fun i ch ->
                let nextLvl = Horde.getCurrentLevelXp ch.Xp
                let progress = Text.raw $"{ch.Xp} / {nextLvl}"
                Elem.tr [] [
                    Elem.td [ ] [ Text.raw $"{i + 1}" ]
                    Elem.td [ ] [ ch.Champ.Ipfs |> UI.getIpfsImg "champImgSmall" ]
                    Elem.td [ ] [ Elem.a [ Attr.href $"https://dark-coin.com/arena/dragonshorde/{ch.Champ.AssetId}"; Attr.targetBlank ] [ Text.raw $"{ch.Champ.Name}" ] ]
                    Elem.td [ ] [ Text.raw $"{ch.Level}" ]
                    Elem.td [ ] [
                        Elem.progress [ Attr.max "100"; Attr.valueString ((ch.Xp * 100UL)/ nextLvl) ]
                            [ progress ]
                    ]
                    Elem.td [ ] [ progress ]

            ])
        Elem.table [ ] [
            yield header
            yield! items
        ]

    [
        yield Text.p $"All champs levels"
        yield getChampLevelsTable champs
    ]