using System.Threading.Tasks;

namespace Gameplay.Interstitial
{
    /// <summary>
    /// 全屏插屏组件接口 — 用于在场景切换前展示引导图、制作人员名单等
    /// </summary>
    public interface IInterstitialScreen
    {
        /// <summary>
        /// 展示插屏图片，等待玩家点击后关闭
        /// </summary>
        /// <param name="addressableKey">Addressables 中 Sprite 的 key</param>
        /// <param name="minDisplaySeconds">最低展示秒数，超时前点击无效；≤0 则立即可点击</param>
        /// <returns>玩家点击且最低时间已过后完成</returns>
        Task ShowAsync(string addressableKey, float minDisplaySeconds = 2f);
    }
}
