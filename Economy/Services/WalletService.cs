using System;
using Dystopia.Core;
using Dystopia.Economy.Data;

namespace Dystopia.Economy.Services
{
    public class WalletService
    {
        private readonly PlayerWallet _wallet;

        // ── Events (UI subscribes to these) ───────────────────────────────
        public event Action<string, int> OnCurrencyChanged;  // currencyName, newBalance

        // ── Constructor ───────────────────────────────────────────────────
        public WalletService(PlayerWallet wallet)
        {
            _wallet = wallet;
        }

        // ── Read-only accessors ───────────────────────────────────────────
        public int Gold       => _wallet.Gold;
        public int Diamonds   => _wallet.Diamonds;
        public int Fragments  => _wallet.Fragments;
        public int RaidTokens => _wallet.RaidTokens;

        public int GetMaterial(CardClass cardClass)
        {
            return _wallet.ClassMaterials[cardClass];
        }

        // ── Validation ────────────────────────────────────────────────────
        public bool CanAfford(int goldCost, int fragmentCost)
        {
            return _wallet.Gold >= goldCost && _wallet.Fragments >= fragmentCost;
        }

        public bool CanAffordTierUpgrade(int goldCost, int fragmentCost, CardClass cardClass, int materialCost)
        {
            return CanAfford(goldCost, fragmentCost)
                && _wallet.ClassMaterials[cardClass] >= materialCost;
        }

        // ── Spend (returns false if insufficient — never goes negative) ──
        public bool TrySpend(int goldCost, int fragmentCost)
        {
            if (!CanAfford(goldCost, fragmentCost)) return false;

            _wallet.Gold      -= goldCost;
            _wallet.Fragments -= fragmentCost;

            OnCurrencyChanged?.Invoke("Gold",      _wallet.Gold);
            OnCurrencyChanged?.Invoke("Fragments", _wallet.Fragments);
            return true;
        }

        public bool TrySpendTierUpgrade(int goldCost, int fragmentCost, CardClass cardClass, int materialCost)
        {
            if (!CanAffordTierUpgrade(goldCost, fragmentCost, cardClass, materialCost))
                return false;

            _wallet.Gold      -= goldCost;
            _wallet.Fragments -= fragmentCost;
            _wallet.ClassMaterials[cardClass] -= materialCost;

            OnCurrencyChanged?.Invoke("Gold",      _wallet.Gold);
            OnCurrencyChanged?.Invoke("Fragments", _wallet.Fragments);
            OnCurrencyChanged?.Invoke($"Material_{cardClass}", _wallet.ClassMaterials[cardClass]);
            return true;
        }

        public bool TrySpendDiamonds(int amount)
        {
            if (_wallet.Diamonds < amount) return false;

            _wallet.Diamonds -= amount;
            OnCurrencyChanged?.Invoke("Diamonds", _wallet.Diamonds);
            return true;
        }

        public bool TrySpendRaidTokens(int amount)
        {
            if (_wallet.RaidTokens < amount) return false;

            _wallet.RaidTokens -= amount;
            OnCurrencyChanged?.Invoke("RaidTokens", _wallet.RaidTokens);
            return true;
        }

        // ── Earn (battle rewards, market sales, etc.) ─────────────────────
        public void AddGold(int amount)
        {
            _wallet.Gold += amount;
            OnCurrencyChanged?.Invoke("Gold", _wallet.Gold);
        }

        public void AddFragments(int amount)
        {
            _wallet.Fragments += amount;
            OnCurrencyChanged?.Invoke("Fragments", _wallet.Fragments);
        }

        public void AddDiamonds(int amount)
        {
            _wallet.Diamonds += amount;
            OnCurrencyChanged?.Invoke("Diamonds", _wallet.Diamonds);
        }

        public void AddRaidTokens(int amount)
        {
            _wallet.RaidTokens += amount;
            OnCurrencyChanged?.Invoke("RaidTokens", _wallet.RaidTokens);
        }

        public void AddMaterial(CardClass cardClass, int amount)
        {
            _wallet.ClassMaterials[cardClass] += amount;
            OnCurrencyChanged?.Invoke($"Material_{cardClass}", _wallet.ClassMaterials[cardClass]);
        }
    }
}