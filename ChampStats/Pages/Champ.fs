module Champs.Pages.Champs

open Falco.Markup
open Champs.Core

let champPage (champDetailedO: ChampDetailed option) =
    [
        match champDetailedO with
        | None -> yield Text.raw "Champ either not found or request can't be evaluated at the moment. Verify id and try later"
        | Some chd  ->
            let chst = chd.Stats
            yield Text.h1 $"{chst.Info.Name} stats"
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
                        | Some champ ->
                            Elem.a [ Attr.href $"/Champs/{champ.AssetId}" ] [
                                Text.raw $"{champ.Name} ({champ.AssetId})" 
                            ]
                        | None -> Text.raw ""
                    ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Most wins against" ]
                    Elem.td [] [
                        match chst.MostWinsAgainst with
                        | Some champ ->
                            Elem.a [ Attr.href $"/Champs/{champ.AssetId}" ] [
                                Text.raw $"{champ.Name} ({champ.AssetId})"
                            ]
                        | None -> Text.raw ""
                    ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Most losses against" ]
                    Elem.td [] [ 
                        match chst.MostLosesAgainst with
                        | Some champ ->
                            Elem.a [ Attr.href $"/Champs/{champ.AssetId}" ] [
                                Text.raw $"{champ.Name} ({champ.AssetId})" 
                            ]
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
                        if b.Winner.AssetId <> chst.Info.AssetId then b.Winner, "-"
                        else b.Loser, "+"
                    [
                        Elem.tr [] [
                            Elem.td [] [ Elem.a [ Attr.href $"/Battles/{b.BattleNum}" ] [ Text.raw $"{b.BattleNum}" ] ]
                            Elem.td [] [ Elem.a [ Attr.href $"/Champs/{opponent.AssetId}" ] [ 
                                Text.raw $"{opponent.Name} ({opponent.AssetId})" ]
                            ]
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
