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
    class PlayerManagerPatchGetOtherPlayer
    {
        private static void Prefix(DealDamageToPlayer __instance)
        {
            if ((Player)__instance.GetFieldValue("target") != null)
            {
                Player target = (Player)__instance.GetFieldValue("target");
                if (target.data.dead || !(bool)target.data.playerVel.GetFieldValue("simulated"))
                {
                    __instance.SetFieldValue("target", null);
                }
            }
        }
    }
}
