﻿using System;
using System.Windows.Threading;

namespace AvalonDockTest.TestHelpers;
public static class Extensions
{
    public static void Invoke(this DispatcherObject dispatcherObject, Action invokeAction)
    {
        if (dispatcherObject == null)
        {
            throw new ArgumentNullException(nameof(dispatcherObject));
        }
        if (invokeAction == null)
        {
            throw new ArgumentNullException(nameof(invokeAction));
        }
        if (dispatcherObject.Dispatcher.CheckAccess())
        {
            invokeAction();
        }
        else
        {
            dispatcherObject.Dispatcher.Invoke(invokeAction);
        }
    }
}