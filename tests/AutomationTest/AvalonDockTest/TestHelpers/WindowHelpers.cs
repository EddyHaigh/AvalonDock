using System;
using System.Threading.Tasks;
using System.Windows;

namespace AvalonDockTest.TestHelpers;

public static class WindowHelpers
{
    public static Task<T> CreateInvisibleWindowAsync<T>(Action<T> changeAdditionalProperties = null)
        where T : Window, new()
    {
        var window = new T()
        {
            Visibility = Visibility.Hidden,
            ShowInTaskbar = false
        };

        changeAdditionalProperties?.Invoke(window);

        var completionSource = new TaskCompletionSource<T>();

        window.Activated += Handler;

        window.Show();

        return completionSource.Task;

        void Handler(object sender, EventArgs args)
        {
            window.Activated -= Handler;
            completionSource.SetResult(window);
        }
    }
}