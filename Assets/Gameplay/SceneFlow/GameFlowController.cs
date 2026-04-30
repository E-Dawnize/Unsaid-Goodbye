using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DI;
using Core.Events.EventInterfaces;
using Gameplay.Interfaces;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.SceneFlow
{
    public class GameFlowController:IGameFlowController
    {
        [Inject] private IEventCenter _events;
        // [Inject] private ISaveManager _save;
        // [Inject] private IAudioManager _audio;
        // [Inject] private IInventoryManager _inventory;
        // [Inject] private IEndingDeterminer _ending;
        
        private readonly String GamePhaseConfigLabel="GamePhaseConfig";
        private AsyncOperationHandle<IList<GamePhaseConfig>> loadHandle;

        private GamePhaseConfig _currentPhase;
        private Dictionary<GamePhase, GamePhaseConfig> _configs;
        private HashSet<string> _completedBeats = new();
        private bool _isTransitioning;
        public GameSaveDataRuntime GameData{get;private set;}
        public GamePhase CurrentPhase => _currentPhase?.PhaseId ?? GamePhase.None;
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<GamePhase> OnPhaseComplete;
        public async void Initialize()
        {
            try
            {
                await LoadConfigsAndContext();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _events.Subscribe<ItemCollectedEvent>(e =>
                TryCompleteBeat(StoryBeatType.CollectItem, e.ItemID));

            _events.Subscribe<PuzzleSolvedEvent>(e =>
                TryCompleteBeat(StoryBeatType.SolvePuzzle, e.PuzzleID));

            _events.Subscribe<DialogueEndedEvent>(e =>
                TryCompleteBeat(StoryBeatType.CompleteDialogue, e.DialogueID));

            _events.Subscribe<InteractionPerformedEvent>(e =>
                TryCompleteBeat(StoryBeatType.InteractWithObject, e.InteractableID));
        }

        private void TryCompleteBeat(StoryBeatType beatType, string targetID)
        {
            if (_isTransitioning || _currentPhase == null) return;
            foreach (var beat in _currentPhase.RequiredBeats)
            {
                if (beat.type == beatType && beat.targetID == targetID)
                {
                    if (_completedBeats.Add(beat.beatID))
                    {
                        Debug.Log($"[SceneFlow] Beat 完成: {beat.beatID} ({beat.description})");
                        _events.Publish(new StoryBeatCompletedEvent { StoryBeatID = beat.beatID });
                    }
                }
            }

            if (AllBeatsDone())
                OnPhaseComplete?.Invoke(CurrentPhase);
        }

        private bool AllBeatsDone()
        {
            return _currentPhase.RequiredBeats.All(b => _completedBeats.Contains(b.beatID));
        }

        private async Task OnPhaseCompleted()
        {
            _isTransitioning = true;
            OnPhaseComplete?.Invoke(CurrentPhase);
            // 1. 播放离开对话
            if (!string.IsNullOrEmpty(_currentPhase.ExitDialogueId))
            {
                await PlayDialogueAndWait(_currentPhase.ExitDialogueId);
            }

            // 2. 确定下一阶段
            var nextPhase = DetermineNextPhase();

            // 3. 执行转场
            await TransitionToPhase(nextPhase);
        }

        private async Task PlayDialogueAndWait(string dialogueID)
        {
            //TODO:播放动画
        }
        
        
        async Task LoadConfigsAndContext(int saveID=0)
        {
            //加载存档
            var handle = Addressables.LoadAssetAsync<GameSaveData>("Configs/GameSaveData.asset");
            GameData = new(await handle.Task);
            Addressables.Release(handle);
            
            //加载剧情流程文件
            loadHandle = Addressables.LoadAssetsAsync<GamePhaseConfig>(
                GamePhaseConfigLabel,
                asset =>
                {
                    _configs[asset.PhaseId] = asset;
                },
                false  // 失败时不自动释放
            );
            await loadHandle.Task;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Total weapons loaded: {loadHandle.Result.Count}");
            }
            else
            {
                throw new Exception($"Failed to load {loadHandle.Status}");
            }
        }
        public void Dispose()
        {
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
        }
    }
}