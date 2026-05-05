using Core.Architecture;
using Core.DI;
using MVVM.ViewModel;
using UnityEngine;
using UnityEngine.UI;

namespace MVVM.View
{
    /// <summary>
    /// 主菜单 View — 集中管理 Start 场景的所有 UI 绑定
    /// 挂到 Canvas 根节点上，View 只做两件事：持有 UI 引用 + 连接输入到 ViewModel
    /// </summary>
    public class MainMenuView : StrictLifecycleMonoBehaviour
    {
        [Inject] private MainMenuViewModel _viewModel;

        [Header("UI 引用")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Slider _volumeSlider;

        protected override void OnStartExternal()
        {
            // 此时所有组件的 Initialize 已完成，VM 已通过 [Inject] 注入，安全绑定
            if (_startGameButton != null)
                _startGameButton.onClick.AddListener(OnStartGameClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

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

            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        private void OnStartGameClicked()  => _viewModel.StartGameCommand.Execute(null);
        private void OnSettingsClicked()   => Debug.Log("[MainMenuView] Settings — 待实现");
        private void OnVolumeChanged(float v) => _viewModel.Volume = v;
    }
}
