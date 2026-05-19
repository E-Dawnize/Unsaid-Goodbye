using System.Collections.Generic;
using Core.Architecture;
using Core.Architecture.Interfaces;
using UnityEngine;

namespace Core.Boot
{
    public class UpdateRunner:MonoBehaviour
    {
        private readonly List<ITickable> _tickables = new List<ITickable>();
        private readonly object _lock = new object();

        private void Awake()
        {
            LifecycleRegistry.OnTickableRegistered += OnTickableRegistered;
            LifecycleRegistry.OnTickableUnregistered += OnTickableUnregistered;
        }

        private void OnTickableRegistered(ITickable tickable)
        {
            Register(tickable);
        }

        private void OnTickableUnregistered(ITickable tickable)
        {
            Unregister(tickable);
        }

        public void Register(ITickable tickable)
        {
            lock (_lock)
            {
                if (!_tickables.Contains(tickable))
                    _tickables.Add(tickable);
            }
        }

        public void Unregister(ITickable tickable)
        {
            lock (_lock)
            {
                _tickables.Remove(tickable);
            }
        }
        private void Update()
        {
            float deltaTime = Time.deltaTime;

            lock (_lock)
            {
                foreach (var tickable in _tickables.ToArray())
                {
                    try
                    {
                        tickable.Tick(deltaTime);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Tick error in {tickable.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            LifecycleRegistry.OnTickableRegistered -= OnTickableRegistered;
            LifecycleRegistry.OnTickableUnregistered -= OnTickableUnregistered;
            lock (_lock)
            {
                _tickables.Clear();
            }
        }

        public int GetTickableCount()
        {
            lock (_lock) return _tickables.Count;
        }
    }
}