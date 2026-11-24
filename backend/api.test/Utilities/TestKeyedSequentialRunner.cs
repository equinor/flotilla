using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Test.Utilities
{
    public class TestKeyedSequentialRunner
    {
        private readonly KeyedSequentialRunner _runner = new(NullLogger.Instance);

        [Fact]
        public async Task WorkWithSameKeyRunsInEnqueueOrder()
        {
            var releaseFirst = new TaskCompletionSource();
            var order = new List<string>();

            var first = _runner.Enqueue("robot", async () =>
            {
                await releaseFirst.Task;
                order.Add("first");
            });
            var second = _runner.Enqueue("robot", () =>
            {
                order.Add("second");
                return Task.CompletedTask;
            });

            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.False(second.IsCompleted);

            releaseFirst.SetResult();
            await Task.WhenAll(first, second);

            Assert.Equal(["first", "second"], order);
        }

        [Fact]
        public async Task WorkWithDifferentKeysDoesNotWaitForEachOther()
        {
            var releaseFirst = new TaskCompletionSource();

            var first = _runner.Enqueue("robot-1", () => releaseFirst.Task);
            var second = _runner.Enqueue("robot-2", () => Task.CompletedTask);

            await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(first.IsCompleted);

            releaseFirst.SetResult();
            await first;
        }

        [Fact]
        public async Task FailingWorkDoesNotBlockLaterWorkWithSameKey()
        {
            var secondRan = false;

            var first = _runner.Enqueue("robot", () =>
                throw new InvalidOperationException("Handler failed")
            );
            var second = _runner.Enqueue("robot", () =>
            {
                secondRan = true;
                return Task.CompletedTask;
            });

            await Task.WhenAll(first, second);

            Assert.True(secondRan);
        }
    }
}
