using Core.Architecture;
using Core.Architecture.Interfaces;
using Core.DI;
using Gameplay.Audio;
using Gameplay.Dialogue;
using Gameplay.Interfaces;
using Gameplay.Inventory;
using Gameplay.Pause;
using Gameplay.Player;
using Gameplay.Save;
using Gameplay.Settings;
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
            // Player
            container.RegisterSingleton<IPlayerManager, PlayerManager>();

            // Inventory — 必须在 GameFlowManager 之前注册
            // 原因：事件处理器按订阅顺序调用，InventoryManager 必须在 DialogueEndedEvent 中
            // 先于 GameFlowManager 执行，确保 GetSaveState() 读取到已更新的 CollectedItems
            var inventoryManager = new InventoryManager();
            container.RegisterSingleton<IInventoryManager>(inventoryManager);
            container.RegisterSingleton<IInitializable>(inventoryManager);

            var inventoryUI = new InventoryUIManager();
            container.RegisterSingleton<InventoryUIManager>(inventoryUI);
            container.RegisterSingleton<IBackpackUI>(inventoryUI);
            container.RegisterSingleton<IInitializable>(inventoryUI);

            // GameFlow Manager — 在 Inventory 之后注册
            container.RegisterSingleton<IGameFlowManager, GameFlowManager>();

            // Save
            container.RegisterSingleton<ISaveManager, SaveManager>();

            // Audio
            container.RegisterSingleton<IAudioManager, AudioManager>();

            // Dialogue
            container.RegisterSingleton<IDialogueManager, DialogueManager>();

            // Pause Menu
            var pauseMenu = new PauseMenuManager();
            container.RegisterSingleton<IPauseMenu>(pauseMenu);
            container.RegisterSingleton<IInitializable>(pauseMenu);
            container.RegisterSingleton<ITickable>(pauseMenu);

            // Settings
            container.RegisterSingleton<SettingsManager, SettingsManager>();
            container.RegisterSingleton<IInitializable, SettingsManager>();

            // Model — 纯运行时类，Manager 在加载存档时填充数据
            container.RegisterSingleton<GameFlowModel>(new GameFlowModel());
        }
    }
}
