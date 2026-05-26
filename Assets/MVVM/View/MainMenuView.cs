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
        [Inject] private IAudioManager _audio;

        private readonly List<MenuButton> _buttons = new();
        private Camera _cam;

        protected override void OnStartExternal()
        {
            _cam = Camera.main;
            _audio?.PlayBgm("BGM/Title");
            BuildButtons();
        }

        private float _bgmCheckTimer;

        protected override void Tick(float dt)
        {
            if (_pauseMenu != null && _pauseMenu.IsOpen) return;

            // BGM 兜底：每 3 秒检查一次，防止被意外停止
            _bgmCheckTimer += dt;
            if (_bgmCheckTimer > 3f)
            {
                _bgmCheckTimer = 0f;
                _audio?.PlayBgm("BGM/Title");
            }

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            if (_cam == null) return;

            // 获取指针位置（优先鼠标，其次触摸）
            Vector2 pointerPos;
            bool isPointerDown;
            bool isPointerClicked;
            if (mouse != null)
            {
                pointerPos = mouse.position.ReadValue();
                isPointerDown = mouse.leftButton.isPressed;
                isPointerClicked = mouse.leftButton.wasPressedThisFrame;
            }
            else if (touch != null && touch.primaryTouch.press.isPressed)
            {
                pointerPos = touch.primaryTouch.position.ReadValue();
                isPointerDown = true;
                isPointerClicked = touch.primaryTouch.press.wasPressedThisFrame;
            }
            else
            {
                return;
            }

            var worldPos = (Vector2)_cam.ScreenToWorldPoint(pointerPos);
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            foreach (var btn in _buttons)
            {
                var isOver = false;
                foreach (var hit in hits)
                {
                    if (hit.gameObject == btn.GameObject)
                    {
                        isOver = true;
                        break;
                    }
                }

                var pressed = isOver && isPointerDown;

                // 颜色：按下 0.6，悬停 0.8，正常 original
                Color targetColor;
                if (pressed)
                    targetColor = new Color(btn.OriginalColor.r * 0.6f, btn.OriginalColor.g * 0.6f, btn.OriginalColor.b * 0.6f, btn.OriginalColor.a);
                else if (isOver)
                    targetColor = new Color(btn.OriginalColor.r * 0.8f, btn.OriginalColor.g * 0.8f, btn.OriginalColor.b * 0.8f, btn.OriginalColor.a);
                else
                    targetColor = btn.OriginalColor;

                if (btn.CurrentColor != targetColor)
                {
                    btn.Sprite.color = targetColor;
                    btn.CurrentColor = targetColor;
                }

                if (isOver && isPointerClicked)
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

            var sr = go.GetComponent<SpriteRenderer>();
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = sr.sprite.bounds.size;

            _buttons.Add(new MenuButton
            {
                GameObject = go, Collider = col, Sprite = sr,
                OriginalColor = sr.color, OnClick = handler
            });
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
            public SpriteRenderer Sprite;
            public Color OriginalColor;
            public Color CurrentColor;
            public System.Action OnClick;
        }
    }
}
