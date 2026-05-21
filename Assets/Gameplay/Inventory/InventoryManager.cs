using System;
using System.Collections.Generic;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using UnityEngine;

namespace Gameplay.Inventory
{
    public class InventoryManager : IInventoryManager, IInitializable, IDisposable
    {
        [Inject] private IEventCenter _events;

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
            Debug.Log($"[Inventory] Item collected: {e.Def.name}");
            OnItemCollected?.Invoke(e.Def);
        }
    }
}
