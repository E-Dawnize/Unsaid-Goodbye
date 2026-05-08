using UnityEngine;

using Core.Architecture;

using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Core.Boot
{
    public static class ProjectBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ProjectContext.ResetStaticState();
            LifecycleRegistry.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying) return;
            Debug.Log("Boot");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ProjectContext.Ensure();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SceneBuild()
        {
            FixEventSystemInputModules();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FixEventSystemInputModules();
        }

        private static void FixEventSystemInputModules()
        {
            var eventSystems = Object.FindObjectsOfType<EventSystem>(true);
            foreach (var eventSystem in eventSystems)
            {
                if (eventSystem.TryGetComponent<StandaloneInputModule>(out var oldInputModule))
                {
                    oldInputModule.enabled = false;
                    Object.Destroy(oldInputModule);
                }

                if (!eventSystem.TryGetComponent<InputSystemUIInputModule>(out _))
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
