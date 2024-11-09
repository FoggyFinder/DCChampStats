module Champs.Pages.Battle

open Falco.Markup
open Champs.Core

let battlePage (battleo:Battle option) =
    [
        match battleo with
        | None ->
            yield Text.raw "Battle either not found or request can't be evaluated at the moment. Verify id and try later"
        | Some battle  ->
            yield 
                [ Elem.tr [] [
                    Elem.td [] [ Text.raw "" ]
                    Elem.td [] [ Text.raw $"{battle.Wager}" ]
                    Elem.td [] [ Text.raw "" ]
                  ]

                  Elem.tr [] [
                    Elem.td [] [ 
                        Elem.a [ Attr.href $"/Champs/{battle.Winner.AssetId}" ] [ Text.raw $"{battle.Winner.Name} ({battle.Winner.AssetId})" ]
                    ]
                    Elem.td [] [ Text.raw $"{battle.Description}" ]
                    Elem.td [] [
                        Elem.a [ Attr.href $"/Champs/{battle.Loser.AssetId}" ] [ 
                            Text.raw $"{battle.Loser.Name} ({battle.Loser.AssetId})"
                        ]
                     ]
                  ]
                ] |> Elem.table []
    ]

let battlesPage (battles:Battle list) =
    [
        yield Text.h1 $"Battles"

        let battlesTableHeader =
            Elem.tr [] [
                Elem.th [] [ Text.raw "Number" ]
                Elem.th [] [ Text.raw "Winner" ]
                Elem.th [] [ Text.raw "Loser" ]
                Elem.th [] [ Text.raw "Wager" ]
            ]

        let battlesTableItems =
            battles
            |> List.sortByDescending(fun b -> b.BattleNum)
            |> List.mapi(fun i b ->
                [
                    Elem.tr [] [
                        Elem.td [] [ Elem.a [ Attr.href $"/Battles/{b.BattleNum}" ] [ Text.raw $"{b.BattleNum}" ] ]
                        Elem.td [] [ Elem.a [ Attr.href $"/Champs/{b.Winner.AssetId}" ] [ 
                            Text.raw $"{b.Winner.Name} ({b.Winner.AssetId})" ]
                        ]
                        Elem.td [] [ Elem.a [ Attr.href $"/Champs/{b.Loser.AssetId}" ] [ 
                            Text.raw $"{b.Loser.Name} ({b.Loser.AssetId})" ]
                        ]
                        Elem.td [] [ Text.raw $"{b.Wager}" ]
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