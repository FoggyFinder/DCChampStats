module Champs.Pages.Battle

open Falco.Markup
open Champs.Core
open UI

let battlePage (battleo:Battle option) =
    [
        match battleo with
        | None ->
            yield Text.raw "Battle either not found or request can't be evaluated at the moment. Verify id and try later"
        | Some battle  ->
            yield 
                [ Elem.tr [] [
                    Elem.td [] [ battle.Winner.Ipfs |> UI.getIpfsImg "champImgAvg" ]
                    Elem.td [] [ 
                        Text.raw $"{battle.Wager}"
                        //if battle.UTCDateTime.IsSome then
                        //    Elem.br []
                        //    Text.raw $"{battle.UTCDateTime.Value.ToString()}"
                    ]
                    Elem.td [] [ battle.Loser.Ipfs |> UI.getIpfsImg "champImgAvg" ]
                  ]

                  Elem.tr [] [
                    Elem.td [] [ UiUtils.linkToChamp battle.Winner ]
                    Elem.td [] [ Text.raw $"{battle.Description}" ]
                    Elem.td [] [ UiUtils.linkToChamp battle.Loser ]
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
                        Elem.td [] [ UiUtils.linkToBattle b ]
                        Elem.td [] [ UiUtils.linkToChamp b.Winner ]
                        Elem.td [] [ UiUtils.linkToChamp b.Loser ] 
                        Elem.td [] [ Text.raw $"{b.Wager}" ]
                    ]
                    if i <= 10 then
                        Elem.tr [] [
                            Elem.td [ ] [ b.Winner.Ipfs |> UI.getIpfsImg "champImgSmall" ]
                            Elem.td [ Attr.colspan "2" ] [ Text.raw $"{b.Description}" ]
                            Elem.td [ ] [ b.Loser.Ipfs |> UI.getIpfsImg "champImgSmall" ]
                        ]
                ])
            |> List.concat

        yield Elem.table [] [
            yield battlesTableHeader
            yield! battlesTableItems
        ]
    ]