using System;
using System.Collections.Generic;
using Core.Identity;

namespace Gameplay.Inventory
{
    public interface IInventoryManager
    {
        IReadOnlyList<InteractionDef> CollectedItems { get; }
        event Action<InteractionDef> OnItemCollected;
        event Action OnCleared;
        void RestoreItem(InteractionDef def);
        void Clear();
    }
}
