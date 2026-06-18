using System;

namespace Dystopia.Economy.Data
{
    [Serializable]
    public class PlayerStats
    {
        public string Username;
        public int    Wins;
        public int    Losses;
        public int    Draws;
        public int    Level;
        public int    CurrentXP;

        public PlayerStats()
        {
            Username = "Player";
            Wins     = 0;
            Losses   = 0;
            Draws    = 0;
            Level     = 1;
            CurrentXP = 0;
        }
    }
}