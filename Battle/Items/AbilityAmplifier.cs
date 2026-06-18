using UnityEngine;

namespace Dystopia.Battle.Items
{
    [CreateAssetMenu(menuName = "Dystopia/Items/Ability Amplifier")]
    public class AbilityAmplifier : BattleItem
    {
        public float amplifyMultiplier = 1.5f;

        // BattleSimulator checks this property to set IsAmplified = true
        public override bool IsAmplifier => _queued && !_used;

        public override void TryActivate(BattleTeam caster, BattleTeam target)
        {
            if (_used || !_queued) return;

            // The actual amplification is handled in BattleSimulator
            // via IsAmplifier — nothing to do here except mark as used
            _used   = true;
            _queued = false;
        }
    }
}