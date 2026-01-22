namespace CodexBarWin.Helpers;

/// <summary>
/// Helper methods for async operations.
/// </summary>
public static class AsyncHelper
{
    /// <summary>
    /// Safely executes a task as fire-and-forget, handling errors gracefully.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="onError">Optional error handler callback.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on captured context.</param>
    public static async void SafeFireAndForget(
        this Task task,
        Action<Exception>? onError = null,
        bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected, ignore
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}
