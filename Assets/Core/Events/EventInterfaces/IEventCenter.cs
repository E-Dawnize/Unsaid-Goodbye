
using System;

namespace Core.Events.EventInterfaces
{
    /// <summary>
    /// 事件总线对外接口，T为事件类型封装接口，定义在EventDefinitions下
    /// </summary>
    public interface IEventCenter
    {
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T evt) where T : struct;
    }
}