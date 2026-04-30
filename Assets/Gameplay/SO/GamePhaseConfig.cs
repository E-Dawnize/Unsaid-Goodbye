using System.Collections.Generic;
using Gameplay.SceneFlow;
using UnityEngine;

namespace Gameplay.SO
{
    [CreateAssetMenu(fileName = "GamePhaseConfig", menuName = "SO/GamePhaseConfig")]
    public class GamePhaseConfig:ScriptableObject
    {
        [Header("基础信息")]
        public GamePhase PhaseId;                    // 阶段ID
        public string DisplayName;                   // 显示名称（调试用）
        public string SceneAssetPath;                // Unity场景路径
        //public BGMTrack BackgroundMusic;             // 背景音乐

        [Header("对话")]
        public string EntryDialogueId;               // 进入时播放的对话ID
        public string ExitDialogueId;                // 完成时播放的对话ID

        [Header("流程控制")]
        public List<StoryBeat> RequiredBeats;         // 本阶段需要完成的故事节拍
        public GamePhase DefaultNextPhase;            // 默认下一阶段
        public GamePhase AltNextPhase;               // 备选下一阶段（用于结局分支）
        public bool IsEndingBranch;                   // 是否在此阶段判定结局

        [Header("转场")]
        public string TransitionSFX;
        public float TransitionDuration = 2f;
    }
}