using System;
using Core.Architecture.Interfaces;
using Gameplay.SceneFlow;
using Gameplay.SO;

namespace Gameplay.Interfaces
{
    public interface IGameFlowManager : IInitializable, IDisposable
    {
        GameSaveDataRuntime GameData{get;}
        public GamePhase CurrentPhase { get; }
        public GamePhaseConfig CurrentConfig { get; }
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<GamePhase> OnPhaseComplete;
        void ConfirmTransition(GamePhase nextPhase);
        GameSaveDataRuntime GetSaveState();
        /// <summary>当游戏场景加载完成时调用，尝试匹配并启动/恢复阶段</summary>
        void OnGameSceneLoaded();
    }
}
