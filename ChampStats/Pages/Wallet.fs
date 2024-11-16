module Champs.Pages.Wallet

open Falco.Markup

let walletPage (wallet:string) (champs: Champs.Core.ChampInfo list) =
    [
        yield Text.p $"{wallet}"
        if champs.IsEmpty then
            yield Text.raw "Wallet doesn't hold any DarkCoin Champions NFT"
        else
            yield UI.getChampInfoTable champs
    ]
