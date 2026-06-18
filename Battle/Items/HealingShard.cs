using UnityEngine;

namespace Dystopia.Battle.Items
{
    [CreateAssetMenu(menuName = "Dystopia/Items/Healing Shard")]
    public class HealingShard : BattleItem
    {
        [Range(0f, 1f)]
        public float healPercent = 0.15f;

        public override void TryActivate(BattleTeam caster, BattleTeam target)
        {
            if (_used || !_queued) return;

            int healAmount = Mathf.RoundToInt(caster.MaxHP * healPercent);
            caster.Heal(healAmount);

            _used   = true;
            _queued = false;
        }
    }
}