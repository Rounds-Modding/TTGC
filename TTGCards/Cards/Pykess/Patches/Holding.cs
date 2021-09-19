using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(Holding), "Start")]
    class HoldingPatchStart
    {
        private static bool Prefix(Holding __instance)
        {

            if (__instance.GetComponent<Player>().data.GetAdditionalData().isAI && __instance.GetComponent<Player>().data.GetAdditionalData().spawner != null)
            {
                __instance.holdable.SetTeamColors(PlayerSkinBank.GetPlayerSkinColors(__instance.GetComponent<Player>().data.GetAdditionalData().spawner.playerID), __instance.GetComponent<Player>());
                return false;
            }
            return true;
        }
    }
}
