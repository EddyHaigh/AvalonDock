using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace AvalonDockTest.TestHelpers;

/// <summary>
/// This class is the ultimate hack to work around that we can't 
/// create more than one application in the same AppDomain
/// 
/// It is initialized once at startup and is never properly cleaned up, 
/// this means the AppDomain will throw an exception when xUnit unloads it.
/// 
/// Your test runner will inevitably hate you and hang endlessly after every test has run.
/// The Resharper runner will also throw an exception message in your face.
/// 
/// Better than no unit tests.
/// </summary>
public class TestHost
{
    private static TestHost testHost;

    private readonly Thread _appThread;
    private readonly AutoResetEvent _gate = new(false);

    private TestApp _app;

    private TestHost()
    {
        try
        {
            _appThread = new Thread(StartDispatcher);
            _appThread.SetApartmentState(ApartmentState.STA);
            _appThread.Start();

            _gate.WaitOne();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    public static void Initialize()
    {
        testHost ??= new TestHost();
    }

    /// <summary>
    /// Await this method in every test that should run on the UI thread.
    /// </summary>
    public static SwitchContextToUiThreadAwaiter SwitchToAppThread()
    {
        return new SwitchContextToUiThreadAwaiter(testHost._app.Dispatcher);
    }

    private void StartDispatcher()
    {
        _app = new TestApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        _app.Exit += (sender, args) =>
            {
                var message = $"""
======= Exit TestApp =======
Thread.CurrentThread:       {Environment.CurrentManagedThreadId}
Current.Dispatcher.Thread:  {Application.Current.Dispatcher.Thread.ManagedThreadId}
""";
                Debug.WriteLine(message);
            };

        _app.Startup += (sender, args) =>
            {
                var message = $"""
====== Start TestApp ======
Thread.CurrentThread:      {Environment.CurrentManagedThreadId}
Current.Dispatcher.Thread: {Application.Current.Dispatcher.Thread.ManagedThreadId}
""";
                Debug.WriteLine(message);
                _gate.Set();
            };
        _app.Run();
    }
}