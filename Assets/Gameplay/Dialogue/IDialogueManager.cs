using System.Threading.Tasks;

namespace Gameplay.Dialogue
{
    public interface IDialogueManager
    {
        Task PlayAndWait(string dialogueId);
    }
}
