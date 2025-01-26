module Champs.Pages.Home

open Falco.Markup
open Champs.Core

let homePage(contributors: Contributor list) =
    [
        yield Text.h1 $"Tool for tracking DarkCoin Champions NFT battles."
        yield Elem.hr []
        yield Text.p "Currently available routes"
        yield Elem.ul [] [
            yield Elem.li [] [
                yield Text.code "wallets/{wallet}"
                yield Text.raw " - shows all Champs for specified wallet and their statistic. For example, "
                yield Elem.a [ Attr.href "/wallets/G6YFTYHG5NGTRLUWYVZOY2OODHYJEFA4E57M4HN7NKP4NC3EUQJWMT5ZMA" ] 
                    [ Text.raw $"/wallets/G6YFTYHG5NGTRLUWYVZOY2OODHYJEFA4E57M4HN7NKP4NC3EUQJWMT5ZMA" ] 
                ]
            
            yield Elem.li [] [
                yield Text.code "champs/{champId}"
                yield Text.raw " - shows stat for specific Champ, where ChampId is AssetId. For example, "
                yield Elem.a [ Attr.href "/champs/1559258156" ] 
                    [ Text.raw $"/champs/1559258156" ] 
                ]
            
            yield Elem.li [] [
                yield Text.code "/battles"
                yield Text.raw " - list of all battles. Try it out: "
                yield Elem.a [ Attr.href "/battles" ] 
                    [ Text.raw $"/battles" ] 
                ]

            yield Elem.li [] [
                yield Text.code "/battles/{battleNum}"
                yield Text.raw " - shows details for specific battle, where battleNum is the number of the battle. For example, "
                yield Elem.a [ Attr.href "/battles/1189" ]
                    [ Text.raw "/battles/1189" ]
                ]
            
            yield Elem.li [] [
                yield Text.code "/leaderboard"
                yield Text.raw " - shows top Champs. Try it out: "
                yield Elem.a [ Attr.href "/leaderboard" ]
                    [ Text.raw "/leaderboard" ]
                ]
            
            yield Elem.li [] [
                yield Text.code "/leaderboard/{range}"
                yield Text.raw " - shows top Champs among selected range of battle. For example, to print table for battles from 1000 to 1100 one shall use "
                yield Elem.a [ Attr.href "/leaderboard/1000-1099" ]
                    [ Text.raw "/leaderboard/1000-1099" ]
            ]

            yield Elem.li [] [
                yield Text.code "/faq"
                yield Text.raw " - answers to most common questions, "
                yield Elem.a [ Attr.href "/faq" ]
                    [ Text.raw "/faq" ]
            ]
        ]
        yield Elem.hr []
        yield Elem.p [] [
            yield Text.raw "Source code is available "
            yield Elem.a [ Attr.href "https://github.com/FoggyFinder/DCChampStats" ]
                    [ Text.raw "on github" ]
        ]
        yield Text.p "Contributors:"
        let contributors =
            contributors
            |> List.map(fun c ->
                Elem.li [] [
                    Elem.div [] [
                        yield Elem.img [
                            Attr.class' "iconImg"
                            Attr.src c.Img
                        ]
                        yield Elem.a [ Attr.href c.Url ]
                            [ Text.code c.Name ]
                        ]
                ]
            )
        yield Elem.ul [] contributors

        yield Elem.hr []

        yield Text.p "To support project or author:"
        yield Elem.ul [] [
            yield Elem.li [] [
                yield Text.code "foggyfinder.algo"
                yield Text.raw " (NFD)"
            ]

            yield Elem.li [] [
                yield Text.code "G6YFTYHG5NGTRLUWYVZOY2OODHYJEFA4E57M4HN7NKP4NC3EUQJWMT5ZMA"
                yield Text.raw " (wallet)"
            ]
        ]

        yield Elem.hr []
        yield Text.p "Tools that were used to build the web-app"
        yield Elem.ul [] [
            yield Elem.li [] [
                yield Elem.a [ Attr.href "https://www.falcoframework.com/docs/" ] 
                    [ Text.raw $"Falco" ] 
                
                yield Text.raw " - a toolkit for building fast and functional-first web applications using F#"
            ]  
            
            yield Elem.li [] [
                yield Elem.a [ Attr.href "https://nodely.io/" ] 
                    [ Text.raw $"Nodely" ] 
                
                yield Text.raw " - Free API to interact with Algorand chain"
            ]

            yield Elem.li [] [
                yield Elem.a [ Attr.href "https://github.com/FrankSzendzielarz/dotnet-algorand-sdk" ] 
                    [ Text.raw $".NET Algorand SDK (v2)" ] 
                
                yield Text.raw " - The .NET Algorand SDK is a dotnet library for communicating and interacting with the Algorand network from .NET applications"
            ]
            
            yield Elem.li [] [
                yield Elem.a [ Attr.href "https://github.com/pimbrouwers/Donald" ] 
                    [ Text.raw $"Donald" ] 
                
                yield Text.raw " - A lightweight, generic F# database abstraction."
            ]

            yield Elem.li [] [
                yield Elem.a [ Attr.href "https://www.sqlite.org/" ] 
                    [ Text.raw $"SQLite" ] 
                
                yield Text.raw " - to store some info to avoid unnecessary request to API"
            ]
        ]
        yield Elem.hr []
        yield Text.p "Version: 0.1"
    ]