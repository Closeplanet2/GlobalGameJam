using System;
using System.Collections.Generic;
using System.Linq;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCScheduler : MonoBehaviour, ICharacterComponent
    {
        public static readonly List<NPCScheduler> All = new();

        private ICharacterManager _characterManager;

        private List<NPCScheduleItem> _loopItems = new();
        private readonly Stack<SchedulerFrame> _interruptStack = new();

        private float _tickTimer;
        private uint _clock;
        private uint _maxClock;

        private NPCScheduleItem _active;
        private uint _activeStart;
        private uint _activeEnd;
        private bool _activeStarted;

        [Header("Schedule")]
        [SerializeField] private bool loopSchedule = true;

        [Header("Debug")]
        [SerializeField] private bool logTransitions = true;

        private struct SchedulerFrame
        {
            public NPCScheduleItem active;
            public uint activeStart;
            public uint activeEnd;
            public bool activeStarted;
            public uint clock;
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Awake()
        {
            RebuildLoopCache();
        }

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void IHandleCharacterComponent()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer < NPCSchedulerStatic.TICK_INTERVALS_SECOND) return;

            _tickTimer = 0f;
            Tick();
        }

        public void RebuildLoopCache()
        {
            var all = GetComponentsInChildren<NPCScheduleItem>(includeInactive: true).ToList();
            _loopItems = all.Where(x => x != null && x.IncludeInLoop).ToList();

            _maxClock = 0;
            for (var i = 0; i < _loopItems.Count; i++)
            {
                var item = _loopItems[i];
                var end = item.TriggerTime + Math.Max(1u, item.DurationTicks);
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

            if (replaceCurrent && _active != null)
            {
                EndActive();
            }

            _active = interruptItem;
            _activeStart = _clock;

            // Interrupt-only items run until IsComplete() returns true.
            _activeEnd = interruptItem is INPCInterruptItem
                ? uint.MaxValue
                : _clock + Math.Max(1u, interruptItem.DurationTicks);

            _activeStarted = false;

            if (logTransitions)
            {
                Debug.Log($"[NPCScheduler] {name} -> INTERRUPT START {_active.GetType().Name}", this as UnityEngine.Object);
            }
        }

        public void IResumeFromInterrupt()
        {
            if (_interruptStack.Count == 0) return;

            if (_active != null)
            {
                EndActive();
            }

            var frame = _interruptStack.Pop();
            _active = frame.active;
            _activeStart = frame.activeStart;
            _activeEnd = frame.activeEnd;
            _activeStarted = frame.activeStarted;
            _clock = frame.clock;

            if (logTransitions && _active != null)
            {
                Debug.Log($"[NPCScheduler] {name} -> RESUME {_active.GetType().Name}", this as UnityEngine.Object);
            }
        }

        public bool ITryInterruptInvestigate(Vector3 point, bool replaceCurrent = true)
        {
            var interrupt = GetComponentInChildren<NPCInvestigatePointInterrupt>(includeInactive: true);
            if (interrupt == null) return false;

            interrupt.SetInvestigatePoint(point);
            IInterrupt(interrupt, replaceCurrent);
            return true;
        }

        public bool ITryInterruptChase(CharacterManager target, bool replaceCurrent = true)
        {
            var chase = GetComponentInChildren<NPCChaseTargetInterrupt>(includeInactive: true);
            if (chase == null) return false;

            chase.SetTarget(target);
            IInterrupt(chase, replaceCurrent);
            return true;
        }

        private void Tick()
        {
            if (_interruptStack.Count == 0 && loopSchedule && _maxClock > 0 && _clock >= _maxClock)
            {
                _clock = 0;
            }

            if (_active != null)
            {
                TickActive();
                return;
            }

            var next = FindNextLoopItemStartingNow();
            if (next != null)
            {
                BeginActive(next);
                TickActive();
                return;
            }

            _clock++;
        }

        private NPCScheduleItem FindNextLoopItemStartingNow()
        {
            NPCScheduleItem best = null;
            var bestPriority = int.MinValue;

            for (var i = 0; i < _loopItems.Count; i++)
            {
                var item = _loopItems[i];
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

        private void BeginActive(NPCScheduleItem item)
        {
            _active = item;
            _activeStart = _clock;
            _activeEnd = _clock + Math.Max(1u, item.DurationTicks);
            _activeStarted = false;

            if (logTransitions)
            {
                Debug.Log($"[NPCScheduler] {name} -> LOOP START {_active.GetType().Name}", this as UnityEngine.Object);
            }
        }

        private void TickActive()
        {
            if (_active == null) return;

            if (!_activeStarted)
            {
                _active.OnStart(_characterManager, _clock);
                _activeStarted = true;
            }

            _active.OnTick(_characterManager, _clock);

            // Interrupt-only: relies on IsComplete.
            if (_active.IsComplete(_characterManager, _clock))
            {
                EndActive();
                _clock++;
                return;
            }

            // Loop items: also time-bound by activeEnd.
            if (_clock >= _activeEnd)
            {
                EndActive();
                _clock++;
                return;
            }

            _clock++;
        }

        private void EndActive()
        {
            if (_active == null) return;

            if (logTransitions)
            {
                Debug.Log($"[NPCScheduler] {name} -> END {_active.GetType().Name}", this as UnityEngine.Object);
            }

            _active.OnEnd(_characterManager, _clock);
            _active = null;
            _activeStarted = false;

            if (_interruptStack.Count > 0)
            {
                IResumeFromInterrupt();
            }
        }
    }
}
