using Core.DI;

namespace Core.Architecture
{
    /// <summary>
    /// 当前场景作用域访问器 — 全局唯一，由 ProjectContext/SceneScopeRunner 管理
    /// 负责追踪"当前活跃场景"的 DI Scope
    /// </summary>
    public interface IScopeProvider
    {
        IScope CurrentScope { get; set; }
    }

    public class ScopeProvider : IScopeProvider
    {
        public IScope CurrentScope { get; set; }
    }
}
