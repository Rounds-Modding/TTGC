using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TTGC.Extensions;
using UnityEngine;
using UnboundLib;
using System.Collections;
using System.Reflection;
using Photon.Pun;
using UnboundLib.Networking;

namespace TTGCards.Cards.Pykess.Patches
{
    // patch to ensure the correct gun is obtained for AIs
    [Serializable]
    [HarmonyPatch(typeof(Gun), "DoAttack")]
    class ProjectileInitPatchDoAttack
    {
        private static bool Prefix(Gun __instance, float charge, bool forceAttack = false, float damageM = 1f, float recoilM = 1f, bool useAmmo = true)
        {
            float num = 1f * (1f + charge * __instance.chargeRecoilTo) * recoilM;
            if ((Rigidbody2D)__instance.GetFieldValue("rig"))
            {
                ((Rigidbody2D)__instance.GetFieldValue("rig")).AddForce(((Rigidbody2D)__instance.GetFieldValue("rig")).mass * __instance.recoil * Mathf.Clamp((float)typeof(Gun).GetProperty("usedCooldown", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance), 0f, 1f) * -__instance.transform.up, ForceMode2D.Impulse);
            }
            __instance.GetFieldValue("holdable");
            if ((Action)__instance.GetFieldValue("attackAction") != null)
            {
                ((Action)__instance.GetFieldValue("attackAction"))();
            }
            // use custom FireBurst method to ensure correct gun is used
            __instance.StartCoroutine(FireBurst(__instance, charge, forceAttack, damageM, recoilM, useAmmo));
            return false; // always skip original (TERRIBLE IDEA)
        }

