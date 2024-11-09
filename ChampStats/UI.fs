module UI

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


