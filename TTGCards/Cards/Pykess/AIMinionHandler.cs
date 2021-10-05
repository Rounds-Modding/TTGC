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
    public static class AIMinionHandler
    {
        internal static bool playersCanJoin = true;
        internal const float actionDelay = 2.5f;
        internal static Coroutine EndStalemateRoutine = null;

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
                    _AIBase = new GameObject("AI", typeof(EnableDisablePlayer));
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

        internal static Player GetPlayerOrAIWithID(Player[] players, int ID)
        {
            return players.Where(player => player.playerID == ID).First();
        }
        private static IEnumerator AddAIsToPlayerManager()
        {
            List<Player> playersAndAI = PlayerManager.instance.players.Where(player => !player.data.GetAdditionalData().isAI).ToList();
            foreach (Player player in PlayerManager.instance.players.Where(player => !player.data.GetAdditionalData().isAI))
            {
                playersAndAI.AddRange(player.data.GetAdditionalData().minions);
            }
            playersAndAI = playersAndAI.Distinct().ToList();

            int totPlayers = 0;
            foreach (Player player in PlayerManager.instance.players.Where(player => !player.data.GetAdditionalData().isAI))
            {
                totPlayers += 1 + player.data.GetAdditionalData().minions.Count;
            }
            PlayerManager.instance.players = new List<Player>() { };
            for (int i = 0; i < totPlayers; i++)
            {
                PlayerManager.instance.players.Add(GetPlayerOrAIWithID(playersAndAI.ToArray(), i));
            }

            yield break;
        }
        private static IEnumerator RemoveAIsFromPlayerManager()
        {
            List<Player> players = PlayerManager.instance.players.Where(player => !player.data.GetAdditionalData().isAI).ToList();

            PlayerManager.instance.players = new List<Player>() { };
            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager.instance.players.Add(GetPlayerOrAIWithID(players.ToArray(), i));
            }

            yield break;
        }

        internal static IEnumerator CreateAllAIs(IGameModeHandler gm)
        {
            yield return new WaitUntil(() => PlayerManager.instance.players.All(player => (bool)player.data.playerVel.GetFieldValue("simulated")));
            yield return new WaitForSecondsRealtime(0.1f);
            yield return AddAIsToPlayerManager();
            yield return new WaitForSecondsRealtime(0.1f);

            List<Player> minionsToSpawn = new List<Player>() { };
            List<Vector3> positions = new List<Vector3>() { };
            foreach (Player player in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().minions.Count > 0))
            {
                minionsToSpawn.AddRange(player.data.GetAdditionalData().minions);
                int minionNum = 0;
                foreach (Player minion in player.data.GetAdditionalData().minions)
                {
                    minionNum++;
                    positions.Add(AIMinionHandler.GetMinionSpawnLocation(minion, minionNum));
                }
            }
            yield return new WaitForEndOfFrame();
            foreach (Player minion in minionsToSpawn)
            {
                minion.data.weaponHandler.gun.sinceAttack = -1f;
                minion.data.block.sinceBlock = -1f;
                minion.data.block.counter = -1f;
            }
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < minionsToSpawn.Count; i++)
            {
                try
                {
                    minionsToSpawn[i].GetComponentInChildren<AIMinionHandler.EnableDisablePlayer>().EnablePlayer(positions[i]);
                    minionsToSpawn[i].GetComponentInChildren<AIMinionHandler.EnableDisablePlayer>().ReviveAndSpawn(positions[i]);
                }
                catch
                { }
                //yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSecondsRealtime(0.1f);
            yield break;
        }
        internal static IEnumerator RemoveAllAIs(IGameModeHandler gm)
        {
            List<Player> minionsToRemove = new List<Player>() { };
            foreach (Player player in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().minions.Count > 0))
            {
                minionsToRemove.AddRange(player.data.GetAdditionalData().minions);
            }
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < minionsToRemove.Count; i++)
            {
                minionsToRemove[i].GetComponentInChildren<AIMinionHandler.EnableDisablePlayer>().DisablePlayer();
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSecondsRealtime(0.1f);
            yield return RemoveAIsFromPlayerManager();
            yield return new WaitForSecondsRealtime(0.1f);
            yield break;
        }
        internal static IEnumerator InitPlayerAssigner(IGameModeHandler gm)
        {
            PlayerAssigner.instance.maxPlayers = int.MaxValue;
            yield break;
        }
        internal static IEnumerator SetPlayersCanJoin(bool playersCanJoin)
        {
            AIMinionHandler.playersCanJoin = playersCanJoin;
            yield break;
        }
        internal static IEnumerator StartStalemateHandler(IGameModeHandler gm)
        {
            if (AIMinionHandler.EndStalemateRoutine != null)
            {
                Unbound.Instance.StopCoroutine(AIMinionHandler.EndStalemateRoutine);
            }

            AIMinionHandler.EndStalemateRoutine = Unbound.Instance.StartCoroutine(StalemateHandler());

            yield break;
        }
        private static IEnumerator StalemateHandler()
        {
            // wait until players have properly spawned in
            yield return new WaitForSecondsRealtime(2f);

            // wait until there are only AIs alive, wait a few seconds, then kill them
            while (PlayerManager.instance.players.Where(player => !player.data.GetAdditionalData().isAI && PlayerStatus.PlayerAliveAndSimulated(player)).Any())
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }
            yield return new WaitForSecondsRealtime(10f);

            foreach (Player minion in PlayerManager.instance.players.Where(player => player.data.GetAdditionalData().isAI && PlayerStatus.PlayerAliveAndSimulated(player)).OrderBy(i => UnityEngine.Random.value))
            {
                minion.data.view.RPC("RPCA_Die", RpcTarget.All, new object[]
                {
                    new Vector2(0, 1)
                });
            }
            yield break;
        }

        private readonly static float baseOffset = 2f;
        private readonly static float range = 2f;
        private readonly static float maxProject = 1000f;
        private readonly static float groundOffset = 1f;
        private readonly static float maxDistanceAway = 10f;
        private readonly static int maxAttempts = 1000;
        private static LayerMask groundMask = (LayerMask)LayerMask.GetMask(new string[] { "Default" });
        private static Vector3 WorldToScreenPos(Vector3 pos)
        {
            Vector3 screenpos = MainCam.instance.transform.GetComponent<Camera>().WorldToScreenPoint(new Vector3(pos.x, pos.y, 0f));

            screenpos.x /= (float)Screen.width;
            screenpos.y /= (float)Screen.height;

            return screenpos;
        }
        private static Vector3 ScreenToWorldPos(Vector3 pos)
        {
            return MainCam.instance.transform.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(pos.x * (float)Screen.width, pos.y * (float)Screen.height, pos.z));
        }
        private static Vector2 CastToGround(Vector2 position)
        {
            RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.down, maxProject);
            if (!raycastHit2D.transform)
            {
                return position;
            }
            return position + Vector2.down * (raycastHit2D.distance - groundOffset);
        }
        private static bool IsValidPosition(Vector2 position)
        {
            RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.down, range);
            return raycastHit2D.transform && raycastHit2D.distance > 0.1f;
        }
        private static Vector2 GetNearbyValidPosition(Vector2 position)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 offset = maxDistanceAway*UnityEngine.Random.insideUnitCircle;
                if (IsValidPosition(position+offset))
                {
                    return CastToGround(position + offset);
                }
            }
            return RandomValidPosition();
        }
        private static Vector2 RandomValidPosition()
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 position = ScreenToWorldPos(new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)));
                if (IsValidPosition(position)) { return CastToGround(position); }
            }
            return Vector2.zero;
        }
        internal static Vector2 GetMinionSpawnLocation(Player minion, int minionNum)
        {
            switch (minion.data.GetAdditionalData().spawnLocation)
            {
                case SpawnLocation.Center:
                    {
                        return GetNearbyValidPosition(CastToGround(ScreenToWorldPos(new Vector2(0.5f, 0.5f))));
                    }
                case SpawnLocation.Center_High:
                    {
                        return GetNearbyValidPosition(CastToGround(ScreenToWorldPos(new Vector2(0.5f, 1f))));
                    }
                case SpawnLocation.Center_Low:
                    {
                        return GetNearbyValidPosition(CastToGround(ScreenToWorldPos(new Vector2(0.5f, 0.33f))));
                    }
                case SpawnLocation.Owner_Random:
                    {
                        return GetNearbyValidPosition(minion.data.GetAdditionalData().spawner.transform.position);
                    }
                case SpawnLocation.Enemy_Random:
                    {
                        Player enemy = PlayerStatus.GetRandomEnemyPlayer(minion);
                        if (enemy == null)
                        {
                            return RandomValidPosition();
                        }
                        return GetNearbyValidPosition(enemy.transform.position);
                    }
                case SpawnLocation.Random:
                    {
                        return RandomValidPosition();
                    }
                case SpawnLocation.Owner_Front:
                    {
                        return GetNearbyValidPosition(CastToGround(minion.data.GetAdditionalData().spawner.transform.position - minionNum * baseOffset * new Vector3(UnityEngine.Mathf.Sign(minion.data.GetAdditionalData().spawner.transform.position.x), 0f, 0f)));
                    }
                case SpawnLocation.Owner_Back:
                    {
                        return GetNearbyValidPosition(CastToGround(minion.data.GetAdditionalData().spawner.transform.position + minionNum * baseOffset * new Vector3(UnityEngine.Mathf.Sign(minion.data.GetAdditionalData().spawner.transform.position.x), 0f, 0f)));
                    }
                case SpawnLocation.Enemy_Front:
                    {
                        Player enemy = PlayerStatus.GetRandomEnemyPlayer(minion);
                        if (enemy == null)
                        {
                            return RandomValidPosition();
                        }
                        return GetNearbyValidPosition(CastToGround(enemy.transform.position - minionNum * baseOffset * new Vector3(UnityEngine.Mathf.Sign(enemy.transform.position.x), 0f, 0f)));
                    }
                case SpawnLocation.Enemy_Back:
                    {
                        Player enemy = PlayerStatus.GetRandomEnemyPlayer(minion);
                        if (enemy == null)
                        {
                            return RandomValidPosition();
                        }
                        return GetNearbyValidPosition(CastToGround(enemy.transform.position + minionNum * baseOffset * new Vector3(UnityEngine.Mathf.Sign(enemy.transform.position.x), 0f, 0f)));
                    }
            }
            return RandomValidPosition();

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
                if (AIMinionHandler.sandbox) { Destroy(this); }
            }
            internal void DisablePlayer()
            {

                if (this.player == null) { return; }

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
        internal static Player SpawnAI(int newID, int spawnerID, int teamID, int actorID, bool activeNow = false, AISkill skill = AISkill.None, AIAggression aggression = AIAggression.None, AI AItype = AI.None, SpawnLocation spawnLocation = SpawnLocation.Owner_Random, float? maxHealth = null)
        {

            if (activeNow)
            {
                SoundPlayerStatic.Instance.PlayPlayerAdded();
            }

            Vector3 position = Vector3.up * 100f;
            CharacterData AIdata = PhotonNetwork.Instantiate(PlayerAssigner.instance.playerPrefab.name, position, Quaternion.identity, 0, null).GetComponent<CharacterData>();

            NetworkingManager.RPC(typeof(AIMinionHandler), nameof(RPCA_SetupAI), new object[] { newID, AIdata.view.ViewID, actorID, spawnerID, teamID, activeNow, (byte)skill, (byte)aggression, (byte)AItype, (byte)spawnLocation, maxHealth });

            return AIdata.player;

        }
        [UnboundRPC]
        private static void RPCA_SetupAI(int newID, int viewID, int spawnerActorID, int spawnerPlayerID, int teamID, bool activeNow, byte aiskill, byte aiaggression, byte ai, byte aispawnlocation, float? maxHealth)
        {

            AISkill skill = (AISkill)aiskill;
            AIAggression aggression = (AIAggression)aiaggression;
            AI AItype = (AI)ai;
            SpawnLocation spawnLocation = (SpawnLocation)aispawnlocation;

            Player spawner = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(spawnerActorID, spawnerPlayerID);
            GameObject AIplayer = PhotonView.Find(viewID).gameObject;
            CharacterData AIdata = AIplayer.GetComponent<CharacterData>();
            // mark this player as an AI
            AIdata.GetAdditionalData().isAI = true;
            // add the spawner to the AI's data
            AIdata.GetAdditionalData().spawner = spawner;
            // set spawn location
            AIdata.GetAdditionalData().spawnLocation = spawnLocation;
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
            
            /*
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
            }, 5f));
            */
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

        internal static void CreateAIWithStats(bool IsMine, int spawnerID, int teamID, int actorID, AISkill skill = AISkill.None, AIAggression aggression = AIAggression.None, AI AItype = AI.None, float? maxHealth = null, ModdingUtils.Extensions.BlockModifier blockStats = null, ModdingUtils.Extensions.GunAmmoStatModifier gunAmmoStats = null, ModdingUtils.Extensions.GunStatModifier gunStats = null, ModdingUtils.Extensions.CharacterStatModifiersModifier characterStats = null, ModdingUtils.Extensions.GravityModifier gravityStats = null, List<System.Type> effects = null, List<CardInfo> cards = null, bool cardsAreReassigned = false, SpawnLocation spawnLocation = SpawnLocation.Owner_Random, int eyeID = 0, Vector2 eyeOffset = default(Vector2), int mouthID = 0, Vector2 mouthOffset = default(Vector2), int detailID = 0, Vector2 detailOffset = default(Vector2), int detail2ID = 0, Vector2 detail2Offset = default(Vector2), bool activeNow = false, Func<int, int, IEnumerator> Finalizer = null, Action<int, int> Callback = null)
        {
            int newID = GetNextMinionID();

            if (IsMine)
            {
                Unbound.Instance.StartCoroutine(SpawnAIAfterDelay(0.1f, newID, spawnerID, teamID, actorID, activeNow, skill, aggression, AItype, spawnLocation, maxHealth));
                

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
                NetworkingManager.RPC(typeof(AIMinionHandler), nameof(RPCA_SetFace), new object[] { minion.data.view.ViewID, eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset });
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
            NetworkingManager.RPC(typeof(AIMinionHandler), nameof(RPCA_AddCardsToAI), new object[] { minionID, actorID, cardNames, reassign});
            
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

        private static IEnumerator SpawnAIAfterDelay(float delay, int newID, int spawnerID, int teamID, int actorID, bool activeNow, AISkill skill, AIAggression aggression, AI AItype, SpawnLocation spawnLocation, float? maxHealth)
        {
            yield return new WaitForSecondsRealtime(delay);
            Player minion = SpawnAI(newID, spawnerID, teamID, actorID, activeNow, skill, aggression, AItype, spawnLocation, maxHealth);
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

        public enum SpawnLocation
        {
            Center,
            Center_High,
            Center_Low,
            Owner_Random,
            Enemy_Random,
            Random,
            Owner_Front,
            Owner_Back,
            Enemy_Front,
            Enemy_Back
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

    static class TimeSinceBattleStart
    {
        static float startTime = -1f;
        
        public static float timeSince
        {
            get { return Time.time - startTime; }
            private set { }
        }

        public static void ResetTimer()
        {
            startTime = Time.time;
        }

        internal static IEnumerator BattleStart(IGameModeHandler gm)
        {
            startTime = Time.time;
            yield break;
        }

    }

    // force AI to not shoot/block for first few seconds of battle start
    [Serializable]
    [HarmonyPatch(typeof(Gun),"Attack")]
    class GunPatchAttack
    {
        private static bool Prefix(Gun __instance)
        {
            if (__instance.player.data.GetAdditionalData().isAI && TimeSinceBattleStart.timeSince <= AIMinionHandler.actionDelay) 
            {
                return false; 
            }
            else { return true; }
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(Block), "TryBlock")]
    class BlockPatchTryBlock
    {
        private static bool Prefix(Block __instance)
        {
            if (((CharacterData)__instance.GetFieldValue("data")).GetAdditionalData().isAI && TimeSinceBattleStart.timeSince <= AIMinionHandler.actionDelay)
            {
                return false; 
            }
            else { return true; }
        }
    }





}
