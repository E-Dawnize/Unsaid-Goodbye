using System;
using Core.Architecture.Interfaces;
using Core.DI;
using Gameplay.Audio;
using Gameplay.Interfaces;
using Gameplay.Pause;
using Input.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Settings
{
    public class SettingsManager : IInitializable, IDisposable
    {
        [Inject] private IPauseMenu _pauseMenu;
        [Inject] private IAudioManager _audio;
        [Inject] private IPlayerManager _player;

        private const string OperateKey = "Setting/Operate";
        private const string OperatePCKey = "Setting/Operate-PC";
        private const string ScreenKey = "Setting/Screen";
        private const string ScreenPCKey = "Setting/Screen-PC";
        private const string SoundKey = "Setting/Sound";

        private GameObject _currentPanel;
        private bool _muted;
        private float _savedVolume = 0.5f;
        private WorldSpaceSlider _masterSlider;

        /// <summary>设置面板打开时屏蔽暂停菜单点击</summary>
        public static bool IsSettingsOpen { get; private set; }

        public void Initialize()
        {
            LoadPersistedSettings();
        }
        public void Dispose() { ClosePanel(); }

        private void LoadPersistedSettings()
        {
            _savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
            _muted = PlayerPrefs.GetInt("Muted", 0) == 1;

            if (_audio != null)
            {
                var vol = _muted ? 0f : _savedVolume;
                _audio.BgmVolume = vol;
                _audio.SfxVolume = vol;
            }

            if (_player != null)
                _player.SetSpeedMultiplier(PlayerPrefs.GetFloat("MoveSpeedMul", 1f));

            RuntimeJoystickInstaller.JoystickScale = PlayerPrefs.GetFloat("JoystickScale", 0.35f);
            RuntimeJoystickInstaller.JoystickAlpha = PlayerPrefs.GetFloat("JoystickAlpha", 0.5f);
        }

        // ==================== Open Panels ====================

        public void OpenOperate()
        {
            var key = Application.isMobilePlatform ? OperateKey : OperatePCKey;
            OpenPanel(key, SetupOperate);
        }

        public void OpenScreen()
        {
            var key = Application.isMobilePlatform ? ScreenKey : ScreenPCKey;
            OpenPanel(key, SetupScreen);
        }

        public void OpenSound()
        {
            OpenPanel(SoundKey, SetupSound);
        }

        // ==================== Panel Lifecycle ====================

        private async void OpenPanel(string key, Action<GameObject> setup)
        {
            ClosePanel();

            var handle = Addressables.InstantiateAsync(key);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) return;

            _currentPanel = handle.Result;
            UnityEngine.Object.DontDestroyOnLoad(_currentPanel);
            _currentPanel.SetActive(true);
            IsSettingsOpen = true;

            var cam = Camera.main;
            if (cam != null)
                _currentPanel.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 8f);

            // 设置 all sprites sorting order 高于暂停菜单 (106+)
            foreach (var sr in _currentPanel.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = "Popup";
                sr.sortingOrder = Mathf.Max(sr.sortingOrder, 0) + 200;
            }

            setup(_currentPanel);

            // Return 按钮（递归查找，不限层级）
            var returnBtn = FindDeep(_currentPanel.transform, "return");
            if (returnBtn != null)
            {
                SetupButtonDirect(returnBtn.gameObject, ClosePanel);
                Debug.Log($"[SettingsManager] Return button setup complete");
            }
        }

        private void ClosePanel()
        {
            if (_currentPanel != null)
            {
                UnityEngine.Object.Destroy(_currentPanel);
                _currentPanel = null;
            }
            IsSettingsOpen = false;
        }

        // ==================== Operate Setup ====================

        private void SetupOperate(GameObject panel)
        {
            // C3: 移动速度 0.5~2
            var speedMul = PlayerPrefs.GetFloat("MoveSpeedMul", 1f);
            SetupSlider(panel, "C3", Mathf.InverseLerp(0.5f, 2f, speedMul), v =>
            {
                var speed = Mathf.Lerp(0.5f, 2f, v);
                PlayerPrefs.SetFloat("MoveSpeedMul", speed);
                if (_player != null) _player.SetSpeedMultiplier(speed);
            });

            if (Application.isMobilePlatform)
            {
                // B3: 摇杆透明度 0.2~1
                var alpha = PlayerPrefs.GetFloat("JoystickAlpha", 0.5f);
                SetupSlider(panel, "B3", Mathf.InverseLerp(0.2f, 1f, alpha), v =>
                {
                    var a = Mathf.Lerp(0.2f, 1f, v);
                    PlayerPrefs.SetFloat("JoystickAlpha", a);
                    RuntimeJoystickInstaller.JoystickAlpha = a;
                    ApplyJoystickAlpha();
                });

                // A3: 摇杆大小 0.5~1.2
                var scale = PlayerPrefs.GetFloat("JoystickScale", 0.35f);
                SetupSlider(panel, "A3", Mathf.InverseLerp(0.5f, 1.2f, scale), v =>
                {
                    var s = Mathf.Lerp(0.5f, 1.2f, v);
                    PlayerPrefs.SetFloat("JoystickScale", s);
                    RuntimeJoystickInstaller.JoystickScale = s;
                    ApplyJoystickScale();
                });
            }
        }

        // ==================== Screen Setup ====================

        private void SetupScreen(GameObject panel)
        {
            SetupButton(panel, "30", () => Application.targetFrameRate = 30);
            SetupButton(panel, "60", () => Application.targetFrameRate = 60);

            if (!Application.isMobilePlatform)
            {
                SetupButton(panel, "Window", () => Screen.fullScreen = false);
                SetupButton(panel, "Full", () => Screen.fullScreen = true);
            }
        }

        // ==================== Sound Setup ====================

        private void SetupSound(GameObject panel)
        {
            var sfxVol = PlayerPrefs.GetFloat("SfxVolume", 0.5f);
            var bgmVol = PlayerPrefs.GetFloat("BgmVolume", 0.5f);
            var masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.5f);

            SetupSlider(panel, "C3", sfxVol, v =>
            {
                PlayerPrefs.SetFloat("SfxVolume", v);
                if (_audio != null) _audio.SfxVolume = v;
            });

            SetupSlider(panel, "B3", bgmVol, v =>
            {
                PlayerPrefs.SetFloat("BgmVolume", v);
                if (_audio != null) _audio.BgmVolume = v;
            });

            _masterSlider = SetupSlider(panel, "A3", masterVol, v =>
            {
                PlayerPrefs.SetFloat("MasterVolume", v);
                if (_audio != null) { _audio.BgmVolume = v; _audio.SfxVolume = v; }
            });

            SetupButton(panel, "Open ", () =>
            {
                if (!_muted) return;
                _muted = false;
                PlayerPrefs.SetInt("Muted", 0);
                _masterSlider?.SetValue(_savedVolume);
            });

            SetupButton(panel, "Off", () =>
            {
                if (_muted) return;
                _muted = true;
                _savedVolume = _masterSlider != null ? _masterSlider.Value : 0.5f;
                PlayerPrefs.SetInt("Muted", 1);
                _masterSlider?.SetValue(0f);
            });
        }

        // ==================== Helpers ====================

        private static WorldSpaceSlider SetupSlider(GameObject panel, string childName, float defaultValue, Action<float> onChange)
        {
            var child = panel.transform.Find(childName);
            if (child == null) return null;

            var slider = child.gameObject.AddComponent<WorldSpaceSlider>();
            slider.InitValue(defaultValue);
            slider.OnValueChanged += onChange;
            return slider;
        }

        private static void SetupButton(GameObject panel, string childName, Action onClick)
        {
            var child = panel.transform.Find(childName);
            if (child == null) return;
            SetupButtonDirect(child.gameObject, onClick);
        }


        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void SetupButtonDirect(GameObject go, Action onClick)
        {
            var btn = go.AddComponent<SettingsButton>();
            btn.OnClick += onClick;
        }

        private static void ApplyJoystickAlpha()
        {
            var js = UnityEngine.Object.FindObjectOfType<WorldSpaceJoystick>(true);
            if (js != null)
            {
                foreach (var sr in js.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, RuntimeJoystickInstaller.JoystickAlpha);
                }
            }
        }

        private static void ApplyJoystickScale()
        {
            var js = UnityEngine.Object.FindObjectOfType<WorldSpaceJoystick>(true);
            if (js != null)
                js.transform.localScale = Vector3.one * RuntimeJoystickInstaller.JoystickScale;
        }
    }
}
