using System;

namespace Dystopia.Economy.Data
{
    [Serializable]
    public class PlayerProfile
    {
        // ── Unique identifier (set by auth system later) ──────────────────
        public string PlayerId;

        // ── Owned components ──────────────────────────────────────────────
        public PlayerWallet    Wallet;
        public CardCollection  Collection;
        public PlayerStats     Stats;

        // ── Constructor ───────────────────────────────────────────────────
        public PlayerProfile()
        {
            PlayerId   = string.Empty;
            Wallet     = new PlayerWallet();
            Collection = new CardCollection();
            Stats      = new PlayerStats();
        }
    }
}