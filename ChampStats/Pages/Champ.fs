module Champs.Pages.Champs

open Falco.Markup
open Champs.Core

let champPage (champStatso: ChampStats option) =
    [
        match champStatso with
        | None -> yield Text.raw "Champ either not found or request can't be evaluated at the moment. Verify id and try later"
        | Some chst  ->
            yield Text.h1 $"{chst.Info.Name} stats"
            let propertiesTableHeader =
                Elem.tr [] [
                    Elem.th [] [ Text.raw "Property" ]
                    Elem.th [] [ Text.raw "Value" ]
                ]
                    
            let propertiesTableItems = [
                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Id" ]
                    Elem.td [] [ Text.raw $"{chst.Info.AssetId}" ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw $"Name" ]
                    Elem.td [] [ Text.raw $"{chst.Info.Name}" ]
                ]

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

            yield Elem.table [] [
                yield propertiesTableHeader
                yield! propertiesTableItems
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
                |> List.collect(fun b ->
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
                                
                        Elem.tr [] [
                            Elem.td [ Attr.colspan "4" ] [ Text.raw $"{b.Description}" ]
                        ]
                    ])

            yield Elem.table [] [
                yield battlesTableHeader
                yield! battlesTableItems
            ]
     ]
