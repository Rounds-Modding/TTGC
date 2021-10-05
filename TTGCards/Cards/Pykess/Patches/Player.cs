using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;
using TTGC.Cards;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch to prevent game crash
    [Serializable]
    [HarmonyPatch(typeof(Player), "GetFaceOffline")]
    class PlayerPatchGetFaceOffline
    {
        private static bool Prefix(Player __instance)
        {

            if (__instance.playerID >= CharacterCreatorHandler.instance.selectedPlayerFaces.Length)
            {
                return false;
            }
            return true;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(Player), "SetColors")]
    class PlayerPatchSetColors
    {
        private static bool Prefix(Player __instance)
        {

            if (__instance.data.GetAdditionalData().isAI && __instance.data.GetAdditionalData().spawner != null)
            {
                SetTeamColor.TeamColorThis(__instance.gameObject, __instance.data.GetAdditionalData().spawner.GetTeamColors());
                return false;
            }
            return true;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(Player), "GetTeamColors")]
    class PlayerPatchGetTeamColors
    {
        private static bool Prefix(Player __instance, ref PlayerSkin __result)
        {

            if (__instance.data.GetAdditionalData().isAI && __instance.data.GetAdditionalData().spawner != null)
            {
                __result = __instance.data.GetAdditionalData().spawner.GetTeamColors();
                return false;
            }
            return true;
        }
    }
    // patch to prevent unwanted registering of AIs online
    [Serializable]
    [HarmonyPatch(typeof(Player), "Start")]
    class PlayerPatchStart
    {
        private static bool Prefix(Player __instance)
        {
            if (!__instance.data.view.IsMine)
            {
                return AIMinionHandler.playersCanJoin;
            }
            else
            {
                return true;
            }
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "AssignPlayerID")]
    class PlayerPatchAssignPlayerID
    {
        private static bool Prefix(Player __instance, int ID)
        {
            __instance.playerID = ID;
            __instance.SetColors();
            return AIMinionHandler.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "AssignTeamID")]
    class PlayerPatchAssignTeamID
    {
        private static bool Prefix(Player __instance, int ID)
        {
            __instance.teamID = ID;
            return AIMinionHandler.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "ReadPlayerID")]
    class PlayerPatchReadPlayerID
    {
        private static bool Prefix(Player __instance)
        {
            return AIMinionHandler.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "ReadTeamID")]
    class PlayerPatchReadTeamID
    {
        private static bool Prefix(Player __instance)
        {
            return AIMinionHandler.playersCanJoin;
        }
    }
}
