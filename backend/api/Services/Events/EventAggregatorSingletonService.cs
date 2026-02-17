namespace Api.Services.Events
{
    /// <summary>
    ///     The event aggregator should be a singleton so that only one exists and every other service can subscribe or publish on the aggregator.
    /// </summary>
    public class EventAggregatorSingletonService
    {
        private readonly Dictionary<Type, List<Action<object>>> _subscribers = [];

        public void Subscribe<T>(Action<T> handler)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = [];
                _subscribers[typeof(T)] = handlers;
            }

            handlers.Add(obj => handler((T)obj));
        }

        public void Publish<T>(T message)
        {
            if (message is null)
                return;

            if (_subscribers.TryGetValue(message.GetType(), out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(message);
                }
            }
        }
    }
}
