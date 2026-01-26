using System;
using Microsoft.Extensions.Hosting;

namespace Api.EventHandlers
{
    /// <summary>
    /// A wrapper for Event handlers that properly unsubscribe to events when the background service is disposed
    ///
    /// <para>ALL EVENT HANDLERS SHOULD INHERIT FROM THIS</para>
    ///
    /// </summary>
    public abstract class EventHandlerBase : BackgroundService
    {
        /// <summary>
        /// Subscribe to events
        /// <para>This should be called in the constructor</para>
        /// </summary>
        public abstract void Subscribe();

        /// <summary>
        /// Unsubscribe to events
        /// <para>This is called automatically when the object is disposed</para>
        /// </summary>
        public abstract void Unsubscribe();

        public override void Dispose()
        {
            base.Dispose();
            Unsubscribe();
            GC.SuppressFinalize(this);
        }
    }
}
