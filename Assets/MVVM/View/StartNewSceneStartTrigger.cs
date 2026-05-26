using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MVVM.View
{
    [RequireComponent(typeof(Collider2D))]
    public class StartNewSceneStartTrigger : MonoBehaviour
    {
        [SerializeField] private string _sceneToLoad = "Scenes/2.SurfaceWorld_LivingRoom";
        [SerializeField] private bool _startOnce = true;

        private bool _started;
        private AsyncOperationHandle<SceneInstance> _sceneHandle;

        private void OnMouseDown()
        {
            StartGame();
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
