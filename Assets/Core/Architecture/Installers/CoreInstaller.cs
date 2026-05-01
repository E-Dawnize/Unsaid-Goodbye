using System;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events;
using Core.Events.EventInterfaces;
using Input.InputInterface;
using Input.Manager;
using MVVM.ViewModel.Factory;
using MVVM.ViewModel.Interfaces;
using UnityEngine;

namespace Core.Architecture.Installers
{
    [CreateAssetMenu(fileName = "CoreInstaller", menuName = "Boot/CoreInstaller")]
    public class CoreInstaller:InstallerAsset
    {
        public override void Register(DIContainer container)
        {
            container.RegisterSingleton<DIContainer>(container);
            // 将容器自身注册为 IServiceProvider，供下层组件按需解析
            container.RegisterSingleton<IServiceProvider>(container);
            // 作用域追踪器 — 全局单例，SceneScopeRunner 动态设置当前场景 Scope
            container.RegisterSingleton<IScopeProvider, ScopeProvider>();
            container.RegisterSingleton<IEventCenter>(new EventManager());
            container.RegisterSingleton<IInitializable,IEventCenter>();
            container.RegisterSingleton<IPlayerInput>(sp =>
            {
                var go = new GameObject("PlayerInputManager");
                DontDestroyOnLoad(go);
                var mgr = go.AddComponent<PlayerInputManager>();
                mgr.Initialize();
                return mgr;
            });
            container.RegisterSingleton<IViewModelFactory, ViewModelFactory>();
        }
    }
}