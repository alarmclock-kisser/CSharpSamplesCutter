using System;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class ControlInvokeExtensions
{
    public static Task InvokeAsync(this Control control, Action action)
    {
        if (control == null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (control.IsDisposed)
        {
            return Task.CompletedTask;
        }

        if (!control.InvokeRequired)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<object?>();
        try
        {
            control.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }));
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        return tcs.Task;
    }

    public static Task InvokeAsync(this Control control, Func<Task> asyncAction)
    {
        if (control == null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        if (asyncAction == null)
        {
            throw new ArgumentNullException(nameof(asyncAction));
        }

        if (control.IsDisposed)
        {
            return Task.CompletedTask;
        }

        if (!control.InvokeRequired)
        {
            // direkt auf UI-Thread ausführen
            return asyncAction();
        }

        var tcs = new TaskCompletionSource<object?>();
        try
        {
            control.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await asyncAction().ConfigureAwait(true);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }));
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        return tcs.Task;
    }
}