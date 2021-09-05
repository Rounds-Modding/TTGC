using UnboundLib.Cards;
using UnityEngine;
using UnboundLib;
using TMPro;
using System.Linq;
using Photon.Pun;
using Photon;
using System.Collections;
using System.Collections.Generic;
using UnboundLib.Networking;
using SoundImplementation;
using HarmonyLib;
using System.Reflection;
using Sonigon;
using TTGC.Extensions;
using UnboundLib.GameModes;
using ModdingUtils;
using System;

namespace TTGC.Cards
{
    public static class AIPlayer
    {
        internal static bool sandbox
        {
            get
            {
                return (GM_Test.instance != null && GM_Test.instance.gameObject.activeInHierarchy);
            }
        }

        private static GameObject _AIBase = null;
        private static GameObject AIBase
        {
            get
            {
                if (_AIBase != null) { return _AIBase; }
                else
                {
                    _AIBase = new GameObject("AI", typeof(EnableDisablePlayer), typeof(DisableOnDeath), typeof(EndStalemate));
                    return _AIBase;
                }
            }
            set { }
        }
        [RequireComponent(typeof(Player))]
        private class RestoreAIControllerOnDeath : MonoBehaviour
        {
            private AI originalController;
            void Start()
            {
                AI? orig = AIPlayer.GetActiveController(this.gameObject.GetComponent<Player>());
                if (orig == null) { Destroy(this); }
                else
                {
                    this.originalController = (AI)orig;
                }
            }
            void OnDisable()
            {
                switch (this.originalController)
                {
                    case AI.Default:
                        ChangeAIController<PlayerAI>(this.gameObject.GetComponent<Player>());
                        break;
                    case AI.David:
                        ChangeAIController<PlayerAIDavid>(this.gameObject.GetComponent<Player>());
                        break;
                    case AI.Minion:
                        ChangeAIController<PlayerAIMinion>(this.gameObject.GetComponent<Player>());
                        break;
                    case AI.Petter:
                        ChangeAIController<PlayerAIPetter>(this.gameObject.GetComponent<Player>());
                        break;
                    case AI.Philip:
                        ChangeAIController<PlayerAIPhilip>(this.gameObject.GetComponent<Player>());
                        break;
                    case AI.Wilhelm:
                        ChangeAIController<PlayerAIWilhelm>(this.gameObject.GetComponent<Player>());
                        break;
                    case AI.Zorro:
                        ChangeAIController<PlayerAIZorro>(this.gameObject.GetComponent<Player>());
                        break;
                }
            }
        }
        

        private class EndStalemate : MonoBehaviour
        {
            // if only AI players remain, give them 10 seconds to duke it out, after that switch their AI to suicidal, then give them another 10 seconds before just killing them
            private readonly float maxTime = 20f;
            private readonly float maxTimeToAIChange = 10f;
            private readonly float updateDelay = 1f;
            private float startTime;
            private float updateTime;
            private bool onlyAI = false;
            private bool suicidal = false;
            private bool killed = false;
            void Start()
            {
                ResetUpdate();
                ResetTimer();
            }
            void OnEnable()
            {
                this.onlyAI = false;
                this.suicidal = false;
                this.killed = false;
                ResetUpdate();
                ResetTimer();
            }
            void OnDisable()
            {
                this.onlyAI = false;
                this.suicidal = false;
                this.killed = false;
                ResetUpdate();
                ResetTimer();
            }
            void Update()
            {
                if (!(Time.time >= this.updateTime + this.updateDelay) || AIPlayer.sandbox)
                {
                    return;
                }

                ResetUpdate();

                if (!onlyAI && !PlayerManager.instance.players.Where(player => !player.data.dead && !player.data.GetAdditionalData().isAI).Any())
                {
                    onlyAI = true;
                    ResetTimer();
                }

                if (onlyAI && !suicidal && Time.time >= this.startTime + this.maxTimeToAIChange)
                {
                    this.suicidal = true;
                    foreach (Player player in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().isAI))
                    {
                        player.gameObject.AddComponent<RestoreAIControllerOnDeath>();
                        Unbound.Instance.ExecuteAfterSeconds(1f, () =>
                        {
                            ChangeAIController(player, AIPlayer.ChooseAIController(aggression: AIAggression.Suicidal, AItype: (AI?)null));
                        });
                    }
                }

