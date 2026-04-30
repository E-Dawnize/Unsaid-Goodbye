using System;
using Core.Architecture.Interfaces;
using Gameplay.SceneFlow;
using Gameplay.SO;

namespace Gameplay.Interfaces
{
    public interface IGameFlowController:IInitializable,IDisposable
    {
        GameSaveDataRuntime GameData{get;}
        public GamePhase CurrentPhase { get; }
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<GamePhase> OnPhaseComplete;
    }
}