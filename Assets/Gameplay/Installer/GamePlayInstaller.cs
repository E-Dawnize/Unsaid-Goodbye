using Core.Architecture;
using Gameplay.Interfaces;
using Gameplay.SceneFlow;
using UnityEngine;

namespace Gameplay.Installer
{
    [CreateAssetMenu(fileName = "GamePlayInstaller", menuName = "Boot/GamePlayInstaller")] 
    public class GamePlayInstaller:InstallerAsset
    {
        public override void Register(Core.DI.DIContainer container)
        {
            container.RegisterSingleton<IGameFlowController,GameFlowController>();
        }
    }
}