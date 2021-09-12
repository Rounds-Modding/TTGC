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
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using System;

namespace TTGC.Cards
{
    public abstract class MinionCardBase : CustomCard
    {
        public static CardCategory category = CustomCardCategories.instance.CardCategory("AIMinion");
        public virtual ModdingUtils.Extensions.BlockModifier GetBlockStats(Player player)
        { return null; }
        public virtual ModdingUtils.Extensions.GunAmmoStatModifier GetGunAmmoStats(Player player)
        { return null; }
        public virtual ModdingUtils.Extensions.GunStatModifier GetGunStats(Player player)
        { return null; }
        public virtual ModdingUtils.Extensions.CharacterStatModifiersModifier GetCharacterStats(Player player)
        { return null; }
        public virtual ModdingUtils.Extensions.GravityModifier GetGravityModifier(Player player)
        { return null; }
        public virtual float? GetMaxHealth(Player player)
        { return null; }
        public virtual List<CardInfo> GetCards(Player player)
        { return null; }
        public virtual bool CardsAreReassigned(Player player)
        { return false; }
        public virtual AIPlayer.AISkill GetAISkill(Player player)
        { return AIPlayer.AISkill.None; }
        public virtual AIPlayer.AIAggression GetAIAggression(Player player)
        { return AIPlayer.AIAggression.None; }
        public virtual AIPlayer.AI GetAI(Player player)
        { return AIPlayer.AI.None; }
        public virtual Color GetBandanaColor(Player player)
        {
            return new Color(0.3679f, 0.2169f, 0.2169f, 1f);
        }
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.categories = cardInfo.categories.Concat(new CardCategory[] { MinionCardBase.category }).ToArray();
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (!this.gameObject.GetOrAddComponent<PhotonView>().IsMine)
            {
                return;
            }

            // AIs can't have AIs
            if (player.data.GetAdditionalData().isAI)
            {
                return;
            }

            // add AI player
            Player minion = AIPlayer.CreateAIWithStats(player.playerID, player.teamID, player.data.view.ControllerActorNr, GetAISkill(player), GetAIAggression(player), GetAI(player), GetMaxHealth(player), GetBlockStats(player), GetGunAmmoStats(player), GetGunStats(player), GetCharacterStats(player), GetGravityModifier(player), AIPlayer.sandbox);
            //Player minion = AIPlayer.CreateAIWithStats(player.playerID, player.teamID, player.data.view.ControllerActorNr, null,null, typeof(CustomAI), GetMaxHealth(player), GetBlockStats(player), GetGunAmmoStats(player), GetGunStats(player), GetCharacterStats(player), GetGravityModifier(player), AIPlayer.sandbox);

            Unbound.Instance.StartCoroutine(AddCardsWhenReady(minion, player));

            Unbound.Instance.ExecuteAfterSeconds(0.5f, () =>
            {
                UnityEngine.Debug.Log("TOTAL PLAYERS IN PLAYERMANAGER: " + PlayerManager.instance.players.Count.ToString());
                UnityEngine.Debug.Log("TOTAL PLAYERS IN PLAYERASSIGNER: " + PlayerAssigner.instance.players.Count.ToString());

                minion.data.view.RPC("RPCA_SetFace", RpcTarget.All, new object[] { 63, new Vector2(0f, -0.5f), 19, new Vector2(0f, -0.5f), 14, new Vector2(0f, 1.1f), 0, new Vector2(0f, 0f) });

                minion.gameObject.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.gameObject.name.Contains("P_A_X6")).First().color = GetBandanaColor(player);

            });

        }
        public override void OnRemoveCard()
        {
        }

        public override string GetModName()
        {
            return "TTGC";
        }
        internal static void callback(CardInfo card)
        {
            card.gameObject.AddComponent<ExtraName>();
        }

        private IEnumerator AddCardsWhenReady(Player minion, Player spawner)
        {
            if (GetCards(spawner).Where(card => !card.categories.Contains(MinionCardBase.category)).ToArray().Length < 1)
            {
                yield break;
            }
            // wait until the player is added to the list of players, then add requisite cards
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitUntil(() => PlayerManager.instance.players.Contains(minion) && minion.gameObject.activeSelf && !minion.data.dead);
            yield return new WaitForSecondsRealtime(0.5f);
            ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(minion, GetCards(spawner).Where(card => !card.categories.Contains(MinionCardBase.category)).ToArray(), CardsAreReassigned(spawner), addToCardBar: false);

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

        private static float baseOffset = 0.5f;
        
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
                    positions.Add(player.gameObject.transform.position - minionNum * baseOffset * new Vector3(UnityEngine.Mathf.Sign(player.gameObject.transform.position.x), 0f, 0f));
                }
            }
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < minionsToSpawn.Count; i++)
            {
                try
                {
                    minionsToSpawn[i].GetComponentInChildren<AIPlayer.EnableDisablePlayer>().Enable(positions[i]);
                    minionsToSpawn[i].GetComponentInChildren<AIPlayer.EnableDisablePlayer>().ReviveAndSpawn(positions[i]);
                }
                catch
                { }
                yield return new WaitForEndOfFrame();
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
                minionsToRemove[i].GetComponentInChildren<AIPlayer.EnableDisablePlayer>().Disable();
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

    }

    // destroy object once its no longer a child
    public class DestroyOnUnparent : MonoBehaviour
    {
        void LateUpdate()
        {
            if (this.gameObject.transform.parent == null) { Destroy(this.gameObject); }
        }
    }
    internal class ExtraName : MonoBehaviour
    {
        
        private void Start()
        {
            // add extra text to bottom right
            // create blank object for text, and attach it to the canvas
            // find bottom right edge object
            RectTransform[] allChildrenRecursive = this.gameObject.GetComponentsInChildren<RectTransform>();
            GameObject BottomLeftCorner = allChildrenRecursive.Where(obj => obj.gameObject.name == "EdgePart (1)").FirstOrDefault().gameObject;
            GameObject modNameObj = UnityEngine.GameObject.Instantiate(new GameObject("ExtraCardText", typeof(TextMeshProUGUI), typeof(DestroyOnUnparent)), BottomLeftCorner.transform.position, BottomLeftCorner.transform.rotation, BottomLeftCorner.transform);
            TextMeshProUGUI modText = modNameObj.gameObject.GetComponent<TextMeshProUGUI>();
            modText.text = "Pykess";
            modText.enableWordWrapping = false;
            modNameObj.transform.Rotate(0f, 0f, 135f);
            modNameObj.transform.localScale = new Vector3(1f, 1f, 1f);
            modNameObj.transform.localPosition = new Vector3(-50f, -50f, 0f);
            modText.alignment = TextAlignmentOptions.Bottom;
            modText.alpha = 0.1f;
            modText.fontSize = 50;
        }
    }
}
