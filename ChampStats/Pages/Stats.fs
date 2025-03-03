module Champs.Pages.Stats

open Falco.Markup
open Champs.Core
open System

let statsPage (activity: Activity) =
    [
        yield Text.raw "Short stats info, data is grouped by server time"
        yield
            Elem.table [] [
                Elem.tr [] [
                    Elem.td [] [ Text.raw "Today" ]
                    Elem.td [] [ Text.raw (string activity.Today) ]
                ]
                
                Elem.tr [] [
                    Elem.td [] [ Text.raw "Yesterday" ]
                    Elem.td [] [ Text.raw (string activity.Yesterday) ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw "This week" ]
                    Elem.td [] [ Text.raw (string activity.Week) ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw "30 days" ]
                    Elem.td [] [ Text.raw (string activity.Month) ]
                ]
                
                Elem.tr [] [
                    Elem.td [] [ Text.raw "Untracked *" ]
                    Elem.td [] [ Text.raw (string activity.Untracked) ]
                ]

                Elem.tr [] [
                    Elem.td [] [ Text.raw "All" ]
                    Elem.td [] [ Text.raw (string activity.Total) ]
                ]
            ]
        yield Text.raw $"* - currently only new battles are tracked. Old smart-contract didn't contain easy way to get `battleNum` from tx"
        yield Elem.br []
        yield Text.raw $"Server time: {DateTime.Now}"
    ]