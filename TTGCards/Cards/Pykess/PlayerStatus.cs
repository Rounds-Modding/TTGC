using System.Collections.Generic;
using HarmonyLib;

// stolen from PCE cause I wrote it
namespace TTGC.Cards
{
    public static class PlayerStatus
    {
        public static bool PlayerAlive(Player player)
        {
            return !player.data.dead;
        }
        public static bool PlayerSimulated(Player player)
        {
            return (bool)Traverse.Create(player.data.playerVel).Field("simulated").GetValue();
        }
        public static bool PlayerAliveAndSimulated(Player player)
        {
            return (PlayerStatus.PlayerAlive(player) && PlayerStatus.PlayerSimulated(player));
        }
        public static int GetNumberOfEnemyPlayers(Player player)
        {
            int num = 0;
            foreach (Player other_player in PlayerManager.instance.players)
            {
                if (other_player.teamID != player.teamID)
                {
                    num++;
                }
            }
            return num;
        }
        public static List<Player> GetEnemyPlayers(Player player)
        {
            List<Player> res = new List<Player>() { };
            foreach (Player other_player in PlayerManager.instance.players)
            {
                if (other_player.teamID != player.teamID)
                {
                    res.Add(other_player);
                }
            }
            return res;
        }
        public static Player GetRandomEnemyPlayer(Player player)
        {
            List<Player> enemies = GetEnemyPlayers(player);
            if (GetNumberOfEnemyPlayers(player) == 0) { return null; }
            return enemies[UnityEngine.Random.Range(0, GetNumberOfEnemyPlayers(player))];
        }
        public static List<Player> GetOtherPlayers(Player player)
        {
            List<Player> res = new List<Player>() { };
            foreach (Player other_player in PlayerManager.instance.players)
            {
                if (other_player.playerID != player.playerID)
                {
                    res.Add(other_player);
                }
            }
            return res;
        }
    }
}
