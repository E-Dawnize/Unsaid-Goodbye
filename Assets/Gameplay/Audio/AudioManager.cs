using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Audio
{
    public class AudioManager : IAudioManager, System.IDisposable
    {
        private const int SfxSourceCount = 4;
        private const string BgmLabel = "BGM";
        private const string SfxLabel = "SFX";

        private GameObject _root;
        private AudioSource _bgmSource;
        private readonly AudioSource[] _sfxSources = new AudioSource[SfxSourceCount];
        private int _sfxIndex;

        private readonly Dictionary<string, AudioClip> _clipCache = new();

        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                if (_bgmSource != null)
                    _bgmSource.volume = _bgmVolume;
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public AudioManager()
        {
            CreateRoot();
        }

        private void CreateRoot()
        {
            _root = new GameObject("AudioRoot");
            Object.DontDestroyOnLoad(_root);

            _bgmSource = _root.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.volume = _bgmVolume;

            for (var i = 0; i < SfxSourceCount; i++)
            {
                var source = _root.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = _sfxVolume;
                _sfxSources[i] = source;
            }
        }

        public async void PlayBgm(string clipAddress)
        {
            var clip = await LoadClip(clipAddress, BgmLabel);
            if (clip == null) return;

            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public async void StopBgm(float fadeDuration = 1f)
        {
            if (fadeDuration <= 0f)
            {
                _bgmSource.Stop();
                return;
            }

            var startVolume = _bgmSource.volume;
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                await Task.Yield();
            }

            _bgmSource.Stop();
            _bgmSource.volume = _bgmVolume;
        }

        public async Task PlaySfx(string clipAddress)
        {
            var clip = await LoadClip(clipAddress, SfxLabel);
            if (clip == null) return;

            var source = _sfxSources[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % SfxSourceCount;

            Debug.Log($"[Audio] Playing SFX: {clipAddress} vol={_sfxVolume}");
            source.PlayOneShot(clip, _sfxVolume);
        }

        private async Task<AudioClip> LoadClip(string address, string label)
        {
            if (_clipCache.TryGetValue(address, out var cached))
                return cached;

            var handle = Addressables.LoadAssetAsync<AudioClip>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _clipCache[address] = handle.Result;
                return handle.Result;
            }

            Debug.LogWarning($"[Audio] Failed to load clip: {address}");
            return null;
        }

        public void Dispose()
        {
            _bgmSource?.Stop();

            foreach (var clip in _clipCache.Values)
            {
                if (clip != null)
                    Addressables.Release(clip);
            }

            _clipCache.Clear();

            if (_root != null)
                Object.Destroy(_root);
        }
    }
}
