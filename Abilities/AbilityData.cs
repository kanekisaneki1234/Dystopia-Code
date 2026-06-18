using UnityEngine;
using Dystopia.Core;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    public abstract class AbilityData : ScriptableObject
    {
        [Header("Ability Info")]
        public string       abilityName;
        public string       description;
        public AbilityKind  kind;
        public int          manaCost;
        public Sprite       icon;

        public abstract void Execute(AbilityContext context);
        public virtual  void ApplyResonanceBoost() { }
        public virtual  string GetShortDescription(CardInstance caster) => "Ability";
    }
}