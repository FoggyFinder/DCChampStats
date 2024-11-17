﻿module UI

open Falco.Markup
let [<Literal>] IPFS = "https://ipfs.dark-coin.io/ipfs/"

let layout (title:string) (content : XmlNode list)  =
    Elem.html [ Attr.lang "en"; ] [
        Elem.head [] [
            Elem.meta  [ Attr.charset "UTF-8" ]
            Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
            Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]
            
            Elem.title [] [ Text.raw title ]
            
            Elem.link  [ Attr.href "/styles.css"; Attr.rel "stylesheet" ]

            Elem.link [ Attr.rel "shortcut icon"; Attr.href "/favicon.ico"; Attr.type' "image/x-icon" ]

            Elem.link [ Attr.rel "icon"; Attr.href "/favicon.ico"; Attr.type' "image/x-icon" ]
        ]

        Elem.body [ ] [ Elem.main [] content ]
    ]


let getIpfsImg (className: string) (ipfso: string option) =
     ipfso |> Option.map(fun ipfs ->
        Elem.img [
            Attr.class' className
            Attr.src (IPFS + ipfs)
        ])
    |> Option.defaultWith (fun () -> Text.raw "")

let getChampInfoTable (champs:Champs.Core.ChampInfo list) =
    let header =
        Elem.tr [ ] [
            Elem.th [ ] [ Text.raw "" ]
            Elem.th [ ] [ Text.raw "Icon" ]
            Elem.th [ ] [ Text.raw "Name" ]
            Elem.th [ ] [ Text.raw "Wins" ]
            Elem.th [ ] [ Text.raw "Loses" ]
            Elem.th [ ] [ Text.raw "Fights" ]
            Elem.th [ ] [ Text.raw "Profit" ]
        ]
    let items = 
        champs |> List.mapi(fun i ch ->
            Elem.tr [] [
                Elem.td [ ] [ Text.raw $"{i + 1}" ]
                Elem.td [ ] [ ch.Champ.Ipfs |> getIpfsImg "champImgSmall" ]
                Elem.td [ ] [ Elem.a [ Attr.href $"/Champs/{ch.Champ.AssetId}" ] [ Text.raw $"{ch.Champ.Name}" ] ]
                Elem.td [ ] [ Text.raw $"{ch.Wins}" ]
                Elem.td [ ] [ Text.raw $"{ch.Loses}" ]
                Elem.td [ ] [ Text.raw $"{ch.Fights}" ]
                Elem.td [ ] [ Text.raw $"{ch.Profit}" ]
        ])
    Elem.table [ ] [
        yield header
        yield! items
    ]
