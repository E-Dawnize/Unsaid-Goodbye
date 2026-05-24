using System;
using System.Collections.Generic;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Audio;
using UnityEngine;

namespace Gameplay.Inventory
{
    public class InventoryManager : IInventoryManager, IInitializable, IDisposable
    {
        [Inject] private IEventCenter _events;
        [InjectOptional] private IAudioManager _audio;

        private const string ItemPickupSfxKey = "SFX/ItemPickup";

        private readonly List<InteractionDef> _collected = new();

        private static readonly HashSet<string> ItemNames = new()
        {
            "Def_DogCollar", "Def_SofaPhoto", "Def_CrateLabel",
            "Def_WallDiary", "Def_ParrotFeather",
            "Def_PillBottle", "Def_SofaPhone", "Def_WallFrame",
            "Def_BalconyCorner", "Def_BedCornerSavings"
        };

        public IReadOnlyList<InteractionDef> CollectedItems => _collected;
        public event Action<InteractionDef> OnItemCollected;
        public event Action OnCleared;

        public void Initialize()
        {
            _events.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
        }

        public void Dispose()
        {
            _events?.Unsubscribe<DialogueEndedEvent>(OnDialogueEnded);
        }

        private void OnDialogueEnded(DialogueEndedEvent e)
        {
            if (e.Def == null || _collected.Contains(e.Def)) return;
            if (!ItemNames.Contains(e.Def.name)) return;

            _collected.Add(e.Def);
            _audio?.PlaySfx(ItemPickupSfxKey);
            Debug.Log($"[Inventory] Item collected: {e.Def.name}");
            OnItemCollected?.Invoke(e.Def);
        }

        public void Clear()
        {
            _collected.Clear();
            OnCleared?.Invoke();
        }

        /// <summary>读档时恢复已收集的道具</summary>
        public void RestoreItem(InteractionDef def)
        {
            if (def == null || _collected.Contains(def)) return;
            if (!ItemNames.Contains(def.name)) return;

            _collected.Add(def);
            Debug.Log($"[Inventory] Item restored from save: {def.name}");
            OnItemCollected?.Invoke(def);
        }
    }
}
