namespace Champs.Core

type Contributor = {
    Name: string
    Img: string
    Url: string
}

type Champ = {
    AssetId: uint64
    Name: string
    Ipfs : string option
}

type Battle = {
    Winner: Champ
    Loser: Champ
    BattleNum: uint64
    Description: string
    Wager: decimal
}

type ChampInfo = {
    Champ: Champ

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

type ChampProperties = {
    Ipfs: string
    Armour: string
    Background: string
    Extra: string
    Head: string
    Magic: string
    Skin: string
    Weapon: string
}

type ChampDetailed = {
    Stats: ChampStats
    Properties: ChampProperties
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
            let winner = { Name = arr.[1]; AssetId = UInt64.Parse(arr.[0]); Ipfs = None }
            let loser =  { Name = arr.[3]; AssetId = UInt64.Parse(arr.[2]); Ipfs = None }
            let wager = decimal (UInt64.Parse(arr.[4])) / 1000000M
            { BattleNum = battleNum; Winner = winner; Loser = loser; Description = arr[5].Trim(); Wager = wager }
            |> Some
        with e ->
            None

    let getListOfContributors(json:string) =
        try
            JArray.Parse(json)
            |> Seq.map(fun jObj ->
                {
                    Name = jObj.Value<string>("login")
                    Img = jObj.Value<string>("avatar_url")
                    Url = jObj.Value<string>("html_url")
                })
            |> Seq.toList
        with e ->
            []

    let parseRange (str:string) =
        let arr =
            str.Split("-")
            |> Array.map(fun v ->
                match UInt64.TryParse v with
                | true, i -> Some i
                | false, _ -> None)
        if arr.Length = 1 then
            arr |> Array.head, None
        elif arr.Length = 2 then
            arr.[0], arr.[1]
        else None, None

    let toUInt64 (str:string) =
        match UInt64.TryParse str with
        | true, v -> Some v
        | false, _ -> None
