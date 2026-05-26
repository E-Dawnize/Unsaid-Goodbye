using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using UnityEngine;

namespace Gameplay.Interactions
{
    public class SequentialBeatActivator : StrictLifecycleMonoBehaviour
    {
        [Inject] private IEventCenter _events;

        [SerializeField] private string[] _beatIds;
        [SerializeField] private GameObject[] _targetsToActivate;

        private int _currentIndex;

        protected override void OnInitialize()
        {
            if (_targetsToActivate == null) return;
            foreach (var target in _targetsToActivate)
            {
                if (target != null)
                    target.SetActive(false);
            }
        }

        protected override void OnStartExternal()
        {
            _events?.Subscribe<StoryBeatCompletedEvent>(OnStoryBeatCompleted);
            SyncToCompletedBeats();
        }

        protected override void OnShutdown()
        {
            _events?.Unsubscribe<StoryBeatCompletedEvent>(OnStoryBeatCompleted);
        }

        private void OnStoryBeatCompleted(StoryBeatCompletedEvent evt)
        {
            if (_beatIds == null || _targetsToActivate == null) return;
            if (_currentIndex >= _beatIds.Length) return;
            if (evt.StoryBeatID != _beatIds[_currentIndex]) return;

            var target = _targetsToActivate[_currentIndex];
            if (target != null)
                target.SetActive(true);

            _currentIndex++;
        }

        private void SyncToCompletedBeats()
        {
            if (_beatIds == null || _targetsToActivate == null) return;

            var allInteractables = FindObjectsByType<InteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            while (_currentIndex < _beatIds.Length)
            {
                var beatId = _beatIds[_currentIndex];
                var completed = false;
                foreach (var obj in allInteractables)
                {
                    if (obj.Def != null && obj.Def.name == beatId && !obj.CanInteract)
                    {
                        completed = true;
                        break;
                    }
                }

                if (!completed) break;

                var target = _targetsToActivate[_currentIndex];
                if (target != null)
                    target.SetActive(true);
                _currentIndex++;
            }
        }
    }
}
