using System.Collections.Generic;
using Core.Architecture;
using Core.DI;
using Gameplay.Audio;
using Gameplay.Pause;
using MVVM.ViewModel;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace MVVM.View
{
    public class MainMenuView : StrictLifecycleMonoBehaviour
    {
        [Inject] private MainMenuViewModel _viewModel;
        [Inject] private IPauseMenu _pauseMenu;
        [InjectOptional] private IAudioManager _audio;

        private readonly List<MenuButton> _buttons = new();
        private Camera _cam;

        protected override void OnStartExternal()
        {
            _cam = Camera.main;
            _audio?.PlayBgm("BGM/Title");
            BuildButtons();
        }

        protected override void Tick(float dt)
        {
            if (_pauseMenu != null && _pauseMenu.IsOpen) return;

            var mouse = Mouse.current;
            if (mouse == null || _cam == null) return;

            var worldPos = (Vector2)_cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            foreach (var btn in _buttons)
            {
                var hovered = false;
                foreach (var hit in hits)
                {
                    if (hit.gameObject == btn.GameObject)
                    {
                        hovered = true;
                        break;
                    }
                }

                if (hovered && mouse.leftButton.wasPressedThisFrame)
                    btn.OnClick?.Invoke();
            }
        }

        protected override void OnShutdown()
        {
            foreach (var btn in _buttons)
                Object.Destroy(btn.Collider);
            _buttons.Clear();
        }

        private void BuildButtons()
        {
            AddButton("start 副本", OnStartClicked);
            AddButton("continue 副本", OnContinueClicked);
            AddButton("setup 副本", OnSetupClicked);
            AddButton("exit 副本", OnExitClicked);
        }

        private void AddButton(string spriteName, System.Action handler)
        {
            var go = FindSpriteInScene(spriteName);
            if (go == null)
            {
                Debug.LogWarning($"[MainMenuView] Button sprite not found: {spriteName}");
                return;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = go.GetComponent<SpriteRenderer>().sprite.bounds.size;

            _buttons.Add(new MenuButton { GameObject = go, Collider = col, OnClick = handler });
        }

        private static GameObject FindSpriteInScene(string name)
        {
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr.gameObject.name == name)
                    return sr.gameObject;
            }
            return null;
        }

        private void OnStartClicked() => _viewModel.StartGameCommand.Execute(null);
        private void OnContinueClicked() => _viewModel.ContinueGameCommand.Execute(null);
        private void OnSetupClicked() => _pauseMenu?.Toggle();

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("[MainMenuView] Exit clicked — ignored in Unity Editor.");
#else
            Application.Quit();
#endif
        }

        private class MenuButton
        {
            public GameObject GameObject;
            public Collider2D Collider;
            public System.Action OnClick;
        }
    }
}
