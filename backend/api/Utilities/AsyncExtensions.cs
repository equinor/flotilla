using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Api.Utilities
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// Allows a cancellation token to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct)
        {
            // return our special awaiter
            return new CancellationTokenAwaiter { _cancellationToken = ct };
        }

        /// <summary>
        /// The awaiter for cancellation tokens.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct CancellationTokenAwaiter(CancellationToken cancellationToken)
            : INotifyCompletion,
                ICriticalNotifyCompletion
        {
            internal CancellationToken _cancellationToken = cancellationToken;

            public readonly object GetResult()
            {
                // this is called by compiler generated methods when the
                // task has completed. Instead of returning a result, we
                // just throw an exception.
                if (IsCompleted)
                    throw new OperationCanceledException();
                throw new InvalidOperationException(
                    "The cancellation token has not yet been cancelled."
                );
            }

            // called by compiler generated/.net internals to check
            // if the task has completed.
            public readonly bool IsCompleted => _cancellationToken.IsCancellationRequested;

            // The compiler will generate stuff that hooks in
            // here. We hook those methods directly into the
            // cancellation token.
            public readonly void OnCompleted(Action continuation) =>
                _cancellationToken.Register(continuation);

            public readonly void UnsafeOnCompleted(Action continuation) =>
                _cancellationToken.Register(continuation);
        }
    }
}
