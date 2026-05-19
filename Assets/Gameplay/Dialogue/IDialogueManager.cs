using System.Threading.Tasks;
using Gameplay.SO;

namespace Gameplay.Dialogue
{
    public interface IDialogueManager
    {
        bool IsPlaying { get; }
        Task PlayAndWait(string dialogueId);
        Task PlayAndWait(DialogueSequence sequence);
    }
}
