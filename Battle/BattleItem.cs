using UnityEngine;

namespace Dystopia.Battle
{
    public abstract class BattleItem : ScriptableObject
    {
        [Header("Item Info")]
        public string itemName;
        public string description;
        public Sprite icon;

        // Whether this item has been used this battle
        protected bool _used = false;

        // Whether this item is the Ability Amplifier
        // BattleSimulator checks this to set IsAmplified in AbilityContext
        public virtual bool IsAmplifier => false;

        // Called by BattleSimulator at the start of each attack phase.
        // Item activates itself if the player has queued it this turn.
        public abstract void TryActivate(BattleTeam caster, BattleTeam target);

        // Called by the player via UI to queue activation this turn
        public void QueueActivation() => _queued = true;

        protected bool _queued = false;

        // Reset at the start of each battle
        public virtual void ResetForBattle()
        {
            _used   = false;
            _queued = false;
        }
    }
}