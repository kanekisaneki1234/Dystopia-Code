using System;

namespace Dystopia.Economy.Data
{
    [Serializable]
    public class DailyRewardResult
    {
        public int streak;
        public int reward;
        public int totalDiamonds;
    }

    [Serializable]
    public class DailyRewardStatus
    {
        public bool   canClaim;
        public int    currentStreak;
        public int    nextReward;
        public string lastClaimDate;
    }
}
