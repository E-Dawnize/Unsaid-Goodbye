namespace Gameplay.Save
{
    public interface ISaveManager
    {
        bool SaveExists();
        GameSaveDto LoadSave();
        void WriteSave(GameSaveDto data);
        GameSaveDto CreateNewSave();
    }
}