        // custom FireBurst method to support multiple players per photon controller
        private static IEnumerator FireBurst(Gun __instance, float charge, bool forceAttack = false, float damageM = 1f, float recoilM = 1f, bool useAmmo = true)
        {
            int currentNumberOfProjectiles = __instance.lockGunToDefault ? 1 : (__instance.numberOfProjectiles + Mathf.RoundToInt(__instance.chargeNumberOfProjectilesTo * charge));
            if (!__instance.lockGunToDefault)
            {
            }
            if (__instance.timeBetweenBullets == 0f)
            {
                GamefeelManager.GameFeel(__instance.transform.up * __instance.shake);
                __instance.soundGun.PlayShot(currentNumberOfProjectiles);
            }
            int num;
            for (int ii = 0; ii < Mathf.Clamp(__instance.bursts, 1, 100); ii = num + 1)
            {
                for (int i = 0; i < __instance.projectiles.Length; i++)
                {
                    for (int j = 0; j < currentNumberOfProjectiles; j++)
                    {
                        if ((bool)typeof(Gun).InvokeMember("CheckIsMine",
        BindingFlags.Instance | BindingFlags.InvokeMethod |
        BindingFlags.NonPublic, null, __instance, new object[] { }))
                        {
                            __instance.SetFieldValue("spawnPos", __instance.transform.position);
                            if (__instance.player)
                            {
                                __instance.player.GetComponent<PlayerAudioModifyers>().SetStacks();
                                if (__instance.holdable)
                                {
                                    __instance.SetFieldValue("spawnPos", __instance.player.transform.position);
                                }
                            }
                            GameObject gameObject = PhotonNetwork.Instantiate(__instance.projectiles[i].objectToSpawn.gameObject.name, (Vector3)__instance.GetFieldValue("spawnPos"), (Quaternion)typeof(Gun).InvokeMember("getShootRotation", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, __instance, new object[] { j, currentNumberOfProjectiles, charge }), 0, null);
                            float seed = UnityEngine.Random.Range(0f, 1f);
                            if (__instance.holdable)
                            {
                                if (useAmmo)
                                {
                                    if (!PhotonNetwork.OfflineMode)
                                    {
                                        NetworkingManager.RPC_Others(typeof(ProjectileInitPatchDoAttack), nameof(RPCO_Init), new object[]
                                        {
                                            gameObject.GetComponent<PhotonView>().ViewID,
                                            __instance.holdable.holder.view.OwnerActorNr,
                                            __instance.holdable.holder.player.playerID,
                                            currentNumberOfProjectiles,
                                            damageM,
                                            seed
                                        });
                                    }
                                    OFFLINE_Init(gameObject.GetComponent<ProjectileInit>(), __instance.holdable.holder.player.data.view.ControllerActorNr, __instance.holdable.holder.player.playerID, currentNumberOfProjectiles, damageM, seed);
                                }
                                else
                                {
                                    if (!PhotonNetwork.OfflineMode)
                                    {
                                        NetworkingManager.RPC_Others(typeof(ProjectileInitPatchDoAttack), nameof(RPCO_Init_noAmmoUse), new object[]
                                        {
                                            gameObject.GetComponent<PhotonView>().ViewID,
                                            __instance.holdable.holder.view.OwnerActorNr,
                                            __instance.holdable.holder.player.playerID,
                                            currentNumberOfProjectiles,
                                            damageM,
                                            seed
                                        });
                                    }
                                    OFFLINE_Init_noAmmoUse(gameObject.GetComponent<ProjectileInit>(), __instance.holdable.holder.player.data.view.ControllerActorNr, __instance.holdable.holder.player.playerID, currentNumberOfProjectiles, damageM, seed);
                                }
                            }
                            else
                            {
                                if (!PhotonNetwork.OfflineMode)
                                {
                                    NetworkingManager.RPC_Others(typeof(ProjectileInitPatchDoAttack), nameof(RPCO_Init_SeparateGun), new object[]
                                    {
                                            gameObject.GetComponent<PhotonView>().ViewID,
                                            __instance.holdable.holder.view.OwnerActorNr,
                                            __instance.holdable.holder.player.playerID,
                                            (int)__instance.GetFieldValue("gunID"),
                                            currentNumberOfProjectiles,
                                            damageM,
                                            seed
                                    });
                                }
                                OFFLINE_Init_SeparateGun(gameObject.GetComponent<ProjectileInit>(), __instance.holdable.holder.player.data.view.ControllerActorNr, __instance.holdable.holder.player.playerID, (int)__instance.GetFieldValue("gunID"), currentNumberOfProjectiles, damageM, seed);

                            }
                        }
                        if (__instance.timeBetweenBullets != 0f)
                        {
                            GamefeelManager.GameFeel(__instance.transform.up * __instance.shake);
                            __instance.soundGun.PlayShot(currentNumberOfProjectiles);
                        }
                    }
                }
                if (__instance.bursts > 1 && ii + 1 == Mathf.Clamp(__instance.bursts, 1, 100))
                {
                    __instance.soundGun.StopAutoPlayTail();
                }
                if (__instance.timeBetweenBullets > 0f)
                {
                    yield return new WaitForSeconds(__instance.timeBetweenBullets);
                }
                num = ii;
            }
            yield break;
        }
        // custom bullet init methods to support multiple players on a single photon connection
        [UnboundRPC]
        private static void RPCO_Init(int viewID, int senderActorID, int playerID, int nrOfProj, float dmgM, float randomSeed)
        {
            ProjectileInit __instance = PhotonView.Find(viewID).gameObject.GetComponent<ProjectileInit>();
            ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(senderActorID, playerID).data.weaponHandler.gun.BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, true);
        }
        private static void OFFLINE_Init(ProjectileInit __instance, int senderActorID, int playerID, int nrOfProj, float dmgM, float randomSeed)
        {
            ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(senderActorID, playerID).data.weaponHandler.gun.BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, true);
        }
        [UnboundRPC]
        private static void RPCO_Init_SeparateGun(int viewID, int senderActorID, int playerID, int gunID, int nrOfProj, float dmgM, float randomSeed)
        {
            ProjectileInit __instance = PhotonView.Find(viewID).gameObject.GetComponent<ProjectileInit>();
            ((Gun)typeof(ProjectileInit).InvokeMember("GetChildGunWithID", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, __instance, new object[] { gunID, ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(senderActorID, playerID).gameObject })).BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, true);
        }
        private static void OFFLINE_Init_SeparateGun(ProjectileInit __instance, int senderActorID, int playerID, int gunID, int nrOfProj, float dmgM, float randomSeed)
        {
            ((Gun)typeof(ProjectileInit).InvokeMember("GetChildGunWithID", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, __instance, new object[] { gunID, ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(senderActorID, playerID).gameObject })).BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, true);
        }
        [UnboundRPC]
        private static void RPCO_Init_noAmmoUse(int viewID, int senderActorID, int playerID, int nrOfProj, float dmgM, float randomSeed)
        {
            ProjectileInit __instance = PhotonView.Find(viewID).gameObject.GetComponent<ProjectileInit>();
            ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(senderActorID, playerID).data.weaponHandler.gun.BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, false);
        }
        private static void OFFLINE_Init_noAmmoUse(ProjectileInit __instance, int senderActorID, int playerID, int nrOfProj, float dmgM, float randomSeed)
        {
            ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(senderActorID, playerID).data.weaponHandler.gun.BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, false);
        }
    }
}
