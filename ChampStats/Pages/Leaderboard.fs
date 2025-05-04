module Champs.Pages.Leaderboard

open Falco.Markup
open Champs.Core
open UI

let leaderBoardPage (leaderboard:LeaderBoard) =
    [
        match leaderboard.Battles with
        | LeaderboardRange.EmptyOrInvalid ->
            yield Text.raw "No battles are found"
        | _ ->
            let battlesTableHeader =
                Elem.tr [ ] [
                    Elem.th [] [ Text.raw "Place" ]
                    Elem.td [] [ Text.raw "Icon" ]
                    Elem.th [] [ Text.raw "Champ" ]
                    Elem.th [] [ Text.raw "Win - Loses" ]
                    Elem.th [] [ Text.raw "Profit" ]
                ]

            let leaderboardTableItems =
                leaderboard.Leaderboard
                |> List.mapi(fun i ci ->
                    Elem.tr [] [
                        Elem.td [] [ Text.raw ((i + 1).ToString()) ]
                        Elem.td [] [ ci.Champ.Ipfs |> UI.getIpfsImg "champImgSmall" ]
                        Elem.td [] [ UiUtils.linkToChamp ci.Champ ]
                        Elem.td [] [ Text.raw $"{ci.Wins - ci.Loses}" ]
                        Elem.td [] [ Text.raw $"{ci.Profit}" ]
                    ])
            
            let title =
                match leaderboard.Battles with
                | LeaderboardRange.EmptyOrInvalid -> ""
                | LeaderboardRange.Full -> ""
                | LeaderboardRange.Range (x, y) -> $"({x} - {y})"

            yield Text.h1 ("Leaderboard " + title)

            yield Elem.table [] [
                yield battlesTableHeader
                yield! leaderboardTableItems
            ]
    ]