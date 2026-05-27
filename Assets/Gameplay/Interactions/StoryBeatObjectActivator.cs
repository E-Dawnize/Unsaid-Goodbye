using System.Collections.Generic;
using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using UnityEngine;

namespace Gameplay.Interactions
{
    public class StoryBeatObjectActivator : StrictLifecycleMonoBehaviour
    {
        [Inject] private IEventCenter _events;

        [SerializeField] private string _requiredBeatId;
        [SerializeField] private string[] _additionalRequiredBeatIds;
        [SerializeField] private GameObject[] _targetsToActivate;
        [SerializeField] private bool _hideTargetsOnInitialize = true;

        private readonly HashSet<string> _completedBeatIds = new();
        private bool _activated;

        protected override void OnInitialize()
        {
            if (!_hideTargetsOnInitialize || _targetsToActivate == null)
                return;

            foreach (var target in _targetsToActivate)
            {
                if (target != null)
                    target.SetActive(false);
            }
        }

        protected override void OnStartExternal()
        {
            _events?.Subscribe<StoryBeatCompletedEvent>(OnStoryBeatCompleted);
        }

        protected override void OnShutdown()
        {
            _events?.Unsubscribe<StoryBeatCompletedEvent>(OnStoryBeatCompleted);
        }

        private void OnStoryBeatCompleted(StoryBeatCompletedEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.StoryBeatID) || !IsRequiredBeat(evt.StoryBeatID))
                return;

            _completedBeatIds.Add(evt.StoryBeatID);

            if (_activated || !AllRequiredBeatsCompleted())
                return;

            if (_targetsToActivate == null)
                return;

            foreach (var target in _targetsToActivate)
            {
                if (target != null)
                    target.SetActive(true);
            }

            _activated = true;
        }

        private bool IsRequiredBeat(string beatId)
        {
            if (!string.IsNullOrWhiteSpace(_requiredBeatId) && beatId == _requiredBeatId)
                return true;

            if (_additionalRequiredBeatIds == null)
                return false;

            foreach (var requiredBeatId in _additionalRequiredBeatIds)
            {
                if (!string.IsNullOrWhiteSpace(requiredBeatId) && beatId == requiredBeatId)
                    return true;
            }

            return false;
        }

        private bool AllRequiredBeatsCompleted()
        {
            var hasRequirement = false;

            if (!string.IsNullOrWhiteSpace(_requiredBeatId))
            {
                hasRequirement = true;
                if (!_completedBeatIds.Contains(_requiredBeatId))
                    return false;
            }

            if (_additionalRequiredBeatIds != null)
            {
                foreach (var requiredBeatId in _additionalRequiredBeatIds)
                {
                    if (string.IsNullOrWhiteSpace(requiredBeatId))
                        continue;

                    hasRequirement = true;
                    if (!_completedBeatIds.Contains(requiredBeatId))
                        return false;
                }
            }

            return hasRequirement;
        }
    }
}
