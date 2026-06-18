using UnityEngine;

namespace Dystopia.Battle.Items
{
    [CreateAssetMenu(menuName = "Dystopia/Items/Speed Surge")]
    public class SpeedSurge : BattleItem
    {
        public override void TryActivate(BattleTeam caster, BattleTeam target)
        {
            if (_used || !_queued) return;

            // Force this team to go first this turn regardless of Speed
            caster.ForceFirstStrike = true;

            _used   = true;
            _queued = false;
        }
    }
}
