using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gameplay.SceneFlow;
using UnityEngine;

namespace Gameplay.SO
{
    /// <summary>
    /// GameFlow 的 MVVM Model 层 — 纯数据 ScriptableObject，可作为 PropertyBinding 绑定源
    /// Controller 写入，View 只读绑定
    /// </summary>
    [CreateAssetMenu(menuName = "UnsaidGoodbye/GameFlowModel")]
    public class GameFlowModel : ScriptableObject, INotifyPropertyChanged
    {
        [field: SerializeField] public GamePhase CurrentPhase        { get; private set; }
        [field: SerializeField] public bool      IsTransitioning      { get; private set; }
        [field: SerializeField] public int       CompletedBeatCount   { get; private set; }
        [field: SerializeField] public int       TotalBeatCount        { get; private set; }

        /// <summary>阶段进度 0~1，可直接绑定到 Slider.value</summary>
        public float PhaseProgress => TotalBeatCount > 0
            ? (float)CompletedBeatCount / TotalBeatCount
            : 0f;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Controller 调用：Beat 进度更新</summary>
        public void ApplyBeatProgress(int completed, int total)
        {
            CompletedBeatCount = completed;
            TotalBeatCount = total;
            OnChanged(nameof(CompletedBeatCount));
            OnChanged(nameof(TotalBeatCount));
            OnChanged(nameof(PhaseProgress));
        }

        /// <summary>Controller 调用：阶段切换完成</summary>
        public void ApplyPhase(GamePhase phase, int completed, int total)
        {
            CurrentPhase = phase;
            CompletedBeatCount = completed;
            TotalBeatCount = total;
            IsTransitioning = false;
            OnChanged(string.Empty);
        }

        /// <summary>Controller 调用：转场中</summary>
        public void SetTransitioning(bool value)
        {
            IsTransitioning = value;
            OnChanged(nameof(IsTransitioning));
        }

        private void OnChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop ?? string.Empty));
    }
}
