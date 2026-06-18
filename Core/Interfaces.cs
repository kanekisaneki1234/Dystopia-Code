using System;
using Dystopia.Abilities;
using Dystopia.Battle;

namespace Dystopia.Core
{
    public interface IBattleParticipant
    {
        string  OwnerName        { get; }
        int     CurrentHP        { get; }
        int     AggregateAttack  { get; }
        int     AggregateDefence { get; }
        int     AggregateSpeed   { get; }
        bool    IsDefeated       { get; }
        void    TakeDamage(int amount);
        void    RegenMana(int currentTurn, int opponentSpeed);
    }

    public interface IProgressable
    {
        TierLevel      Tier      { get; }
        ResonanceLevel Resonance { get; }
        void           Recalculate();
    }

    public interface IAbility
    {
        AbilityKind Kind     { get; }
        int         ManaCost { get; }
        void        Execute(AbilityContext context);
        void        ApplyResonanceBoost();
    }

    public interface IAbilitySelector
    {
        bool  IsReady      { get; }
        int?  SelectedSlot { get; }
        event Action OnReady;
        void  BeginSelection(BattleTeam team, float windowSeconds);
        void  ForceEnd();
    }
}