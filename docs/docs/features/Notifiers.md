# Notifiers

`Reactive.Bindings.Notifiers` namespace provides many useful classes which implement `IObservable` interface.

## `BooleanNotifier`

`BooleanNotifier` class implements the `IObservable<bool>` interface.
And has some methods and property.

- `TurnOn` method
    - Change state to true.
- `TurnOff` method
    - Change state to false.
- `SwitchValue` method
    - Switch state.
- `Value` property
    - Set state

The initial state can be set the constructor. The default value is false.


```csharp
var n = new BooleanNotifier();
n.Subscribe(x => Debug.WriteLine(x));

n.TurnOn(); // true
n.TurnOff(); // false
n.Value = true; // true
n.Value = false; // false
```

It can use as source of `ReactiveCommand` like below:

```csharp
var n = new BooleanNotifier(); // the default value is false.

// CanExecute method of ReactiveCommand returns true as default. So, you set initialValue explicitly to `n.Value`.
var command = n.ToReactiveCommand(initialValue: n.Value);

// Or if you would like to convert to something using Select and others before calling ToReactiveCommand, you can use StartWith.
var command2 = n.StartWith(n.Value).Select(x => Something(x)).ToReactiveCommand();
```

## `CountNotifier`

`CountNotifier` class implements the `IObservable<CountChangedStates>` interface. It provides increment and decrement features, and raise a `CountChangedStates` value when the state changes.

CountChangedStates enum is defined as below.

```csharp
/// <summary>Event kind of CountNotifier.</summary>
public enum CountChangedStatus
{
    /// <summary>Count incremented.</summary>
    Increment,
    /// <summary>Count decremented.</summary>
    Decrement,
    /// <summary>Count is zero.</summary>
    Empty,
    /// <summary>Count arrived max.</summary>
    Max
}
```

`CountNotifier`'s max value can be set from constructor argument:

```csharp
var c = new CountNotifier(); // default max value is int.MaxValue
// output status.
c.Subscribe(x => Debug.WriteLine(x));
// output current value.
c.Select(_ => c.Count).Subscribe(x => Debug.WriteLine(x));
// increment
var d = c.Increment(10);
// revert increment
d.Dispose();
// increment and decrement
c.Increment(10);
c.Decrement(5);
// output current value.
Debug.WriteLine(c.Count);
```

Output is below.

```
Increment
10
Decrement
0
Empty
0
Increment
10
Decrement
5
5
```

## `ScheduledNotifier`

This class raises the value on the scheduler. The default scheduler is `Scheduler.Immediate`. Set the scheduler using constructor argument.

```csharp
var n = new ScheduledNotifier<string>();
n.Subscribe(x => Debug.WriteLine(x));
// output the value immediately
n.Report("Hello world");
// output the value after 2 seconds.
n.Report("After 2 seconds.", TimeSpan.FromSeconds(2));
```

## `BusyNotifier`

This class implements the `IObservable<bool>` interface.
It raises `true` during running the process, raises `false` when all processes end.

The `StartProcess` method returns an `IDisposable` instance. When the process finishes, call the Dispose method.


```csharp
using Reactive.Bindings.Notifiers;
using System;
using System.Threading.Tasks;

namespace ReactivePropertyEduApp
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var b = new BusyNotifier();
            b.Subscribe(x => Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: OnNext: {x}"));

            await Task.WhenAll(
                Task.Run(async () =>
                {
                    using (b.ProcessStart())
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: Process1 started.");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: Process1 finished.");
                    }
                }),
                Task.Run(async () =>
                {
                    using (b.ProcessStart())
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: Process2 started.");
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: Process2 finished.");
                    }
                }));
        }
    }
}
```

Output is below.

```
15:07:45: OnNext: False
15:07:45: OnNext: True
15:07:45: Process1 started.
15:07:45: Process2 started.
15:07:46: Process1 finished.
15:07:47: Process2 finished.
15:07:47: OnNext: False
```


## `MessageBroker`

I suggest creating a new notifier called `MessageBroker`: an in-memory pubsub. This is an Rx and async friendly `EventAggregator` or `MessageBus`, etc. We can use this for the messenger pattern.
If reviewer accept this code, please add to all platforms.

```csharp
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

public class MyClass
{
    public int MyProperty { get; set; }

    public override string ToString()
    {
        return "MP:" + MyProperty;
    }
}
class Program
{
    static void RunMessageBroker()
    {
        // global scope pub-sub messaging
        MessageBroker.Default.Subscribe<MyClass>(x =>
        {
            Console.WriteLine("A:" + x);
        });

        var d = MessageBroker.Default.Subscribe<MyClass>(x =>
        {
            Console.WriteLine("B:" + x);
        });

        // support convert to IObservable<T>
        MessageBroker.Default.ToObservable<MyClass>().Subscribe(x =>
        {
            Console.WriteLine("C:" + x);
        });

        MessageBroker.Default.Publish(new MyClass { MyProperty = 100 });
        MessageBroker.Default.Publish(new MyClass { MyProperty = 200 });
        MessageBroker.Default.Publish(new MyClass { MyProperty = 300 });

        d.Dispose(); // unsubscribe
        MessageBroker.Default.Publish(new MyClass { MyProperty = 400 });
    }

    static async Task RunAsyncMessageBroker()
    {
        // asynchronous message pub-sub
        AsyncMessageBroker.Default.Subscribe<MyClass>(async x =>
        {
            Console.WriteLine("A:" + x);
            await Task.Delay(TimeSpan.FromSeconds(1));
        });

        var d = AsyncMessageBroker.Default.Subscribe<MyClass>(async x =>
        {
            Console.WriteLine("B:" + x);
            await Task.Delay(TimeSpan.FromSeconds(2));
        });

        // await all subscriber complete
        await AsyncMessageBroker.Default.PublishAsync(new MyClass { MyProperty = 100 });
        await AsyncMessageBroker.Default.PublishAsync(new MyClass { MyProperty = 200 });
        await AsyncMessageBroker.Default.PublishAsync(new MyClass { MyProperty = 300 });

        d.Dispose(); // unsubscribe
        await AsyncMessageBroker.Default.PublishAsync(new MyClass { MyProperty = 400 });
    }

    static void Main(string[] args)
    {
        Console.WriteLine("MessageBroker");
        RunMessageBroker();

        Console.WriteLine("AsyncMessageBroker");
        RunAsyncMessageBroker().Wait();
    }
}
```

Messenger pattern's multi thread dispatch can be handled easily by Rx.

```csharp
MessageBroker.Default.ToObservable<MyClass>()
    .ObserveOn(Dispatcher) // Rx Magic!
    .Subscribe(x =>
    {
        Console.WriteLine(x);
    });
```







