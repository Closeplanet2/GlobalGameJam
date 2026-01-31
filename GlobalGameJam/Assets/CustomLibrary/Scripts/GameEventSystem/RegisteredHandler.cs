using System;
using System.Reflection;
using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{
    public class RegisteredHandler
    {
        public IEventListener Listener { get; }
        public MethodInfo Method { get; }
        public Type ParameterType { get; }
        public EventHandlerAttribute Attr { get; }

        public RegisteredHandler(IEventListener listener, MethodInfo method, Type parameterType, EventHandlerAttribute attr)
        {
            Listener = listener;
            Method = method;
            ParameterType = parameterType;
            Attr = attr;
        }
    }
}