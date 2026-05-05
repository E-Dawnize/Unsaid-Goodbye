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
            // 订阅 LifecycleRegistry 的动态 ITickable 回调
            LifecycleRegistry.OnTickableRegistered += OnTickableRegistered;
        }

        private void OnTickableRegistered(ITickable tickable)
        {
            Register(tickable);
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
                foreach (var tickable in _tickables.ToArray()) // 复制避免迭代修改
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