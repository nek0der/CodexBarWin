using CodexBarWin.Helpers;
using FluentAssertions;

namespace CodexBarWin.Tests.Unit.Helpers;

[TestClass]
public class AsyncHelperTests
{
    [TestMethod]
    public async Task SafeFireAndForget_SuccessfulTask_Completes()
    {
        // Arrange
        var completed = false;
        var task = Task.Run(() => completed = true);

        // Act
        task.SafeFireAndForget();
        await Task.Delay(100); // Give time for task to complete

        // Assert
        completed.Should().BeTrue();
    }

    [TestMethod]
    public async Task SafeFireAndForget_TaskThrows_CallsOnError()
    {
        // Arrange
        Exception? caughtException = null;
        var expectedException = new InvalidOperationException("Test error");
        var task = Task.FromException(expectedException);

        // Act
        task.SafeFireAndForget(ex => caughtException = ex);
        await Task.Delay(100); // Give time for handler to be called

        // Assert
        caughtException.Should().Be(expectedException);
    }

    [TestMethod]
    public async Task SafeFireAndForget_OperationCanceled_DoesNotCallOnError()
    {
        // Arrange
        Exception? caughtException = null;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromCanceled(cts.Token);

        // Act
        task.SafeFireAndForget(ex => caughtException = ex);
        await Task.Delay(100);

        // Assert
        caughtException.Should().BeNull();
    }

    [TestMethod]
    public async Task SafeFireAndForget_NoOnError_DoesNotThrow()
    {
        // Arrange
        var task = Task.FromException(new InvalidOperationException());

        // Act
        var action = () =>
        {
            task.SafeFireAndForget(); // No onError handler
            return Task.CompletedTask;
        };

        // Assert
        await action.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task SafeFireAndForget_AsyncTask_Completes()
    {
        // Arrange
        var result = 0;
        var task = Task.Run(async () =>
        {
            await Task.Delay(50);
            result = 42;
        });

        // Act
        task.SafeFireAndForget();
        await Task.Delay(200);

        // Assert
        result.Should().Be(42);
    }
}
