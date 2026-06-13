using Input.Manager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Input.UI
{
    public static class RuntimeJoystickInstaller
    {
        private const string RootName = "MobileJoystick";
        private const string PrefabKey = "UI/Joystick";
        private const string KnobName = "N4";
        public static float JoystickRadius = 1f;
        public static float JoystickScale = 0.35f;
        public static float JoystickAlpha = 0.5f;
        public static Vector3 JoystickPosition = new(-7.29f, -4.82f, 0f);

        public static async void Ensure()
        {
            if (!Application.isPlaying)
                return;

#if !UNITY_EDITOR
            if (!Application.isMobilePlatform)
                return;
#endif

            var existing = Object.FindObjectOfType<WorldSpaceJoystick>(true);
            if (!ShouldShowInActiveScene())
            {
                if (existing != null)
                    existing.gameObject.SetActive(false);
                VirtualJoystickInput.SetDirection(Vector2.zero);
                return;
            }

            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(PrefabKey);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[Joystick] Failed to load prefab: {PrefabKey}");
                return;
            }

            var go = Object.Instantiate(handle.Result);
            go.name = RootName;
            Object.DontDestroyOnLoad(go);

            // 设置层级高于场景物体
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = "Popup";
                sr.sortingOrder = 900;
            }

            var knob = go.transform.Find(KnobName);
            if (knob == null)
            {
                Debug.LogWarning($"[Joystick] Knob '{KnobName}' not found in prefab");
                Object.Destroy(go);
                return;
            }

            // 旋钮必须渲染在底圈之上（同 SortingLayer 按 order 排序）
            var knobSr = knob.GetComponent<SpriteRenderer>();
            if (knobSr != null) knobSr.sortingOrder = 901;

            var joystick = go.AddComponent<WorldSpaceJoystick>();
            go.transform.position = JoystickPosition;
            go.transform.localScale = Vector3.one * JoystickScale;
            SetAlpha(go, JoystickAlpha);

            joystick.Init(knob, JoystickRadius);
        }

        private static void SetAlpha(GameObject go, float alpha)
        {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        private static bool ShouldShowInActiveScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName != "Start"
                && sceneName != "Start New"
                && sceneName != "SampleScene";
        }
    }
}
