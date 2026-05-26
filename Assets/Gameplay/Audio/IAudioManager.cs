using System.Threading.Tasks;
using UnityEngine;

namespace Gameplay.Audio
{
    public interface IAudioManager
    {
        float BgmVolume { get; set; }
        float SfxVolume { get; set; }

        void PlayBgm(string clipAddress);
        void StopBgm(float fadeDuration = 1f);
        Task PlaySfx(string clipAddress);
    }
}
