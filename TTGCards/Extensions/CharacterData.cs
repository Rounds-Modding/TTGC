using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using TTGC.Cards;

namespace TTGC.Extensions
{
    // ADD FIELDS TO CHARACTERDATA
    [Serializable]
    public class CharacterDataAdditionalData
    {
        public List<Player> minions;
        public bool isAI;
        public Player spawner;
        public AIPlayerHandler.SpawnLocation spawnLocation;

        public CharacterDataAdditionalData()
        {
            minions = new List<Player>() { };
            isAI = false;
            spawner = null;
            spawnLocation = AIPlayerHandler.SpawnLocation.Owner_Random;
        }
    }
    public static class CharacterDataExtension
    {
        public static readonly ConditionalWeakTable<CharacterData, CharacterDataAdditionalData> data =
            new ConditionalWeakTable<CharacterData, CharacterDataAdditionalData>();

        public static CharacterDataAdditionalData GetAdditionalData(this CharacterData characterData)
        {
            return data.GetOrCreateValue(characterData);
        }

        public static void AddData(this CharacterData characterData, CharacterDataAdditionalData value)
        {
            try
            {
                data.Add(characterData, value);
            }
            catch (Exception) { }
        }
    }
    // patch Player.FullReset to properly clear extra stats
    [HarmonyPatch(typeof(Player), "FullReset")]
    class PlayerPatchFullReset_CharacterDataExtension
    {
        private static void Postfix(Player __instance)
        {
            for (int i = 0; i < __instance.data.GetAdditionalData().minions.Count; i++)
            {
                UnityEngine.GameObject.Destroy(__instance.data.GetAdditionalData().minions[i]);
            }
            __instance.data.GetAdditionalData().minions = new List<Player>() { };
            __instance.data.GetAdditionalData().isAI = false;
            __instance.data.GetAdditionalData().spawner = null;
            __instance.data.GetAdditionalData().spawnLocation = AIPlayerHandler.SpawnLocation.Owner_Random;
        }
    }
    
}
