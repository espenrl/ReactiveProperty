# Transfer events to ViewModels from Views

`EventToReactiveProperty` and `EventToReactiveCommand` classes transfer events to a `ReactiveProperty` and `ReactiveCommand` from the View layer.
Those classes extend `TriggerAction`. Those are designed that uses together with `EventTrigger`.

<b>Note:</b> 
> This feature available only WPF and UWP. Xamarin.Forms can't use this. If you would like to use it, then please add `ReactiveProperty.WPF` package for WPF or `ReactiveProperty.UWP` package for UWP to your project.

Those classes can convert `EventArgs` to any types object using `ReactiveConverter<T, U>`.

`ReactiveConverter` class can use Rx method chain. It's very powerful.


UWP sample:

```csharp
using Reactive.Bindings.Interactivity;
using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;

namespace App1
{
    public class FileOpenReactiveConverter : ReactiveConverter<RoutedEventArgs, string>
    {
        protected override IObservable<string> OnConvert(IObservable<RoutedEventArgs> source)
        {
            return source.SelectMany(async _ =>
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".snippet");
                var f = await picker.PickSingleFileAsync();
                return f?.Path;
            })
            .Where(x => x != null);

        }
    }
}
```

It converts the `RoutedEventArgs` to the file path.

XAML and Code behind are below.

```xml
<Page x:Class="App1.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:App1"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:i="using:Microsoft.Xaml.Interactivity"
      xmlns:c="using:Microsoft.Xaml.Interactions.Core"
      xmlns:reactiveProperty="using:Reactive.Bindings.Interactivity"
      mc:Ignorable="d">
    <StackPanel Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Button Content="OpenFile...">
            <i:Interaction.Behaviors>
                <c:EventTriggerBehavior EventName="Click">
                    <reactiveProperty:EventToReactiveCommand Command="{x:Bind ViewModel.SelectFileCommand}">
                        <local:FileOpenReactiveConverter />
                    </reactiveProperty:EventToReactiveCommand>
                </c:EventTriggerBehavior>
            </i:Interaction.Behaviors>
        </Button>
        <TextBlock Text="{x:Bind ViewModel.FileName.Value, Mode=OneWay}" />
    </StackPanel>
</Page>
```

```csharp
using Reactive.Bindings;
using Windows.UI.Xaml.Controls;

namespace App1
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; } = new MainPageViewModel();

        public MainPage()
        {
            this.InitializeComponent();
        }
    }

    public class MainPageViewModel
    {
        public ReactiveCommand<string> SelectFileCommand { get; }
        public ReadOnlyReactiveProperty<string> FileName { get; }

        public MainPageViewModel()
        {
            this.SelectFileCommand = new ReactiveCommand<string>();
            this.FileName = this.SelectFileCommand.ToReadOnlyReactiveProperty();
        }
    }

}
```

![EventToReactiveCommand and EventToReactiveProperty](./images/event-to-reactivexxx.gif)


`EventToReactiveProperty` sets the value converted by `ReactiveConverter` to `ReactiveProperty`.

```xml
<Page x:Class="App1.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:App1"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:i="using:Microsoft.Xaml.Interactivity"
      xmlns:c="using:Microsoft.Xaml.Interactions.Core"
      xmlns:reactiveProperty="using:Reactive.Bindings.Interactivity"
      mc:Ignorable="d">
    <StackPanel Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Button Content="OpenFile...">
            <i:Interaction.Behaviors>
                <c:EventTriggerBehavior EventName="Click">
                    <reactiveProperty:EventToReactiveProperty ReactiveProperty="{x:Bind ViewModel.FileName}">
                        <local:FileOpenReactiveConverter />
                    </reactiveProperty:EventToReactiveProperty>
                </c:EventTriggerBehavior>
            </i:Interaction.Behaviors>
        </Button>
        <TextBlock Text="{x:Bind ViewModel.FileName.Value, Mode=OneWay}" />
    </StackPanel>
</Page>
```

```csharp
using Reactive.Bindings;
using Windows.UI.Xaml.Controls;

namespace App1
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; } = new MainPageViewModel();

        public MainPage()
        {
            this.InitializeComponent();
        }
    }

    public class MainPageViewModel
    {
        public ReactiveProperty<string> FileName { get; } = new ReactiveProperty<string>();
    }

}
```

## Customizing EventToReactiveCommand

### CallExecuteOnScheduler property

The default behavior is calling Command's Execute method on `IScheduler` that is set to `ReactivePropertyScheduler.Default`. If you disable this behavior, set this property to false.

### AutoEnable property

The default behavior is synchronizing automatically between AssosiateObject.IsEnabled and the Command's CanExecute.
If you want to disable this behavior, set this property to false.
