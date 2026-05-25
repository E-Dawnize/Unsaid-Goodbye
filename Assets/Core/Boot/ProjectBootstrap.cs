using UnityEngine;

using Core.Architecture;
using Input.UI;

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

            // 移动端强制横屏
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ProjectContext.Ensure();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SceneBuild()
        {
            FixEventSystemInputModules();
            RuntimeJoystickInstaller.Ensure();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FixEventSystemInputModules();
            FixCameraFor1080p(scene);
        }

        /// <summary>
        /// 设置正交相机 orthoSize=5.4，使 1920×1080 PPU100 的 Sprite 刚好填满屏幕
        /// </summary>
        private static void FixCameraFor1080p(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null && cam.orthographic)
                {
                    cam.orthographicSize = 5.4f;
                    break;
                }
            }
            RuntimeJoystickInstaller.Ensure();
        }

        private static void FixEventSystemInputModules()
        {
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
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
