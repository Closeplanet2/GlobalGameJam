using System;
using System.Collections.Generic;
using System.Linq;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public class NPCScheduler : MonoBehaviour, INPCScheduler
    {
        private ICharacterManager _characterManager;
        private List<NPCScheduleItem> _scheduleItems = new();
        private readonly Stack<SchedulerFrame> _interruptStack = new();
        private float _tickTimer;
        private uint _clock;
        private uint _maxClock;
        private NPCScheduleItem _active;
        private uint _activeStart;
        private uint _activeEnd;
        private bool _activeStarted;

        [Header("Schedule Settings")]
        [SerializeField] private bool loopSchedule = true;

        private void Awake()
        {
            _scheduleItems = GetComponentsInChildren<NPCScheduleItem>().ToList();
            IRebuildScheduleCache();
        }

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void IHandleCharacterComponent()
        {
            _tickTimer += Time.deltaTime;
            if(_tickTimer < NPCSchedulerStatic.TICK_INTERVALS_SECOND) return;
            _tickTimer = 0f;
            TickMe();
        }

        public void IRebuildScheduleCache()
        {
            _maxClock = 0;
            for (int i = 0; i < _scheduleItems.Count; i++)
            {
                var item = _scheduleItems[i];
                if (item == null) continue;
                var end = item.TriggerTime + item.DurationTicks;
                if (end > _maxClock) _maxClock = end;
            }
        }

        public void IInterrupt(NPCScheduleItem interruptItem, bool replaceCurrent = false)
        {
            if (interruptItem == null) return;
            _interruptStack.Push(new SchedulerFrame
            {
                active = _active,
                activeStart = _activeStart,
                activeEnd = _activeEnd,
                activeStarted = _activeStarted,
                clock = _clock
            });
            if (replaceCurrent && _active != null) EndActive(); 
            _active = interruptItem;
            _activeStart = _clock;
            _activeEnd = _clock + Math.Max(1u, interruptItem.DurationTicks);
            _activeStarted = false;
        }

        public void IResumeFromInterrupt()
        {
            if (_interruptStack.Count == 0) return;
            if (_active != null) EndActive();
            var frame = _interruptStack.Pop();
            _active = frame.active;
            _activeStart = frame.activeStart;
            _activeEnd = frame.activeEnd;
            _activeStarted = frame.activeStarted;
            _clock = frame.clock;
        }

        private void TickMe()
        {
            if (loopSchedule && _maxClock > 0 && _clock >= _maxClock)
            {
                _clock = 0;
            }

            if(_active != null)
            {
                HandleExistingActive();
                return;
            }

            var next = FindItemStartingAt();
            if(next != null)
            {
                HandleNextActive(next);
                return;
            }

            _clock++;
        } 

        private NPCScheduleItem FindItemStartingAt()
        {
            NPCScheduleItem best = null;
            var bestPriority = int.MinValue;
            for (int i = 0; i < _scheduleItems.Count; i++)
            {
                var item = _scheduleItems[i];
                if (item == null) continue;
                if (item.TriggerTime != _clock) continue;
                if (best == null || item.Priority > bestPriority)
                {
                    best = item;
                    bestPriority = item.Priority;
                }
            }
            return best;
        }

        private void HandleNextActive(NPCScheduleItem next)
        {
            _active = next;
            _activeStart = _clock;
            _activeEnd = _clock + Math.Max(1u, next.DurationTicks);
            _activeStarted = false;
            _active.OnStart(_characterManager, _clock);
            _activeStarted = true;
            _active.OnTick(_characterManager, _clock);
        }

        private void HandleExistingActive()
        {
            if (!_activeStarted)
            {
                _active.OnStart(_characterManager, _clock);
                _activeStarted = true;
            }

            _active.OnTick(_characterManager, _clock);

            if(_active.IsComplete(_characterManager, _clock))
            {
                EndActive();
                _clock++;
                return;
            }

            if(_clock >= _activeEnd)
            {
                EndActive();
                _clock++;
                return;
            }

            _clock++;
        }

        private void EndActive()
        {
            if(_active == null) return;
            _active.OnEnd(_characterManager, _clock);
            _active = null;
            _activeStarted = false;
        }
    }
}