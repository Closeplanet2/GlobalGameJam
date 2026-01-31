using System;
using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EventHandlerAttribute : Attribute
    {
        public string Channel { get; set; } = string.Empty;
        public EventPrioirty Priority { get; set; } = EventPrioirty.NORMAL;
        public bool IgnoreCancelled { get; set; } = false;
        public bool DebugCalls { get; set; } = false;
    }
}