﻿namespace Champs.Core

type Champ = {
    AssetId: uint64
    Name: string
}

type Battle = {
    Winner: Champ
    Loser: Champ
    BattleNum: uint64
    Description: string
    Wager: decimal
}

type ChampInfo = {
    AssetId: uint64
    Name: string

    Wins: int
    Loses: int
    Earned: decimal
    Losed: decimal
}
    with
        member ci.Fights = ci.Wins + ci.Loses
        member ci.Profit = ci.Earned - ci.Losed

type ChampStats = {
    Info: ChampInfo
    Battles: Battle list
    MostFightsWith: Champ option
    MostWinsAgainst: Champ option
    MostLosesAgainst: Champ option
}

[<RequireQualifiedAccess>]
module Utils =
    open System
    open Newtonsoft.Json.Linq

    let battleFromString (battleNum:uint64) (content:string) =
        try 
            let key = 
                JObject.Parse(content).SelectToken("value").Value<string>()
                |> System.Convert.FromBase64String
                |> System.Text.ASCIIEncoding.ASCII.GetString
            let arr = key.Split(">")
            let winner = { Name = arr.[1]; AssetId = UInt64.Parse(arr.[0]) }
            let loser =  { Name = arr.[3]; AssetId = UInt64.Parse(arr.[2]) }
            let wager = decimal (UInt64.Parse(arr.[4])) / 1000000M
            { BattleNum = battleNum; Winner = winner; Loser = loser; Description = arr[5].Trim(); Wager = wager }
            |> Some
        with e ->
            None

    let parseRange (str:string) =
        let arr =
            str.Split("-")
            |> Array.choose(fun v ->
                match UInt64.TryParse v with
                | true, i -> Some i
                | false, _ -> None)
        if arr.Length <> 2 then None
        else Some(arr.[0], arr.[1])

    let toUInt64 (str:string) =
        match UInt64.TryParse str with
        | true, v -> Some v
        | false, _ -> None
