using UnityEngine;

namespace Dystopia.Battle.Items
{
    [CreateAssetMenu(menuName = "Dystopia/Items/Mana Crystal")]
    public class ManaCrystal : BattleItem
    {
        public override void TryActivate(BattleTeam caster, BattleTeam target)
        {
            if (_used || !_queued) return;

            // Fill mana to maximum instantly
            int fillAmount = caster.MaxMana - caster.CurrentMana;
            caster.AddMana(fillAmount);

            _used   = true;
            _queued = false;
        }
    }
}