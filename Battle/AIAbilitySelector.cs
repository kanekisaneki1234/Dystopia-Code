using System;
using UnityEngine;
using Dystopia.Core;

namespace Dystopia.Battle
{
    public class AIAbilitySelector : MonoBehaviour, IAbilitySelector
    {
        public bool  IsReady      { get; private set; }
        public int?  SelectedSlot { get; private set; }
        public event Action OnReady;

        public void BeginSelection(BattleTeam team, float windowSeconds)
        {
            IsReady      = true;
            SelectedSlot = null;
            OnReady?.Invoke();
        }

        public void ForceEnd() { }
    }
}
