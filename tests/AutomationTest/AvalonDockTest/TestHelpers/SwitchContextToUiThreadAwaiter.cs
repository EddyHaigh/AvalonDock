using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace AvalonDockTest.TestHelpers;

public class SwitchContextToUiThreadAwaiter : INotifyCompletion
{
    private readonly Dispatcher _uiContext;

    public SwitchContextToUiThreadAwaiter(Dispatcher uiContext)
    {
        this._uiContext = uiContext;
    }

    public SwitchContextToUiThreadAwaiter GetAwaiter()
    {
        return this;
    }

    public bool IsCompleted => false;

    public void OnCompleted(Action continuation)
    {
        this._uiContext.Invoke(continuation);
    }

    public void GetResult() { }
}