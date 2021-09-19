using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch FollowPlayer.LateUpdate to fix lag
    [Serializable]
    [HarmonyPatch(typeof(FollowPlayer), "LateUpdate")]
    class FollowPlayerPatchLateUpdate
    {
        private static bool Prefix(FollowPlayer __instance)
        {
            Player otherPlayer;
            if (__instance.target == FollowPlayer.Target.Other)
            {
                otherPlayer = PlayerManager.instance.GetOtherPlayer(__instance.GetComponentInParent<Player>());
            }
            else
            {
                otherPlayer = __instance.GetComponentInParent<Player>();
            }

            if (otherPlayer == null)
            {
                return false;
            }
            return true;
        }
    }
}
