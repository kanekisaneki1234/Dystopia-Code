using UnityEngine;

namespace Dystopia.Battle.Items
{
    [CreateAssetMenu(menuName = "Dystopia/Items/Mana Drain")]
    public class ManaDrain : BattleItem
    {
        public int drainAmount = 30;

        public override void TryActivate(BattleTeam caster, BattleTeam target)
        {
            if (_used || !_queued) return;

            target.SpendMana(drainAmount);

            _used   = true;
            _queued = false;
        }
    }
}