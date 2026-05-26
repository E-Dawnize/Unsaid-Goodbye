namespace Gameplay.Inventory
{
    public interface IBackpackUI
    {
        bool IsOpen { get; }
        void Open();
        void Close();
    }
}
