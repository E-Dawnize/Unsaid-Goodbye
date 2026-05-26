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
        [SerializeField] private GameObject[] _targetsToActivate;
        [SerializeField] private bool _hideTargetsOnInitialize = true;

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
            if (string.IsNullOrWhiteSpace(_requiredBeatId) || evt.StoryBeatID != _requiredBeatId)
                return;

            if (_targetsToActivate == null)
                return;

            foreach (var target in _targetsToActivate)
            {
                if (target != null)
                    target.SetActive(true);
            }
        }
    }
}
