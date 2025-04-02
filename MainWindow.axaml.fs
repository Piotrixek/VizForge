namespace VizForge

open System
open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Media
open Avalonia.Platform.Storage
open Deedle
open LiveChartsCore
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.SkiaSharpView.Painting
open LiveChartsCore.Defaults
open SkiaSharp

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable dataFrame = Frame.ReadCsv("dummy.csv")
    let mutable chartSeries = ResizeArray<ISeries>()
    let mutable numericColumns = ResizeArray<string>()
    let mutable xAxisColumn = ""
    let mutable yAxisColumn = ""
    let mutable chartColor = Colors.Blue
    let mutable showGrid = true
    let mutable showLegend = true
    let mutable dataRange = 100.0

    member val ChartTypes = ResizeArray ["Line"; "Scatter"; "Bar"; "Area"] with get, set
    member val SelectedChartType = "Line" with get, set

    member this.NumericColumns
        with get() = numericColumns
        and set(value) = 
            numericColumns <- value
            this.OnPropertyChanged()

    member this.XAxisColumn
        with get() = xAxisColumn
        and set(value) = 
            xAxisColumn <- value
            this.OnPropertyChanged()
            this.UpdateChart()

    member this.YAxisColumn
        with get() = yAxisColumn
        and set(value) = 
            yAxisColumn <- value
            this.OnPropertyChanged()
            this.UpdateChart()

    member this.ChartSeries
        with get() = chartSeries
        and set(value) = 
            chartSeries <- value
            this.OnPropertyChanged()

    member this.ChartColor
        with get() = chartColor
        and set(value) = 
            chartColor <- value
            this.OnPropertyChanged()
            this.UpdateChart()

    member this.ShowGrid
        with get() = showGrid
        and set(value) = 
            showGrid <- value
            this.OnPropertyChanged()
            this.UpdateChart()

    member this.ShowLegend
        with get() = showLegend
        and set(value) = 
            showLegend <- value
            this.OnPropertyChanged()
            this.UpdateChart()

    member this.DataRange
        with get() = dataRange
        and set(value) = 
            dataRange <- value
            this.OnPropertyChanged()
            this.UpdateChart()

    member this.XAxes =
        ResizeArray [| 
            Axis(
                Name = this.XAxisColumn,
                LabelsPaint = SolidColorPaint(SKColors.Black),
                SeparatorsPaint = if this.ShowGrid then SolidColorPaint(SKColors.LightGray) else null
            )
        |]

    member this.YAxes =
        ResizeArray [| 
            Axis(
                Name = this.YAxisColumn,
                LabelsPaint = SolidColorPaint(SKColors.Black),
                SeparatorsPaint = if this.ShowGrid then SolidColorPaint(SKColors.LightGray) else null
            )
        |]

    member this.LegendPosition =
        if this.ShowLegend then LiveChartsCore.Measure.LegendPosition.Right 
        else LiveChartsCore.Measure.LegendPosition.Hidden

    member this.LoadDataCommand =
        RelayCommand(fun _ ->
            async {
                let topLevel = TopLevel.GetTopLevel(App.Current.MainWindow)
                let options = FilePickerOpenOptions(
                    AllowMultiple = false,
                    FileTypeFilter = [| 
                        FilePickerFileTypes.Csv 
                        FilePickerFileTypes.Json 
                    |]
                )

                let! files = topLevel.StorageProvider.OpenFilePickerAsync(options) |> Async.AwaitTask
                if files.Count > 0 then
                    use stream = files.[0].OpenReadAsync().Result
                    use reader = new StreamReader(stream)
                    
                    dataFrame <- match files.[0].Name with
                                 | n when n.EndsWith(".csv") -> Frame.ReadCsv(reader.BaseStream)
                                 | n when n.EndsWith(".json") -> Frame.ReadJson(reader.BaseStream)
                                 | _ -> dataFrame

                    this.NumericColumns <- ResizeArray(
                        dataFrame.ColumnKeys 
                        |> Seq.filter (fun k -> dataFrame.[k].ElementType = typeof<float>)
                        |> Seq.map string
                    )

                    if this.NumericColumns.Count >= 2 then
                        this.XAxisColumn <- this.NumericColumns.[0]
                        this.YAxisColumn <- this.NumericColumns.[1]
            } |> Async.StartImmediate
        )

    member this.UpdateChart() =
        if String.IsNullOrEmpty(this.XAxisColumn) || String.IsNullOrEmpty(this.YAxisColumn) then () else

        let filteredData = 
            dataFrame
            |> Frame.take (int (dataFrame.RowCount * dataRange / 100.0))
        
        let xValues = filteredData.GetColumn<float>(this.XAxisColumn).Values |> Array.ofSeq
        let yValues = filteredData.GetColumn<float>(this.YAxisColumn).Values |> Array.ofSeq

        let series = 
            match this.SelectedChartType with
            | "Line" -> 
                LineSeries<ObservableValue>(
                    Values = yValues |> Array.map (fun v -> ObservableValue(v)),
                    GeometrySize = 8.0,
                    Stroke = SolidColorPaint(SKColor(this.ChartColor.R, this.ChartColor.G, this.ChartColor.B), 2f)
                ) :> ISeries
            | "Scatter" ->
                ScatterSeries<ObservableValue>(
                    Values = yValues |> Array.map (fun v -> ObservableValue(v)),
                    GeometrySize = 12.0,
                    Fill = SolidColorPaint(SKColor(this.ChartColor.R, this.ChartColor.G, this.ChartColor.B), 2f)
                ) :> ISeries
            | "Bar" ->
                ColumnSeries<ObservableValue>(
                    Values = yValues |> Array.map (fun v -> ObservableValue(v)),
                    Fill = SolidColorPaint(SKColor(this.ChartColor.R, this.ChartColor.G, this.ChartColor.B))
                ) :> ISeries
            | _ -> LineSeries<ObservableValue>() :> ISeries

        this.ChartSeries <- ResizeArray [| series |]
        this.OnPropertyChanged("XAxes")
        this.OnPropertyChanged("YAxes")

type MainWindow() as this =
    inherit Window()

    do this.InitializeComponent()
       this.DataContext <- MainWindowViewModel()

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)