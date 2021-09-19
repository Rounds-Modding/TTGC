using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkinColors")]
    class PlayerSkinBankPatchGetPlayersSkinColors
    {
        private static void Prefix(ref int team)
        {
            team = team % 4;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkin")]
    class PlayerSkinBankPatchGetPlayerSkin
    {
        private static void Prefix(ref int team)
        {
            team = team % 4;
        }
    }
}
