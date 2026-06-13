using UnityEngine;

using Core.Architecture;
using Input.UI;

using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
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

            // 开启 EnhancedTouch，确保 WebGL/H5 触摸设备能被 Touchscreen.current 识别
            EnhancedTouchSupport.Enable();

            // 横屏设置（BeforeSceneLoad 阶段先设一次）

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ProjectContext.Ensure();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SceneBuild()
        {
            // 移动端再次强制横屏（AfterSceneLoad 阶段，确保生效）
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                Debug.Log($"[Bootstrap] Orientation forced to Landscape, current={Screen.orientation}");
            }

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
                    cam.backgroundColor = new Color(0.85f, 0.72f, 0.55f); // 淡棕色
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
