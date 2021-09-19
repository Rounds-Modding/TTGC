using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace TTGCards.Cards.Pykess.Patches
{

    // patch cardbarhandler to prevent trying to add cards to non-existant AI card bars
    [Serializable]
    [HarmonyPatch(typeof(CardBarHandler), "AddCard")]
    class CardBarHandlerPatchAddCard
    {
        private static bool Prefix(CardBarHandler __instance, int teamId)
        {
            if (teamId >= ((CardBar[])Traverse.Create(__instance).Field("cardBars").GetValue()).Length)
            {
                return false;
            }
            return true;
        }
    }
}
