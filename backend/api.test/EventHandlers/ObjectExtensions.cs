using System;
using System.Reflection;
namespace Api.Test.EventHandlers
{
    public static class ObjectExtensions
    {
        public static void RaiseEvent<TEventArgs>(this object target, string eventName, TEventArgs eventArgs)
        {
            var targetType = target.GetType();
            const BindingFlags BindingFlags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var fi = targetType.GetField(eventName, BindingFlags);
            if (fi != null)
            {
                if (fi.GetValue(target) is EventHandler<TEventArgs> eventHandler)
                {
                    eventHandler.Invoke(null, eventArgs);
                }
            }
        }
    }
}
