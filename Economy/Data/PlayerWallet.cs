using System;
using System.Collections.Generic;
using UnityEngine;
using Dystopia.Core;

namespace Dystopia.Economy.Data
{
    [Serializable]
    public class PlayerWallet
    {
        // ── Currencies ────────────────────────────────────────────────────
        public int Gold;
        public int Diamonds;
        public int Fragments;
        public int RaidTokens;

        // ── Class materials (one type per CardClass) ──────────────────────
        public Dictionary<CardClass, int> ClassMaterials;

        // ── Constructor ───────────────────────────────────────────────────
        public PlayerWallet()
        {
            Gold       = 0;
            Diamonds   = 0;
            Fragments  = 0;
            RaidTokens = 0;

            ClassMaterials = new Dictionary<CardClass, int>();
            foreach (CardClass c in Enum.GetValues(typeof(CardClass)))
                ClassMaterials[c] = 0;
        }
    }
}