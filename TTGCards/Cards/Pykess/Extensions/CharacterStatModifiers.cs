using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System.Reflection;
using UnboundLib;
using TTGC.Cards;

namespace TTGC.Cards.Extensions
{
    // ADD FIELDS TO CHARACTERSTATMODIFIERS
    public partial class CharacterStatModifiersAdditionalData
    {
        public Dictionary<(int,int), int> minionIDstoCardIndxMap;
        public Dictionary<(int, int), int> oldMinionIDstoCardIndxMap;

        public CharacterStatModifiersAdditionalData()
        {
            minionIDstoCardIndxMap = new Dictionary<(int, int), int>();
            oldMinionIDstoCardIndxMap = new Dictionary<(int, int), int>();
        }
    }
    public static class CharacterStatModifiersExtension
    {
        public static readonly ConditionalWeakTable<CharacterStatModifiers, CharacterStatModifiersAdditionalData> data =
            new ConditionalWeakTable<CharacterStatModifiers, CharacterStatModifiersAdditionalData>();

        public static CharacterStatModifiersAdditionalData GetAdditionalData(this CharacterStatModifiers characterstats)
        {
            return data.GetOrCreateValue(characterstats);
        }

        public static void AddData(this CharacterStatModifiers characterstats, CharacterStatModifiersAdditionalData value)
        {
            try
            {
                data.Add(characterstats, value);
            }
            catch (Exception) { }
        }
        
        // reset additional CharacterStatModifiers when ResetStats is called
        [HarmonyPatch(typeof(CharacterStatModifiers), "ResetStats")]
        class CharacterStatModifiersPatchResetStats
        {
            private static void Prefix(CharacterStatModifiers __instance)
            {
                __instance.GetAdditionalData().oldMinionIDstoCardIndxMap = new Dictionary<(int, int), int>(__instance.GetAdditionalData().minionIDstoCardIndxMap);
                __instance.GetAdditionalData().minionIDstoCardIndxMap = new Dictionary<(int, int), int>() { };
            }
        }

    }
}
