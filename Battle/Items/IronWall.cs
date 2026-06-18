using UnityEngine;

namespace Dystopia.Battle.Items
{
    [CreateAssetMenu(menuName = "Dystopia/Items/Iron Wall")]
    public class IronWall : BattleItem
    {
        [Range(0f, 1f)]
        public float damageReduction = 0.5f;

        public override void TryActivate(BattleTeam caster, BattleTeam target)
        {
            if (_used || !_queued) return;

            caster.SetDamageReduction(damageReduction);

            _used   = true;
            _queued = false;
        }
    }
}