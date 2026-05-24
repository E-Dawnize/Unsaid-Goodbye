using System.Threading.Tasks;
using Gameplay.SO;

namespace Gameplay.Dialogue
{
    public interface IDialogueManager
    {
        bool IsPlaying { get; }
        Task PlayAndWait(string dialogueId);
        Task PlayAndWait(DialogueSequence sequence);
        /// <summary>每行对话显示时触发 (lineIndex, totalLines)</summary>
        event System.Action<int, int> OnLineDisplayed;
    }
}
