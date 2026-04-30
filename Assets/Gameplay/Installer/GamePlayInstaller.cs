using Core.Architecture;
using Core.DI;
using Gameplay.Interfaces;
using Gameplay.SceneFlow;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Gameplay.Installer
{
    [CreateAssetMenu(fileName = "GamePlayInstaller", menuName = "Boot/GamePlayInstaller")]
    public class GamePlayInstaller : InstallerAsset
    {
        public override void Register(DIContainer container)
        {
            // Controller
            container.RegisterSingleton<IGameFlowController, GameFlowController>();

            // Model — ScriptableObject 资产，通过 Addressables 加载后注册为单例
            container.RegisterSingleton<GameFlowModel>(sp =>
            {
                var handle = Addressables.LoadAssetAsync<GameFlowModel>("Configs/GameFlowModel.asset");
                var model = handle.WaitForCompletion();
                return model;
            });
        }
    }
}
