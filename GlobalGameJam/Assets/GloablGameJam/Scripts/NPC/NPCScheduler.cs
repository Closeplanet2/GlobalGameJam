using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public class NPCScheduler : MonoBehaviour, ICharacterComponent
    {
        private ICharacterManager _characterManager;

        [Header("Schedule")]
        [SerializeField] private List<NPCScheduleItem> scheduleItems = new();
        [SerializeField] private bool loopSchedule = true;

        [Header("Clock")]
        [Tooltip("How many seconds per schedule tick. 1 = real seconds.")] 
        [SerializeField] private float tickIntervalSeconds = 1f;
        
        private float _tickTimer;
        private uint _clock;
        private uint _maxClock;

        private NPCScheduleItem _active;
        private uint _activeStart;
        private uint _activeEnd;
        private bool _activeStarted;        

        private readonly Stack<SchedulerFrame> _interruptStack = new();

        private struct SchedulerFrame
        {
            public NPCScheduleItem active;
            public uint activeStart;
            public uint activeEnd;
            public bool activeStarted;
            public uint clock;
        }

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        private void Awake()
        {
            RebuildScheduleCache();
        }

        private void OnValidate()
        {
            if (tickIntervalSeconds < 0.02f) tickIntervalSeconds = 0.02f;
        }

        public void IHandleCharacterComponent()
        {
            if (_characterManager == null) return;

            // Drive clock at tickIntervalSeconds (default 1 second).
            _tickTimer += Time.deltaTime;
            if (_tickTimer < tickIntervalSeconds) return;
            _tickTimer = 0f;

            TickOnce();
        }

        public void RebuildScheduleCache()
        {
            _maxClock = 0;
            scheduleItems.Sort((a, b) => a.ITriggerTime().CompareTo(b.ITriggerTime()));
            for (int i = 0; i < scheduleItems.Count; i++)
            {
                var item = scheduleItems[i];
                if (item == null) continue;
                var end = item.ITriggerTime() + item.ITaskDuration();
                if (end > _maxClock) _maxClock = end;
            }
        }

        public void Interrupt(NPCScheduleItem interruptItem, bool replaceCurrent = false)
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
                // End current task cleanly before replacing
                SafeEnd(_active);
            }

            _active = interruptItem;
            _activeStart = _clock;
            _activeEnd = _clock + Math.Max(1u, interruptItem.ITaskDuration());
            _activeStarted = false;
        }

        public void ResumeFromInterrupt()
        {
            if (_interruptStack.Count == 0) return;

            if (_active != null)
            {
                SafeEnd(_active);
                _active = null;
                _activeStarted = false;
            }

            var frame = _interruptStack.Pop();
            _active = frame.active;
            _activeStart = frame.activeStart;
            _activeEnd = frame.activeEnd;
            _activeStarted = frame.activeStarted;
            _clock = frame.clock;
        }

        private void EndActiveNow()
        {
            if (_active == null) return;

            SafeEnd(_active);
            _active = null;
            _activeStarted = false;

            // If we were in an interrupt, resume the schedule stack immediately.
            if (_interruptStack.Count > 0)
            {
                ResumeFromInterrupt();
            }
        }

        private void TickOnce()
        {
            if (_interruptStack.Count == 0 && loopSchedule && _maxClock > 0 && _clock > _maxClock)
            {
                if (_active != null)
                {
                    SafeEnd(_active);
                    _active = null;
                    _activeStarted = false;
                }

                _clock = 0;
            }

            if (_active != null)
            {
                if (!_activeStarted)
                {
                    SafeStart(_active);
                    _activeStarted = true;
                }

                SafeTick(_active);
                if (_active.IIsComplete(_characterManager, _clock))
                {
                    EndActiveNow();
                    _clock += 1;
                    return;
                }

                // Duration completion
                if (_clock >= _activeEnd)
                {
                    EndActiveNow();
                    _clock += 1;
                    return;
                }

                _clock += 1;
                return;
            }

            // No active task: select one for this time
            var next = FindItemStartingAt(_clock);
            if (next != null)
            {
                _active = next;
                _activeStart = _clock;
                _activeEnd = _clock + next.ITaskDuration();
                _activeStarted = false;

                // Run start immediately on same tick so it responds instantly
                SafeStart(_active);
                _activeStarted = true;
                SafeTick(_active);
            }

            _clock += 1;
        }

        private NPCScheduleItem FindItemStartingAt(uint clock)
        {
            // scheduleItems are sorted by trigger time
            for (int i = 0; i < scheduleItems.Count; i++)
            {
                var item = scheduleItems[i];
                if (item == null) continue;

                if (item.ITriggerTime() == clock)
                    return item;
            }

            return null;
        }

        private void SafeStart(NPCScheduleItem item)
        {
            try { item.IStartTask(_characterManager, _clock); }
            catch (Exception ex) { Debug.LogException(ex, item); }
        }

        private void SafeTick(NPCScheduleItem item)
        {
            try { item.ITickTask(_characterManager, _clock); }
            catch (Exception ex) { Debug.LogException(ex, item); }
        }

        private void SafeEnd(NPCScheduleItem item)
        {
            try { item.IEndTask(_characterManager, _clock); }
            catch (Exception ex) { Debug.LogException(ex, item); }
        }
    }
}