using Core.Architecture;
using Core.DI;
using MVVM.ViewModel;
using UnityEngine;
using UnityEngine.UI;

namespace MVVM.View
{
    public class MainMenuView : StrictLifecycleMonoBehaviour
    {
        private const string CoverResourcePath = "UI/Start/CoverUI";
        private const float CoverDesignWidth = 1902f;
        private const float CoverDesignHeight = 1069f;

        [Inject] private MainMenuViewModel _viewModel;

        [Header("UI")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Slider _volumeSlider;

        private bool _coverUiBuilt;

        protected override void OnStartExternal()
        {
            BuildCoverUi();

            if (_startGameButton != null)
                _startGameButton.onClick.AddListener(OnStartGameClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);

            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                _volumeSlider.value = _viewModel.Volume;
            }
        }

        protected override void OnShutdown()
        {
            if (_startGameButton != null)
                _startGameButton.onClick.RemoveListener(OnStartGameClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);

            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);

            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        private void OnStartGameClicked() => _viewModel.StartGameCommand.Execute(null);
        private void OnSettingsClicked() => Debug.Log("[MainMenuView] Settings is not implemented yet.");
        private void OnVolumeChanged(float value) => _viewModel.Volume = value;

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("[MainMenuView] Exit clicked — ignored in Unity Editor.");
#else
            Application.Quit();
#endif
        }

        private void BuildCoverUi()
        {
            if (_coverUiBuilt)
                return;

            if (transform.Find("CoverRoot") != null)
            {
                _coverUiBuilt = true;
                return;
            }

            var coverSprite = Resources.Load<Sprite>(CoverResourcePath);
            if (coverSprite == null)
            {
                Debug.LogWarning($"[MainMenuView] Cover sprite not found at Resources/{CoverResourcePath}");
                return;
            }

            HideLegacyMenuVisuals();

            var coverRoot = new GameObject("CoverRoot");
            coverRoot.transform.SetParent(transform, false);

            var coverRect = coverRoot.AddComponent<RectTransform>();
            coverRect.anchorMin = Vector2.zero;
            coverRect.anchorMax = Vector2.one;
            coverRect.offsetMin = Vector2.zero;
            coverRect.offsetMax = Vector2.zero;

            var coverImage = coverRoot.AddComponent<Image>();
            coverImage.sprite = coverSprite;
            coverImage.color = Color.white;
            coverImage.preserveAspect = true;
            coverImage.raycastTarget = false;

            _startGameButton = CreateHotspot("StartHotspot", coverRoot.transform, 963f, 536f, 317f, 105f);
            _exitButton = CreateHotspot("ExitHotspot", coverRoot.transform, 1309f, 875f, 318f, 105f);

            _coverUiBuilt = true;
        }

        private void HideLegacyMenuVisuals()
        {
            for (var index = 0; index < transform.childCount; index++)
            {
                var child = transform.GetChild(index);
                if (child.name != "CoverRoot")
                    child.gameObject.SetActive(false);
            }
        }

        private static Button CreateHotspot(string name, Transform parent, float left, float top, float width, float height)
        {
            var hotspot = new GameObject(name);
            hotspot.transform.SetParent(parent, false);

            var rect = hotspot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(left / CoverDesignWidth, 1f - (top + height) / CoverDesignHeight);
            rect.anchorMax = new Vector2((left + width) / CoverDesignWidth, 1f - top / CoverDesignHeight);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = hotspot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var button = hotspot.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return button;
        }
    }
}
