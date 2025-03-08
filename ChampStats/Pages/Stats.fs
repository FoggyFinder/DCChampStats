module Champs.Pages.Stats

open Falco.Markup
open Champs.Core
open System
open Plotly.NET
open Newtonsoft.Json

let CreateChartScript(data: string, layout: string, config: string, plotlyReference: PlotlyJSReference, guid: string) =
    match plotlyReference with
    | CDN _ ->
        Elem.script
            [ Attr.type' "text/javascript" ]
            [
                Text.raw (
                    Globals.SCRIPT_TEMPLATE
                        .Replace("[SCRIPTID]", guid.Replace("-", ""))
                        .Replace("[ID]", guid)
                        .Replace("[DATA]", data)
                        .Replace("[LAYOUT]", layout)
                        .Replace("[CONFIG]", config)
                )
            ]
    | _ -> failwith "Not supported"

let CreateChartHTML(data: string, layout: string, config: string, plotlyReference: PlotlyJSReference) =
    let guid = Guid.NewGuid().ToString()

    let chartScript = CreateChartScript(data, layout, config, plotlyReference, guid)

    Elem.div [ Attr.class' "chartJs"; Attr.id guid ] [
        chartScript
    ]

let chartToHTML(gChart:GenericChart) =
    let JSON_CONFIG =
        JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Serialize)
    let tracesJson =
        let traces = GenericChart.getTraces gChart
        JsonConvert.SerializeObject(traces, JSON_CONFIG)

    let layoutJson =
        let layout = GenericChart.getLayout gChart
        JsonConvert.SerializeObject(layout, JSON_CONFIG)

    let configJson =
        let config = GenericChart.getConfig gChart
        JsonConvert.SerializeObject(config, JSON_CONFIG)
        
    let displayOpts = GenericChart.getDisplayOptions gChart    
        
    let plotlyReference =
        displayOpts |> DisplayOptions.getPlotlyReference

    CreateChartHTML(tracesJson, layoutJson, configJson, plotlyReference)
            
let statsPage (ar: ActivityReport) =
    let activityTable (activity: Activity) =
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
                Elem.td [] [ Text.raw "Unprocessed" ]
                Elem.td [] [ Text.raw (string activity.Untracked) ]
            ]

            Elem.tr [] [
                Elem.td [] [ Text.raw "All" ]
                Elem.td [] [ Text.raw (string activity.Total) ]
            ]
        ]
    
    let warriorsChart =
        let active, nonActive = ar.Warriors
        Chart.Doughnut([active; nonActive], Labels=["Active"; "Inactive"])

    let monthlyActivityChart =
        let keys, values =
            ar.MonthlyBattles
            |> List.map(fun (dt, battles) ->
                dt.ToString("dd.MM.yyyy"), battles)
            |> List.unzip
        Chart.Column(values, keys)

    let battlesChart =
        let x, y =
            ar.BattlesByDay
            |> List.map(fun (dt, battles) ->
                dt.ToString("dd.MM.yyyy"), battles)
            |> List.unzip
        Chart.Line(x = x, y = y, Name = "battles", ShowMarkers = true, MarkerSymbol = StyleParam.MarkerSymbol.Square)
        |> Chart.withLineStyle (Width = 2., Dash = StyleParam.DrawingStyle.Dot)
    [
        yield Elem.h1 [] [ Text.raw "Stats info (data is grouped by server time)" ]
        yield Elem.hr []
        
        let column1 =
            [
                yield activityTable ar.Activity
            ]
        
        let column2 =
            [
                yield Text.raw $"30-days activity"
                yield chartToHTML monthlyActivityChart
            ]

        yield Elem.div [ Attr.class' "row" ] [
                yield Elem.div [ Attr.class' "column" ] [ yield! column1 ]
                yield Elem.div [ Attr.class' "column" ] [ yield! column2 ]
            ]

        let column3 =
            [
                yield Text.raw "Active Champs (participated at least in one battle)"
                yield chartToHTML warriorsChart    
            ]
        
        let column4 =
            [
                yield Text.raw $"Overall battles"
                yield chartToHTML battlesChart          
            ]

        yield Elem.div [ Attr.class' "row" ] [
                yield Elem.div [ Attr.class' "column" ] [ yield! column3 ]
                yield Elem.div [ Attr.class' "column" ] [ yield! column4 ]
            ]

        yield Elem.br []
        yield Elem.hr []
        yield Text.raw $"Server time: {DateTime.Now}"
    ]