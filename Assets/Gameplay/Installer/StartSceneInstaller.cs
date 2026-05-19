using Core.Architecture;
using Core.DI;
using MVVM.ViewModel;
using UnityEngine;

namespace Gameplay.Installer
{
    /// <summary>
    /// Start 场景专用 Installer — 注册场景级 Scoped ViewModel
    /// 需添加到 InstallerConfig 的 sceneInstallers 列表中
    /// </summary>
    [CreateAssetMenu(fileName = "StartSceneInstaller", menuName = "Boot/StartSceneInstaller")]
    public class StartSceneInstaller : InstallerAsset
    {
        public override void Register(DIContainer container)
        {
            // 纯C# ViewModel，Scoped 生命周期 — 场景卸载时自动释放
            container.RegisterScoped<MainMenuViewModel, MainMenuViewModel>();
        }
    }
}
