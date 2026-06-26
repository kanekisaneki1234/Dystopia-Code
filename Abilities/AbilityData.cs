using UnityEngine;
using Dystopia.Core;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    public abstract class AbilityData : ScriptableObject, IAbility
    {
        [Header("Ability Info")]
        public string abilityName;
        public string description;
        [SerializeField] private AbilityKind kind;
        [SerializeField] private int         manaCost;
        public Sprite icon;

        public AbilityKind Kind     => kind;
        public int         ManaCost => manaCost;

        public abstract void Execute(AbilityContext context);
        public virtual  void ApplyResonanceBoost() { }
        public virtual  string GetShortDescription(CardInstance caster) => "Ability";
    }
}