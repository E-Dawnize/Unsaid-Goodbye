using Core.Architecture;
using Input;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MVVM.View
{
    [RequireComponent(typeof(Collider2D))]
    public class StartNewSceneStartTrigger : StrictLifecycleMonoBehaviour
    {
        [SerializeField] private string _sceneToLoad = "Scenes/2.SurfaceWorld_LivingRoom";
        [SerializeField] private bool _startOnce = true;

        private bool _started;
        private AsyncOperationHandle<SceneInstance> _sceneHandle;
        private Collider2D _collider;
        private Camera _cachedCamera;

        protected override void OnInitialize()
        {
            _collider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// 每帧通过 PointerInputHelper 轮询点击，替代旧版 OnMouseDown。
        /// 内部处理 Mouse → Pointer → Touchscreen 设备优先级及 H5/WebGL 回退。
        /// </summary>
        protected override void Tick(float dt)
        {
            if (_started && _startOnce) return;

            if (_cachedCamera == null || !_cachedCamera.gameObject.activeInHierarchy)
                _cachedCamera = Camera.main;
            if (_cachedCamera == null) return;

            if (!PointerInputHelper.WasClickedThisFrame) return;

            var worldPos = (Vector2)_cachedCamera.ScreenToWorldPoint(PointerInputHelper.ScreenPosition);
            if (_collider != null && _collider.OverlapPoint(worldPos))
            {
                StartGame();
            }
        }

        public async void StartGame()
        {
            if (_started && _startOnce)
                return;

            if (string.IsNullOrWhiteSpace(_sceneToLoad) || !_sceneToLoad.StartsWith("Scenes/"))
            {
                Debug.LogError($"[StartNew] Invalid scene address '{_sceneToLoad}' on '{gameObject.name}'. Expected format: Scenes/XXX", this);
                return;
            }

            _started = true;
            Debug.Log($"[StartNew] Loading scene: {_sceneToLoad}");

            _sceneHandle = Addressables.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Single);
            await _sceneHandle.Task;

            if (_sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                _started = false;
                Debug.LogError($"[StartNew] Scene load failed: {_sceneToLoad}", this);
            }
        }
    }
}
