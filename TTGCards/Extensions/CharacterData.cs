using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System.Collections.Generic;

namespace TTGC.Extensions
{
    // ADD FIELDS TO CHARACTERDATA
    [Serializable]
    public class CharacterDataAdditionalData
    {
        public List<Player> minions;
        public bool isAI;
        public Player spawner;

        public CharacterDataAdditionalData()
        {
            minions = new List<Player>() { };
            isAI = false;
            spawner = null;
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
    class OutOfBoundsHandlerPatchStart
    {
        private static void Postfix(Player __instance)
        {
            __instance.data.GetAdditionalData().minions = new List<Player>() { };
            __instance.data.GetAdditionalData().isAI = false;
            __instance.data.GetAdditionalData().spawner = null;
        }
    }
    
}
