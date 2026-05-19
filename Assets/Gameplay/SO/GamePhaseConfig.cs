using System.Collections.Generic;
using Core.Identity;
using Gameplay.SceneFlow;
using UnityEngine;

namespace Gameplay.SO
{
    [CreateAssetMenu(fileName = "GamePhaseConfig", menuName = "SO/GamePhaseConfig")]
    public class GamePhaseConfig : ScriptableObject
    {
        [Header("基础信息")]
        public GamePhase PhaseId;
        public string DisplayName;
        public string SceneAssetPath;
        public string BackgroundMusic;

        [Header("对话")]
        public string EntryDialogueId;
        public string ExitDialogueId;

        [Header("流程控制")]
        public List<InteractionDef> RequiredBeats;
        public GamePhase DefaultNextPhase;
        public GamePhase AltNextPhase;
        public bool IsEndingBranch;

        [Header("转场")]
        public string TransitionSFX;
        public float TransitionDuration = 2f;
    }
}