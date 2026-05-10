using Core.Architecture;
using Core.DI;
using Gameplay.Audio;
using Gameplay.Dialogue;
using Gameplay.Interfaces;
using Gameplay.Inventory;
using Gameplay.Player;
using Gameplay.Save;
using Gameplay.SceneFlow;
using Gameplay.SO;
using UnityEngine;

namespace Gameplay.Installer
{
    [CreateAssetMenu(fileName = "GamePlayInstaller", menuName = "Boot/GamePlayInstaller")]
    public class GamePlayInstaller : InstallerAsset
    {
        public override void Register(DIContainer container)
        {
            // GameFlow Manager
            container.RegisterSingleton<IGameFlowManager, GameFlowManager>();

            // Player
            container.RegisterSingleton<IPlayerManager, PlayerManager>();

            // Inventory
            container.RegisterSingleton<IInventoryManager, InventoryManager>();

            // Save
            container.RegisterSingleton<ISaveManager, SaveManager>();

            // Audio
            container.RegisterSingleton<IAudioManager, AudioManager>();

            // Dialogue
            container.RegisterSingleton<IDialogueManager, DialogueManager>();

            // Model — 纯运行时类，Manager 在加载存档时填充数据
            container.RegisterSingleton<GameFlowModel>(new GameFlowModel());
        }
    }
}
