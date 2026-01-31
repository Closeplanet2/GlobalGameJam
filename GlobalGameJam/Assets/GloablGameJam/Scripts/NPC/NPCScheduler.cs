using System;
using System.Collections.Generic;
using System.Linq;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCScheduler : MonoBehaviour, INPCScheduler
    {
        public static readonly List<NPCScheduler> All = new();

        private ICharacterManager _characterManager;
        private readonly Stack<SchedulerFrame> _interruptStack = new();

        private List<NPCScheduleItem> _scheduleItems = new();
        private float _tickTimer;
        private uint _clock;
        private uint _maxClock;

        private NPCScheduleItem _active;
        private uint _activeEnd;
        private bool _activeStarted;

        [Header("Schedule Settings")]
        [SerializeField] private bool _loopSchedule = true;

        private void Awake()
        {
            _scheduleItems = GetComponentsInChildren<NPCScheduleItem>(includeInactive: true).ToList();
            IRebuildScheduleCache();
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void IHandleCharacterComponent()
        {
            if (_characterManager == null) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < NPCSchedulerStatic.TICK_INTERVALS_SECOND) return;
            _tickTimer = 0f;
            Tick();
        }

        public void IRebuildScheduleCache()
        {
            _scheduleItems = _scheduleItems.Where(x => x != null && x.IncludeInLoop).ToList();
            _maxClock = 0;
            for (var i = 0; i < _scheduleItems.Count; i++)
            {
                var item = _scheduleItems[i];
                var end = item.TriggerTime + Mathf.Max(1u, item.DurationTicks);
                if (end > _maxClock) _maxClock = (uint) end;
            }
        }

        public bool ITryInterruptInvestigate(Vector3 point, bool replaceCurrent = false)
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

        public void IInterrupt(NPCScheduleItem interruptItem, bool replaceCurrent = false)
        {
            if (_characterManager == null) return;
            if (interruptItem == null) return;

            _interruptStack.Push(new SchedulerFrame
            {
                active = _active,
                activeEnd = _activeEnd,
                activeStarted = _activeStarted,
                clock = _clock
            });

            if (replaceCurrent && _active != null)  EndActive();
            _active = interruptItem;
            _activeEnd = _clock + Math.Max(1u, interruptItem.DurationTicks);
            _activeStarted = false;
        }

        public void IResumeFromInterrupt()
        {
            if (_interruptStack.Count == 0) return;
            if (_active != null) EndActive();
            var frame = _interruptStack.Pop();
            _active = frame.active;
            _activeEnd = frame.activeEnd;
            _activeStarted = frame.activeStarted;
            _clock = frame.clock;
        }

        private void Tick()
        {
            if (_interruptStack.Count == 0 && _loopSchedule && _maxClock > 0 && _clock >= _maxClock) _clock = 0;

            if (_active != null)
            {
                TickActive();
                return;
            }

            var next = FindItemStartingAt(_clock);
            if (next != null)
            {
                StartActive(next);
                TickActive(); 
                return;
            }

            _clock++;
        }

        private NPCScheduleItem FindItemStartingAt(uint clock)
        {
            NPCScheduleItem best = null;
            var bestPriority = int.MinValue;
            for (var i = 0; i < _scheduleItems.Count; i++)
            {
                var item = _scheduleItems[i];
                if (item == null) continue;
                if (item.TriggerTime != clock) continue;

                if (best == null || item.Priority > bestPriority)
                {
                    best = item;
                    bestPriority = item.Priority;
                }
            }
            return best;
        }

        private void StartActive(NPCScheduleItem item)
        {
            _active = item;
            _activeEnd = _clock + Math.Max(1u, item.DurationTicks);
            _activeStarted = false;
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
            var complete = _active.IsComplete(_characterManager, _clock);
            var timedOut = _clock >= _activeEnd;
            if (complete || timedOut)
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
            _active.OnEnd(_characterManager, _clock);
            _active = null;
            _activeStarted = false;
            if (_interruptStack.Count > 0) IResumeFromInterrupt();
        }

        private struct SchedulerFrame
        {
            public NPCScheduleItem active;
            public uint activeEnd;
            public bool activeStarted;
            public uint clock;
        }
    }
}