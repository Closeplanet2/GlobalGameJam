using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomLibrary.Scripts.Instance;
using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{    public class GameEventSystem : MonoBehaviourInstance<GameEventSystem>
    {
        private readonly Dictionary<IEventListener, List<RegisteredHandler>> _handlersByListener = new();
        private readonly List<RegisteredHandler> _handlers = new();
        public void RegisterListeners(IEventListener eventListener)
        {
            if (eventListener == null) return;
            if (_handlersByListener.ContainsKey(eventListener)) return;
            var handlers = new List<RegisteredHandler>();
            foreach (var method in eventListener.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var attr = method.GetCustomAttribute<EventHandlerAttribute>(true);
                if (attr == null) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 1) continue;
                var parameterType = parameters[0].ParameterType;
                var handler = new RegisteredHandler(eventListener, method, parameterType, attr);
                handlers.Add(handler);
                _handlers.Add(handler);
            }
            _handlersByListener[eventListener] = handlers;
        }

        public void UnRegisterListeners(IEventListener eventListener)
        {
            if (eventListener == null) return;
            if (!_handlersByListener.TryGetValue(eventListener, out var handlers)) return;
            foreach (var handler in handlers) _handlers.Remove(handler);
            _handlersByListener.Remove(eventListener);
        }

        public void Fire<T>(T eventInstance, string channelName) where T : BaseEvent
        {
            if (eventInstance == null) return;
            var eventType = eventInstance.GetType();
            var matchedHandlers = new List<RegisteredHandler>();
            foreach (var handler in _handlers)
            {
                if (!handler.ParameterType.IsAssignableFrom(eventType)) continue;
                var attr = handler.Attr;
                if (!string.IsNullOrEmpty(attr.Channel) && !string.Equals(attr.Channel, channelName, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (attr.IgnoreCancelled && eventInstance is ICancellableEvent cancellable && cancellable.IsCancelled())  continue;
                matchedHandlers.Add(handler);
            }
            foreach (var handler in matchedHandlers.OrderByDescending(h => h.Attr.Priority))
            {
                if (handler.Attr.DebugCalls) Debug.Log($"[EVENT SYSTEM] Sending event for [{eventType}] to method [{handler.Method.Name}] on [{handler.Listener.GetType().Name}]!");
                handler.Method.Invoke(handler.Listener, new object[] { eventInstance });
            }
        }
    }
}