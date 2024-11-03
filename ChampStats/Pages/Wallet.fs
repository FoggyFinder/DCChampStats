module Champs.Pages.Wallet

open Falco.Markup

let walletPage (wallet:string) (champs: Champs.Core.ChampInfo list) =
    [
        yield Text.p $"{wallet}"
        if champs.IsEmpty then
            yield Text.raw "Wallet doesn't hold any DarkCoin Champions NFT"
        else
            let header =
                Elem.tr [] [
                    Elem.th [ Attr.class' "w-50" ] [ Text.raw "Name (Asset Id)" ]
                    Elem.th [ Attr.class' "w-10" ] [ Text.raw "Wins" ]
                    Elem.th [ Attr.class' "w-10" ] [ Text.raw "Loses" ]
                    Elem.th [ Attr.class' "w-10" ] [ Text.raw "Fights" ]
                    Elem.th [ Attr.class' "w-20" ] [ Text.raw "Profit" ]
                ]
            let items = 
                champs |> List.map(fun ch ->
                    Elem.tr [] [
                        Elem.td [ ] [ Elem.a [ Attr.href $"/Champs/{ch.AssetId}" ] [ Text.raw $"{ch.Name} ({ch.AssetId})" ] ]
                        Elem.td [ ] [ Text.raw $"{ch.Wins}" ]
                        Elem.td [ ] [ Text.raw $"{ch.Loses}" ]
                        Elem.td [ ] [ Text.raw $"{ch.Fights}" ]
                        Elem.td [ ] [ Text.raw $"{ch.Profit}" ]
                ])
            yield Elem.table [ ] [
                yield header
                yield! items
            ]
    ]
