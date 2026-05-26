namespace Gameplay.Pause
{
    public interface IPauseMenu
    {
        bool IsOpen { get; }
        void Open();
        void Close();
        void Toggle();
    }
}
