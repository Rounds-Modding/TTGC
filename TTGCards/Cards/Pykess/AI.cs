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
                    _AIBase = new GameObject("AI", typeof(EnableDisablePlayer), typeof(EndStalemate));
                    return _AIBase;
                }
            }
            set { }
        }
        private static int NextAvailablePlayerID
        {
            get
            {
                int ID = 0;
                ID += PlayerManager.instance.players.Count;
                foreach (Player player in PlayerManager.instance.players)
                {
                    ID += player.data.GetAdditionalData().minions.Count;
                }
                return ID;
            }
            set { }
        }

        private class EndStalemate : MonoBehaviour
        {
            // if only AI players remain, give them 10 seconds to duke it out, after that switch their AI to suicidal, then give them another 10 seconds before just killing them
            private readonly float maxTime = 20f;
            private readonly float updateDelay = 1f;
            private float startTime;
            private float updateTime;
            private bool onlyAI = false;
            //private bool suicidal = false;
            private bool killed = false;
            void Start()
            {
                ResetUpdate();
                ResetTimer();
            }
            internal void OnEnable()
            {
                this.onlyAI = false;
                //this.suicidal = false;
                this.killed = false;
                ResetUpdate();
                ResetTimer();
            }
            internal void OnDisable()
            {
                this.onlyAI = false;
                //this.suicidal = false;
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
                /*
                if (onlyAI && !suicidal && Time.time >= this.startTime + this.maxTimeToAIChange)
                {
                    this.suicidal = true;
                    foreach (Player player in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().isAI))
                    {
                        player.gameObject.AddComponent<RestoreAIControllerOnDeath>();
                        //Unbound.Instance.ExecuteAfterSeconds(1f, () =>
                        //{
                            ChangeAIController(player, AIPlayer.ChooseAIController(aggression: AIAggression.Suicidal, AItype: AI.None));
                        //});
                    }
                }*/

                if (!killed && onlyAI && Time.time >= this.startTime + this.maxTime)
                {
                    ResetTimer();
                    this.killed = true;
                    // kill ALL AI players
                    foreach (Player AIPlayer in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().isAI))
                    {
                        
                        //Unbound.Instance.ExecuteAfterSeconds(1f, delegate
                        //{
                            AIPlayer.data.view.RPC("RPCA_Die", RpcTarget.All, new object[]
                            {
                                    new Vector2(0, 1)
                            });

                        //});
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
            internal void DisablePlayer()
            {

                if (this.player == null) { return; }

                this.player.GetComponentInChildren<EndStalemate>().OnDisable();

                this.player.data.isPlaying = false;
                Traverse.Create(this.player.data.playerVel).Field("simulated").SetValue(false);

                
                Unbound.Instance.ExecuteAfterSeconds(1f, () =>
                {
                    this.gameObject.transform.parent.gameObject.SetActive(false);
                    this.player.data.gameObject.transform.position = Vector3.up * 200f;
                });
            }
            internal void EnablePlayer(Vector3? pos = null)
            {
                Vector3 Pos = pos ?? Vector3.zero;

                this.player.GetComponentInChildren<EndStalemate>().OnEnable();
                this.player.data.weaponHandler.gun.sinceAttack = -1f;
                this.player.data.block.sinceBlock = -1f;
                this.player.data.block.counter = -1f;

                this.gameObject.transform.parent.gameObject.SetActive(true);
                this.player.data.isPlaying = true;
                this.player.data.gameObject.transform.position = Pos;
                NetworkingManager.RPC(typeof(EnableDisablePlayer), nameof(RPCA_Teleport), new object[] { Pos, this.player.data.view.ControllerActorNr, this.player.playerID });
                Traverse.Create(this.player.data.playerVel).Field("simulated").SetValue(true);
            }
            internal void ReviveAndSpawn(Vector3? pos = null, bool isFullRevive = true)
            {
                Vector3 Pos = pos ?? Vector3.zero;

                this.player.GetComponentInChildren<EndStalemate>().OnEnable();
                this.player.GetComponent<GeneralInput>().enabled = true;
                this.player.data.gameObject.transform.position = Pos;
                NetworkingManager.RPC(typeof(EnableDisablePlayer), nameof(RPCA_Teleport), new object[] { Pos, this.player.data.view.ControllerActorNr, this.player.playerID });
                SoundManager.Instance.Play(PlayerManager.instance.soundCharacterSpawn[0], this.player.transform);
                this.player.data.healthHandler.Revive(isFullRevive);
            }
            [UnboundRPC]
            private static void RPCA_Teleport(Vector3 pos, int actorID, int playerID)
            {
                Player player = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(actorID, playerID);
                player.GetComponentInParent<PlayerCollision>().IgnoreWallForFrames(2);
                player.transform.position = pos;
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
        internal static System.Type ChooseAIController(AISkill skill = AISkill.None, AIAggression aggression = AIAggression.None, System.Type AItype = null)
        {
            System.Type AIController = typeof(PlayerAI);

            if (skill != AISkill.None)
            {
                AIController = GetAIType(ChooseAIController(skill, aggression, AI.None));
            }
            else if (aggression != AIAggression.None)
            {
                AIController = GetAIType(ChooseAIController(skill, aggression, AI.None));
            }
            else if (AItype != null)
            {
                AIController = AItype;
            }

            return AIController;
        }
        internal static AI ChooseAIController(AISkill skill = AISkill.None, AIAggression aggression = AIAggression.None, AI AItype = AI.None)
        {
            AI AIController = AI.Default;

            if (skill != AISkill.None)
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
            else if (aggression != AIAggression.None)
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
            else if (AItype != AI.None)
            {
                AIController = (AI)AItype;
            }

            return AIController;
        }
        internal static int GetNextMinionID()
        {
            int ID = 0;

            foreach (Player player in PlayerManager.instance.players.Where(player => !player.data.GetAdditionalData().isAI))
            {
                ID += 1 + player.data.GetAdditionalData().minions.Count;
            }

            return ID;
        }
        internal static Player SpawnAI(int newID, int spawnerID, int teamID, int actorID, bool activeNow = false, AISkill skill = AISkill.None, AIAggression aggression = AIAggression.None, AI AItype = AI.None, float? maxHealth = null)
        {

            if (activeNow)
            {
                SoundPlayerStatic.Instance.PlayPlayerAdded();
            }

            Vector3 position = Vector3.up * 100f;
            CharacterData AIdata = PhotonNetwork.Instantiate(PlayerAssigner.instance.playerPrefab.name, position, Quaternion.identity, 0, null).GetComponent<CharacterData>();

            NetworkingManager.RPC(typeof(AIPlayer), nameof(RPCA_SetupAI), new object[] { newID, AIdata.view.ViewID, actorID, spawnerID, teamID, activeNow, (byte)skill, (byte)aggression, (byte)AItype, maxHealth });

            return AIdata.player;

        }
        [UnboundRPC]
        private static void RPCA_SetupAI(int newID, int viewID, int spawnerActorID, int spawnerPlayerID, int teamID, bool activeNow, byte aiskill, byte aiaggression, byte ai, float? maxHealth)
        {

            AISkill skill = (AISkill)aiskill;
            AIAggression aggression = (AIAggression)aiaggression;
            AI AItype = (AI)ai;

            Player spawner = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(spawnerActorID, spawnerPlayerID);
            GameObject AIplayer = PhotonView.Find(viewID).gameObject;
            CharacterData AIdata = AIplayer.GetComponent<CharacterData>();
            // mark this player as an AI
            AIdata.GetAdditionalData().isAI = true;
            // add the spawner to the AI's data
            AIdata.GetAdditionalData().spawner = spawner;
            // add AI to spawner's data
            spawner.data.GetAdditionalData().minions.Add(AIdata.player);
            // set maxHealth
            if (maxHealth != null)
            {
                AIdata.maxHealth = (float)maxHealth;
            }


            AIdata.GetComponent<CharacterData>().SetAI(null);
            
            System.Type AIController = GetAIType(ChooseAIController(skill, aggression, AItype));
            Component aicontroller = UnityEngine.Object.Instantiate<GameObject>(AIBase, AIdata.transform.position, AIdata.transform.rotation, AIdata.transform).AddComponent(AIController);
            if (!AIdata.view.IsMine)
            {
                // if another player created this AI, then make sure it's AI controller is removed and remove this player from PlayerManager
                UnityEngine.GameObject.Destroy(aicontroller);
                List<Player> playersToRemove = new List<Player>() { };
                foreach (Player playerToRemove in PlayerManager.instance.players.Where(player => player.playerID == newID && player.data.view.ControllerActorNr == spawnerActorID))
                {
                    playersToRemove.Add(playerToRemove);
                }
                foreach (Player playerToRemove in playersToRemove)
                {
                    PlayerManager.instance.players.Remove(playerToRemove);
                }
            }

            AIdata.player.AssignPlayerID(newID);
            PlayerAssigner.instance.players.Add(AIdata);
            AIdata.player.AssignTeamID(teamID);
            

            Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(newID, AIdata.view.ControllerActorNr, (mID, aID) =>
            {
                Player minion = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(aID, mID);

                // set the player skin correctly
                GameObject AISkinOrig = minion.GetTeamColors().gameObject;
                GameObject AISkinSlot = minion.GetComponentInChildren<PlayerSkin>().gameObject.transform.parent.gameObject;
                UnityEngine.GameObject.Destroy(AISkinSlot.transform.GetChild(0).gameObject);
                GameObject AISkin = UnityEngine.GameObject.Instantiate(AISkinOrig, AISkinSlot.transform);
                AISkin.GetComponent<PlayerSkinParticle>().Init(703124351);

                minion.GetComponentInChildren<PlayerSkinHandler>().SetFieldValue("skins", new PlayerSkinParticle[] { AISkin.GetComponent<PlayerSkinParticle>() });
            }, 1f));
            
            if (activeNow)
            {
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
        }

        internal static void CreateAIWithStats(bool IsMine, int spawnerID, int teamID, int actorID, AISkill skill = AISkill.None, AIAggression aggression = AIAggression.None, AI AItype = AI.None, float? maxHealth = null, ModdingUtils.Extensions.BlockModifier blockStats = null, ModdingUtils.Extensions.GunAmmoStatModifier gunAmmoStats = null, ModdingUtils.Extensions.GunStatModifier gunStats = null, ModdingUtils.Extensions.CharacterStatModifiersModifier characterStats = null, ModdingUtils.Extensions.GravityModifier gravityStats = null, List<System.Type> effects = null, List<CardInfo> cards = null, bool cardsAreReassigned = false, int eyeID = 0, Vector2 eyeOffset = default(Vector2), int mouthID = 0, Vector2 mouthOffset = default(Vector2), int detailID = 0, Vector2 detailOffset = default(Vector2), int detail2ID = 0, Vector2 detail2Offset = default(Vector2), bool activeNow = false, Func<int, int, IEnumerator> Finalizer = null, Action<int, int> Callback = null)
        {
            int newID = GetNextMinionID();

            if (IsMine)
            {
                Unbound.Instance.StartCoroutine(SpawnAIAfterDelay(0.1f, newID, spawnerID, teamID, actorID, activeNow, skill, aggression, AItype, maxHealth));
                

                // delay the add cards request so that it happens after the pick phase
                Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(newID, actorID, (mID, aID) => AskHostToAddCards(mID, aID, cards, cardsAreReassigned)));
                Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(newID, actorID, (mID, aID) => SetFace(mID, aID, eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset)));
            }


            Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(newID, actorID, (mID, aID) => ApplyStatsWhenReady(mID, aID, blockStats, gunAmmoStats, gunStats, characterStats, gravityStats, effects)));

            if (Finalizer != null)
            {
                Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(newID, actorID, Finalizer));
            }

            if (Callback != null)
            {

                Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(newID, actorID, Callback));
            }


            return;
        }
        internal static IEnumerator ExecuteWhenAIIsReady(int minionID, int actorID, Func<int, int, IEnumerator> action, float delay = 0.1f)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitUntil(() =>
            {
                Player minion = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(actorID, minionID);

                return (minion != null && PlayerManager.instance.players.Contains(minion) && minion.gameObject.activeSelf && PlayerStatus.PlayerAliveAndSimulated(minion));
            });
            yield return new WaitForSecondsRealtime(delay);

            yield return action(minionID, actorID);
            yield break;
        }
        private static IEnumerator ExecuteWhenAIIsReady(int minionID, int actorID, Action<int, int> action, float delay = 0.1f)
        {
            IEnumerator ActionEnum(int minionID, int actorID)
            {
                action(minionID, actorID);
                yield break;
            }
            yield return ExecuteWhenAIIsReady(minionID, actorID, ActionEnum, delay);
            yield break;
        }
        private static IEnumerator SetFace(int minionID, int actorID, int eyeID = 0, Vector2 eyeOffset = default(Vector2), int mouthID = 0, Vector2 mouthOffset = default(Vector2), int detailID = 0, Vector2 detailOffset = default(Vector2), int detail2ID = 0, Vector2 detail2Offset = default(Vector2))
        {
            Unbound.Instance.ExecuteAfterSeconds(0.5f, () =>
            {
                Player minion = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(actorID, minionID);
                NetworkingManager.RPC(typeof(AIPlayer), nameof(RPCA_SetFace), new object[] { minion.data.view.ViewID, eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset });
            });
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_SetFace(int viewID, int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
        {
            if (PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode)
            {
                PhotonView playerView = PhotonView.Find(viewID);
                playerView.RPC("RPCA_SetFace", RpcTarget.All, new object[] { eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset });
            }
        }
        private static IEnumerator AskHostToAddCards(int minionID, int actorID, List<CardInfo> cards, bool reassign)
        {
            
            if (cards == null || cards.Count == 0)
            {
                
                yield break;
            }

            // if there are valid cards, then have the host add them
            string[] cardNames = cards.Select(card => card.cardName).ToArray();
            NetworkingManager.RPC(typeof(AIPlayer), nameof(RPCA_AddCardsToAI), new object[] { minionID, actorID, cardNames, reassign});
            
            yield break;
        }

        [UnboundRPC]
        private static void RPCA_AddCardsToAI(int minionID, int actorID, string[] cardNames, bool reassign)
        {
            if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Unbound.Instance.StartCoroutine(ExecuteWhenAIIsReady(minionID, actorID, (mID, aID) => HostAddCardsToAIWhenReady(mID, aID, cardNames, reassign)));

        }
        private static IEnumerator HostAddCardsToAIWhenReady(int minionID, int actorID, string[] cardNames, bool reassign)
        {

            Player minion = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(actorID, minionID);

            // finally, add the cards to the AI
            CardInfo[] cards = cardNames.Select(card => ModdingUtils.Utils.Cards.instance.GetCardWithName(card)).ToArray();
            ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(minion, cards, reassign: reassign, addToCardBar: false);

            yield break;
        }

        private static IEnumerator SpawnAIAfterDelay(float delay, int newID, int spawnerID, int teamID, int actorID, bool activeNow, AISkill skill, AIAggression aggression, AI AItype, float? maxHealth)
        {
            yield return new WaitForSecondsRealtime(delay);
            Player minion = SpawnAI(newID, spawnerID, teamID, actorID, activeNow, skill, aggression, AItype, maxHealth);
            yield break;

        }

        private static IEnumerator ApplyStatsWhenReady(int minionID, int actorID, ModdingUtils.Extensions.BlockModifier blockStats = null, ModdingUtils.Extensions.GunAmmoStatModifier gunAmmoStats = null, ModdingUtils.Extensions.GunStatModifier gunStats = null, ModdingUtils.Extensions.CharacterStatModifiersModifier characterStats = null, ModdingUtils.Extensions.GravityModifier gravityStats = null, List<System.Type> effects = null)
        {
            Player minion = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(actorID, minionID);
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
            if (effects != null)
            {
                foreach (System.Type effect in effects)
                {
                    minion.gameObject.AddComponent(effect);
                }
            }
            yield break;
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
            None,
            Beginner,
            Normal,
            Expert
        }
        public enum AIAggression
        {
            None,
            Peaceful,
            Normal,
            Aggressive,
            Suicidal
        }
        public enum AI
        {
            None,
            Default,
            David,
            Minion,
            Petter,
            Philip,
            Wilhelm,
            Zorro
        }
    }
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
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "AssignPlayerID")]
    class PlayerPatchAssignPlayerID
    {
        private static bool Prefix(Player __instance, int ID)
        {
            __instance.playerID = ID;
            __instance.SetColors();
            return MinionCardBase.playersCanJoin;
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
            return MinionCardBase.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "ReadPlayerID")]
    class PlayerPatchReadPlayerID
    {
        private static bool Prefix(Player __instance)
        {
            return MinionCardBase.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "ReadTeamID")]
    class PlayerPatchReadTeamID
    {
        private static bool Prefix(Player __instance)
        {
            return MinionCardBase.playersCanJoin;
        }
    }
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
            return MinionCardBase.playersCanJoin;
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
                return MinionCardBase.playersCanJoin;
            }
            else
            {
                return true;
            }
        }
    }

    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkinColors")]
    class PlayerSkinBankPatchGetPlayersSkinColors
    {
        private static void Prefix(ref int team)
        {
            team = team % 4;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkin")]
    class PlayerSkinBankPatchGetPlayerSkin
    {
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
                __result = __instance.data.GetAdditionalData().spawner.GetTeamColors();
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
