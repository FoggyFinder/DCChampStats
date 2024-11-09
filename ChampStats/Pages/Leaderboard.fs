module Champs.Pages.Leaderboard

open Falco.Markup
open Champs.Core

let leaderBoardPage (title:string) (leaderboard:ChampInfo list) =
    [
        if leaderboard.IsEmpty then
            yield Text.raw "No battles are found"
        else
            let battlesTableHeader =
                Elem.tr [ ] [
                    Elem.th [] [ Text.raw "Place" ]
                    Elem.th [] [ Text.raw "Champ" ]
                    Elem.th [] [ Text.raw "Win - Loses" ]
                    Elem.th [] [ Text.raw "Profit" ]
                ]

            let leaderboardTableItems =
                    leaderboard
                    |> List.mapi(fun i ci ->
                    Elem.tr [] [
                        Elem.td [] [ Text.raw ((i + 1).ToString()) ]
                        Elem.td [] [ Elem.a [ Attr.href $"/Champs/{ci.AssetId}" ] [ Text.raw $"{ci.Name} ({ci.AssetId})" ] ]
                        Elem.td [] [ Text.raw $"{ci.Wins - ci.Loses}" ]
                        Elem.td [] [ Text.raw $"{ci.Profit}" ]
                    ])
            
            yield Text.h1 title

            yield Elem.table [] [
                yield battlesTableHeader
                yield! leaderboardTableItems
            ]
    ]