                if (!killed && onlyAI && suicidal && Time.time >= this.startTime + this.maxTime)
                {
                    ResetTimer();
                    this.killed = true;
                    // kill ALL AI players
                    foreach (Player AIPlayer in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().isAI))
                    {
                        
                        Unbound.Instance.ExecuteAfterSeconds(1f, delegate
                        {
                            AIPlayer.data.view.RPC("RPCA_Die", RpcTarget.All, new object[]
                            {
                                    new Vector2(0, 1)
                            });

                        });
                    }
                }
            }
            void ResetTimer()
            {
                this.startTime = Time.time;
            }
            void ResetUpdate()
            {
                this.updateTime = Time.time;
            }
        }
        internal class EnableDisablePlayer : MonoBehaviour
        {
            private Player player
            {
                get
                {
                    return this.gameObject.transform.parent.gameObject.GetComponent<Player>();
                }
            }
            void Start()
            {
                if (AIPlayer.sandbox) { Destroy(this); }
            }
            internal void Disable()
            {
                if (this.player == null) { return; }
                PlayerManager.instance.players.Remove(this.player);
                PlayerAssigner.instance.players.Remove(this.player.data);
                this.player.data.isPlaying = false;
                Traverse.Create(this.player.data.playerVel).Field("simulated").SetValue(false);

                Unbound.Instance.ExecuteAfterSeconds(1f, () =>
                {
                    this.player.GetComponent<Holding>().holdable.GetComponent<Gun>().gameObject.SetActive(false);
                    this.gameObject.transform.parent.gameObject.SetActive(false);
                    this.player.data.gameObject.transform.position = Vector3.up * 200f;
                });
            }
            internal void Enable(Vector3? pos = null)
            {
                Vector3 Pos = pos ?? Vector3.zero;
                PlayerManager.instance.players.Add(this.player);
                PlayerAssigner.instance.players.Add(this.player.data);
                this.player.GetComponent<Holding>().holdable.GetComponent<Gun>().gameObject.SetActive(true);
                this.gameObject.transform.parent.gameObject.SetActive(true);
                this.player.data.isPlaying = true;
                this.player.data.gameObject.transform.position = Pos;
                Traverse.Create(this.player.data.playerVel).Field("simulated").SetValue(true);
            }
            internal void ReviveAndSpawn(Vector3? pos = null)
            {
                Vector3 Pos = pos ?? Vector3.zero;

                this.player.data.healthHandler.Revive(true);
                this.player.GetComponent<GeneralInput>().enabled = true;
                this.player.data.gameObject.transform.position = Pos;
                SoundManager.Instance.Play(PlayerManager.instance.soundCharacterSpawn[0], this.player.transform);
            }
        }
        private class DisableOnDeath : MonoBehaviour
        {
            private Player player
            {
                get
                {
                    return this.gameObject.transform.parent.gameObject.GetComponent<Player>();
                }
            }
            void Start()
            {
                if (AIPlayer.sandbox) { Destroy(this); }
            }
            void OnDisable()
            {
                if (!AIPlayer.sandbox) { this.GetComponent<EnableDisablePlayer>().Disable(); }
            }
        }
        internal static AI? GetActiveController(Player minion)
        {
            GameObject AIobj = null;

            if (minion.gameObject.GetComponentInChildren<PlayerAI>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAI>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIDavid>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAIDavid>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIMinion>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAIMinion>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIPetter>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAIPetter>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIPhilip>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAIPhilip>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIWilhelm>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAIWilhelm>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIZorro>() != null) { AIobj = minion.gameObject.GetComponentInChildren<PlayerAIZorro>().gameObject; }
            else { return null; }

            if (AIobj == null) { return null; }

            if (AIobj.GetComponent<PlayerAI>() != null && AIobj.GetComponent<PlayerAI>().enabled ){ return AI.Default; }
            else if (AIobj.GetComponent<PlayerAIDavid>() != null && AIobj.GetComponent<PlayerAIDavid>().enabled ){ return AI.David; }
            else if (AIobj.GetComponent<PlayerAIMinion>() != null && AIobj.GetComponent<PlayerAIMinion>().enabled ){ return AI.Minion; }
            else if (AIobj.GetComponent<PlayerAIPetter>() != null && AIobj.GetComponent<PlayerAIPetter>().enabled ){ return AI.Petter; }
            else if (AIobj.GetComponent<PlayerAIPhilip>() != null && AIobj.GetComponent<PlayerAIPhilip>().enabled ){ return AI.Philip; }
            else if (AIobj.GetComponent<PlayerAIWilhelm>() != null && AIobj.GetComponent<PlayerAIWilhelm>().enabled ){ return AI.Wilhelm; }
            else if (AIobj.GetComponent<PlayerAIZorro>() != null && AIobj.GetComponent<PlayerAIZorro>().enabled ){ return AI.Zorro; }
            else { return null; }
        }
        internal static void ChangeAIController(Player minion, AI newAI)
        {
            switch(newAI)
            {
                case AI.Default:
                    ChangeAIController<PlayerAI>(minion);
                    break;
                case AI.David:
                    ChangeAIController<PlayerAIDavid>(minion);
                    break;
                case AI.Minion:
                    ChangeAIController<PlayerAIMinion>(minion);
                    break;
                case AI.Petter:
                    ChangeAIController<PlayerAIPetter>(minion);
                    break;
                case AI.Philip:
                    ChangeAIController<PlayerAIPhilip>(minion);
                    break;
                case AI.Wilhelm:
                    ChangeAIController<PlayerAIWilhelm>(minion);
                    break;
                case AI.Zorro:
                    ChangeAIController<PlayerAIZorro>(minion);
                    break;
            }
        }
        internal static void ChangeAIController<newAIController>(Player minion) where newAIController : MonoBehaviour
        {
            GameObject AI = null;

            if (minion.gameObject.GetComponentInChildren<PlayerAI>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAI>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIDavid>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAIDavid>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIMinion>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAIMinion>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIPetter>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAIPetter>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIPhilip>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAIPhilip>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIWilhelm>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAIWilhelm>().gameObject; }
            else if (minion.gameObject.GetComponentInChildren<PlayerAIZorro>() != null) { AI = minion.gameObject.GetComponentInChildren<PlayerAIZorro>().gameObject; }
            else { return; }

            if (AI == null) { return; }

            if (AI.GetComponent<PlayerAI>() != null) { AI.GetComponent<PlayerAI>().enabled = false; }
            if (AI.GetComponent<PlayerAIDavid>() != null) { AI.GetComponent<PlayerAIDavid>().enabled = false; }
            if (AI.GetComponent<PlayerAIMinion>() != null) { AI.GetComponent<PlayerAIMinion>().enabled = false; }
            if (AI.GetComponent<PlayerAIPetter>() != null) { AI.GetComponent<PlayerAIPetter>().enabled = false; }
            if (AI.GetComponent<PlayerAIPhilip>() != null) { AI.GetComponent<PlayerAIPhilip>().enabled = false; }
            if (AI.GetComponent<PlayerAIWilhelm>() != null) { AI.GetComponent<PlayerAIWilhelm>().enabled = false; }
            if (AI.GetComponent<PlayerAIZorro>() != null) { AI.GetComponent<PlayerAIZorro>().enabled = false; }

            // get or add new AIController and enable it
            AI.gameObject.GetOrAddComponent<newAIController>().enabled = true;
        }
        internal static System.Type ChooseAIController(AISkill? skill = null, AIAggression? aggression = null, System.Type AItype = null)
        {
            System.Type AIController = typeof(PlayerAI);

            if (skill != null)
            {
                AIController = GetAIType(ChooseAIController(skill, aggression, (AI?)null));
            }
            else if (aggression != null)
            {
                AIController = GetAIType(ChooseAIController(skill, aggression, (AI?)null));
            }
            else if (AItype != null)
            {
                AIController = AItype;
            }

            return AIController;
        }
        internal static AI ChooseAIController(AISkill? skill = null, AIAggression? aggression = null, AI? AItype = null)
        {
            AI AIController = AI.Default;

            if (skill != null)
            {
                switch (skill)
                {
                    case AISkill.Beginner:
                        {
                            AIController = AI.David;
                            break;
                        }
                    case AISkill.Normal:
                        {
                            switch (UnityEngine.Random.Range(0, 2))
                            {
                                case 0:
                                    {
                                        AIController = AI.Default;
                                        break;
                                    }
                                case 1:
                                    {
                                        AIController = AI.Wilhelm;
                                        break;
                                    }
                            }
                            break;
                        }
                    case AISkill.Expert:
                        {
                            AIController = AI.Philip;
                            break;
                        }
                }
            }
            else if (aggression != null)
            {
                switch (aggression)
                {
                    case AIAggression.Peaceful:
                        {
                            switch (UnityEngine.Random.Range(0, 2))
                            {
                                case 0:
                                    {
                                        AIController = AI.Petter;
                                        break;
                                    }
                                case 1:
                                    {
                                        AIController = AI.Zorro;
                                        break;
                                    }
                            }
                            break;
                        }
                    case AIAggression.Normal:
                        {
                            AIController = AI.Default;
                            break;
                        }
                    case AIAggression.Aggressive:
                        {
                            AIController = AI.Philip;
                            break;
                        }
                    case AIAggression.Suicidal:
                        {
                            AIController = AI.Wilhelm;
                            break;
                        }
                }
            }
            else if (AItype != null)
            {
                AIController = (AI)AItype;
            }

            return AIController;
        }

        internal static Player SpawnAI(int spawnerID, int teamID, bool activeNow = false, AISkill? skill = null, AIAggression? aggression = null, System.Type AItype = null)
        {
            Player spawner = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithID",
                                BindingFlags.Instance | BindingFlags.InvokeMethod |
                                BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { spawnerID });

            System.Type AIController = ChooseAIController(skill, aggression, AItype);


            if (PlayerManager.instance.players.Count >= PlayerAssigner.instance.maxPlayers)
            {
                PlayerAssigner.instance.maxPlayers++;
            }

            if (activeNow)
            {
                SoundPlayerStatic.Instance.PlayPlayerAdded();
            }

            Vector3 position = Vector3.up * 100f;
            CharacterData AIdata = PhotonNetwork.Instantiate(PlayerAssigner.instance.playerPrefab.name, position, Quaternion.identity, 0, null).GetComponent<CharacterData>();

            // mark this player as an AI
            AIdata.GetAdditionalData().isAI = true;
            // add the spawner to the AI's data
            AIdata.GetAdditionalData().spawner = spawner;
            // add AI to spawner's data
            spawner.data.GetAdditionalData().minions.Add(AIdata.player);

            AIdata.GetComponent<CharacterData>().SetAI(null);
            UnityEngine.Object.Instantiate<GameObject>(AIBase, AIdata.transform.position, AIdata.transform.rotation, AIdata.transform).AddComponent(AIController);

            AIdata.player.AssignPlayerID(PlayerManager.instance.players.Count);
            AIdata.player.AssignTeamID(teamID);

            if (activeNow)
            {
                PlayerAssigner.instance.players.Add(AIdata);
                PlayerManager.instance.players.Add(AIdata.player);
                if ((bool)Traverse.Create(PlayerManager.instance).Field("playersShouldBeActive").GetValue()) { AIdata.isPlaying = true; }
            }
            else
            {
                AIdata.player.gameObject.SetActive(false);
                AIdata.player.data.isPlaying = false;
                AIdata.player.data.gameObject.transform.position = Vector3.up * 200f;
                Traverse.Create(AIdata.player.data.playerVel).Field("simulated").SetValue(false);
            }



            return AIdata.player;

        }
        internal static Player SpawnAI(int spawnerID, int teamID, bool activeNow = false, AISkill? skill = null, AIAggression? aggression = null, AI? AItype = null)
        {
            Player spawner = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithID",
                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { spawnerID });

            System.Type AIController = GetAIType(ChooseAIController(skill, aggression, AItype));

            if (PlayerManager.instance.players.Count >= PlayerAssigner.instance.maxPlayers)
            {
                PlayerAssigner.instance.maxPlayers++;
            }

            if (activeNow)
            {
                SoundPlayerStatic.Instance.PlayPlayerAdded();
            }

            Vector3 position = Vector3.up * 100f;
            CharacterData AIdata = PhotonNetwork.Instantiate(PlayerAssigner.instance.playerPrefab.name, position, Quaternion.identity, 0, null).GetComponent<CharacterData>();
            // mark this player as an AI
            AIdata.GetAdditionalData().isAI = true;
            // add the spawner to the AI's data
            AIdata.GetAdditionalData().spawner = spawner;
            // add AI to spawner's data
            spawner.data.GetAdditionalData().minions.Add(AIdata.player);

            AIdata.GetComponent<CharacterData>().SetAI(null);
            UnityEngine.Object.Instantiate<GameObject>(AIBase, AIdata.transform.position, AIdata.transform.rotation, AIdata.transform).AddComponent(AIController);

            AIdata.player.AssignPlayerID(PlayerManager.instance.players.Count);
            AIdata.player.AssignTeamID(teamID);

            if (activeNow)
            {
                PlayerAssigner.instance.players.Add(AIdata);
                PlayerManager.instance.players.Add(AIdata.player);
                if ((bool)Traverse.Create(PlayerManager.instance).Field("playersShouldBeActive").GetValue()) { AIdata.isPlaying = true; }
            }
            else
            {
                AIdata.player.gameObject.SetActive(false);
                AIdata.player.data.isPlaying = false;
                AIdata.player.data.gameObject.transform.position = Vector3.up * 200f;
                Traverse.Create(AIdata.player.data.playerVel).Field("simulated").SetValue(false);
            }


            return AIdata.player;

        }

        internal static Player CreateAIWithStats(int spawnerID, int teamID, AISkill? skill = null, AIAggression? aggression = null, System.Type AItype = null, float? maxHealth = null, ModdingUtils.Extensions.BlockModifier blockStats = null, ModdingUtils.Extensions.GunAmmoStatModifier gunAmmoStats = null, ModdingUtils.Extensions.GunStatModifier gunStats = null, ModdingUtils.Extensions.CharacterStatModifiersModifier characterStats = null, ModdingUtils.Extensions.GravityModifier gravityStats = null, bool activeNow = false)
        {
            Player minion = SpawnAI(spawnerID, teamID, activeNow, skill, aggression, AItype);

            if (maxHealth != null)
            {
                minion.data.maxHealth = (float)maxHealth;
            }
            if (blockStats != null)
            {
                blockStats.ApplyBlockModifier(minion.data.block);
            }
            if (gunAmmoStats != null)
            {
                gunAmmoStats.ApplyGunAmmoStatModifier(minion.GetComponent<Holding>().holdable.GetComponent<Gun>().GetComponentInChildren<GunAmmo>());
            }
            if (gunStats != null)
            {
                gunStats.ApplyGunStatModifier(minion.GetComponent<Holding>().holdable.GetComponent<Gun>());
            }
            if (gravityStats != null)
            {
                gravityStats.ApplyGravityModifier(minion.GetComponent<Gravity>());
            }
            if (characterStats != null)
            {
                characterStats.ApplyCharacterStatModifiersModifier(minion.data.stats);
            }

            return minion;
        }
        internal static Player CreateAIWithStats(int spawnerID, int teamID, AISkill? skill = null, AIAggression? aggression = null, AI? AI = null, float? maxHealth = null, ModdingUtils.Extensions.BlockModifier blockStats = null, ModdingUtils.Extensions.GunAmmoStatModifier gunAmmoStats = null, ModdingUtils.Extensions.GunStatModifier gunStats = null, ModdingUtils.Extensions.CharacterStatModifiersModifier characterStats = null, ModdingUtils.Extensions.GravityModifier gravityStats = null, bool activeNow = false)
        {
            Player minion = SpawnAI(spawnerID, teamID, activeNow, skill, aggression, AI);
            Unbound.Instance.ExecuteAfterSeconds(0.5f, () =>
            {
                if (maxHealth != null)
                {
                    minion.data.maxHealth = (float)maxHealth;
                }
                if (blockStats != null)
                {
                    blockStats.ApplyBlockModifier(minion.data.block);
                }
                if (gunAmmoStats != null)
                {
                    gunAmmoStats.ApplyGunAmmoStatModifier(minion.GetComponent<Holding>().holdable.GetComponent<Gun>().GetComponentInChildren<GunAmmo>());
                }
                if (gunStats != null)
                {
                    gunStats.ApplyGunStatModifier(minion.GetComponent<Holding>().holdable.GetComponent<Gun>());
                }
                if (gravityStats != null)
                {
                    gravityStats.ApplyGravityModifier(minion.GetComponent<Gravity>());
                }
                if (characterStats != null)
                {
                    characterStats.ApplyCharacterStatModifiersModifier(minion.data.stats);
                }
            });
            return minion;
        }

        internal static System.Type GetAIType(AI AI)
        {
            System.Type AIController = null;
            switch (AI)
            {
                case AI.Default:
                    AIController = typeof(PlayerAI);
                    break;
                case AI.David:
                    AIController = typeof(PlayerAIDavid);
                    break;
                case AI.Minion:
                    AIController = typeof(PlayerAIMinion);
                    break;
                case AI.Petter:
                    AIController = typeof(PlayerAIPetter);
                    break;
                case AI.Philip:
                    AIController = typeof(PlayerAIPhilip);
                    break;
                case AI.Wilhelm:
                    AIController = typeof(PlayerAIWilhelm);
                    break;
                case AI.Zorro:
                    AIController = typeof(PlayerAIZorro);
                    break;
            }
            return AIController;
        }

        public enum AISkill
        {
            Beginner,
            Normal,
            Expert
        }
        public enum AIAggression
        {
            Peaceful,
            Normal,
            Aggressive,
            Suicidal
        }
        public enum AI
        {
            Default,
            David,
            Minion,
            Petter,
            Philip,
            Wilhelm,
            Zorro
        }
    }

    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkinColors")]
    class PlayerSkinBankPatchGetPlayersSkinColors
    {
        /*
        private static bool Prefix(int team, ref PlayerSkin __result)
        {
            Player player = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithID",
                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { team });

            if (player.data.GetAdditionalData().isAI && player.data.GetAdditionalData().spawner != null)
            {
                __result = PlayerSkinBank.GetPlayerSkinColors(player.data.GetAdditionalData().spawner.playerID);
                return false;
            }
            return true;
        }*/
        private static void Prefix(ref int team)
        {
            team = team % 4;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkin")]
    class PlayerSkinBankPatchGetPlayerSkin
    {/*
        private static bool Prefix(int team, ref PlayerSkinBank.PlayerSkinInstance __result)
        {
            Player player = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithID",
                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { team });

            if (player.data.GetAdditionalData().isAI && player.data.GetAdditionalData().spawner != null)
            {
                __result = ((PlayerSkinBank)typeof(PlayerSkinBank).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).skins[player.data.GetAdditionalData().spawner.playerID];
                return false;
            }
            return true;
        }*/
        private static void Prefix(ref int team)
        {
            team = team % 4;
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
                __result = PlayerSkinBank.GetPlayerSkinColors(__instance.data.GetAdditionalData().spawner.playerID);
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
                SetTeamColor.TeamColorThis(__instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(__instance.data.GetAdditionalData().spawner.playerID));
                return false;
            }
            return true;
        }
    }
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
    // patch to "fix" PlayerAIPetter
    [Serializable]
    [HarmonyPatch(typeof(PlayerAIPetter), "Update")]
    class PlayerAIPetterPatchUpdate
    {
        private static bool Prefix(PlayerAIPetter __instance)
        {
            if (__instance.aimCurve == null && __instance.GetComponentInParent<PlayerAPI>().GetOtherPlayer() != null)
            {
                if ((double)UnityEngine.Random.Range(0f, 1f) > 0.9)
                {
                    __instance.GetComponentInParent<PlayerAPI>().Move(__instance.GetComponentInParent<PlayerAPI>().TowardsOtherPlayer() * -1f);
                }
                else
                {
                    __instance.GetComponentInParent<PlayerAPI>().Move(__instance.GetComponentInParent<PlayerAPI>().TowardsOtherPlayer());
                }
                if (UnityEngine.Random.Range(0f, 1f) > 0.9)
                {
                    __instance.GetComponentInParent<PlayerAPI>().Jump();
                }
                return false;
            }
            else if (__instance.GetComponentInParent<PlayerAPI>().GetOtherPlayer() == null)
            {
                return false;
            }
            return true;
        }
    }

    // patch to "fix" PlayerAIZorro
    [Serializable]
    [HarmonyPatch(typeof(PlayerAIZorro), "ShootAt")]
    class PlayerAIZorroPatchShootAt
    {
        private static bool Prefix(PlayerAIZorro __instance)
        {
            if (__instance.m_AimCompensastionCurve == null)
            {
                return false;
            }
            return true;
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(PlayerAIZorro), "Update")]
    class PlayerAIZorroPatchUpdate
    {
        private static bool Prefix(PlayerAIZorro __instance)
        {
            if (__instance.GetComponentInParent<PlayerAPI>().GetOtherPlayer() == null)
            {
                return false;
            }
            return true;
        }
    }

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
