using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;
using TTGC.Cards;
using UnityEngine;
using UnboundLib;
using System.Reflection;
using Photon.Pun;
using Sonigon;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch to prevent all clients from calling RPCA_Die and RPCA_Die_Phoenix
    [Serializable]
    [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
    class HealthHandlerPatchDoDamage
    {
        private static bool Prefix(HealthHandler __instance, Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon = null, Player damagingPlayer = null, bool healthRemoval = false, bool lethal = true, bool ignoreBlock = false)
        {
            if (damage == Vector2.zero)
            {
                return false;
            }
            CharacterData data = (CharacterData)__instance.GetFieldValue("data");
            if (!data.isPlaying)
            {
                return false;
            }
            if (data.dead)
            {
                return false;
            }
            if (data.block.IsBlocking() && !ignoreBlock)
            {
                return false;
            }
            if (__instance.isRespawning)
            {
                return false;
            }
            if (damagingPlayer)
            {
                damagingPlayer.GetComponent<CharacterStatModifiers>().DealtDamage(damage, damagingPlayer != null && damagingPlayer.transform.root == __instance.transform, data.player);
            }
            __instance.StopAllCoroutines();
            typeof(HealthHandler).InvokeMember("DisplayDamage", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, __instance, new object[] { blinkColor });
            data.lastSourceOfDamage = damagingPlayer;
            data.health -= damage.magnitude;
            ((CharacterStatModifiers)__instance.GetFieldValue("stats")).WasDealtDamage(damage, damagingPlayer != null && damagingPlayer.transform.root == __instance.transform);
            if (!lethal)
            {
                data.health = Mathf.Clamp(data.health, 1f, data.maxHealth);
            }
            // ONLY SEND RPC IF THIS PLAYER IS OURS
            if (data.health < 0f && !data.dead && data.view.IsMine)
            {
                if (data.stats.remainingRespawns > 0)
                {
                    data.view.RPC("RPCA_Die_Phoenix", RpcTarget.All, new object[]
                    {
                        damage
                    });
                }
                else
                {
                    data.view.RPC("RPCA_Die", RpcTarget.All, new object[]
                    {
                        damage
                    });
                }
            }
            if ((float)__instance.GetFieldValue("lastDamaged") + 0.15f < Time.time && damagingPlayer != null && damagingPlayer.data.stats.lifeSteal != 0f)
            {
                SoundManager.Instance.Play(__instance.soundDamageLifeSteal, __instance.transform);
            }
            __instance.SetFieldValue("lastDamaged", Time.time);
            return false;
        }
    }
}
