using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;
using UnityEngine;
using UnboundLib;
using TTGC.Cards;

namespace TTGCards.Cards.Pykess.Patches
{

    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(CharacterData), "Start")]
    class CharacterDataPatchStart
    {
        private static bool Prefix(CharacterData __instance)
        {
            __instance.SetFieldValue("groundMask", (LayerMask)LayerMask.GetMask(new string[]
            {
                    "Default"
            }));
            return AIMinionHandler.playersCanJoin;
        }
    }
}
