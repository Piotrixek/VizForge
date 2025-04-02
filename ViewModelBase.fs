namespace VizForge

open System
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Windows.Input

type ViewModelBase() =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChanged.Publish

    member this.OnPropertyChanged([<CallerMemberName>] ?propertyName: string) =
        propertyChanged.Trigger(this, PropertyChangedEventArgs(defaultArg propertyName null))

type RelayCommand(action: obj -> unit) =
    interface ICommand with
        member val CanExecuteChanged = Event<_,_>().Publish
        
        member _.CanExecute(_) = true
        member _.Execute(param) = action param