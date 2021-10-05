using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;
using TTGC.Cards;
using UnboundLib;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch to fix DealDamageToPlayer.Go
    [Serializable]
    [HarmonyPatch(typeof(DealDamageToPlayer), "Go")]
    class DealDamageToPlayerPatchGo
    {
        private static void Prefix(DealDamageToPlayer __instance)
        {
            __instance.SetFieldValue("target", null);
        }
    }
}
