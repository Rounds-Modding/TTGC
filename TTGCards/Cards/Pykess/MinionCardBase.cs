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

        internal static bool AIsDoneSpawning = true;

        private const float delayBetweenSpawns = 0.5f;

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
        public virtual AIMinionHandler.SpawnLocation GetAISpawnLocation(Player player)
        { return AIMinionHandler.SpawnLocation.Owner_Random; }

        public virtual AIMinionHandler.AISkill GetAISkill(Player player)
        { return AIMinionHandler.AISkill.None; }
        public virtual AIMinionHandler.AIAggression GetAIAggression(Player player)
        { return AIMinionHandler.AIAggression.None; }
        public virtual AIMinionHandler.AI GetAI(Player player)
        { return AIMinionHandler.AI.None; }
        public virtual Color GetBandanaColor(Player player)
        {
            return new Color(0.3679f, 0.2169f, 0.2169f, 1f);
        }
        public virtual int GetNumberOfMinions(Player player)
        {
            return 1;
        }
        public virtual List<System.Type> GetEffects(Player player)
        { return new List<System.Type>() { }; }
        private protected List<CardInfo> GetValidCards(Player player)
        {
            // Certain AI cards can cause infinite recursion (e.g. Mirror & Doppleganger) and so are not valid for AIs to have

            if (GetCards(player) == null || !GetCards(player).Where(card => !card.categories.Contains(MinionCardBase.category)).Any()) { return new List<CardInfo>() { }; }
            else { return GetCards(player).Where(card => !card.categories.Contains(MinionCardBase.category)).ToList(); }
        }
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.categories = cardInfo.categories.Concat(new CardCategory[] { MinionCardBase.category }).ToArray();
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // AIs can't have AIs
            if (player.data.GetAdditionalData().isAI)
            {
                return;
            }

            AIsDoneSpawning = false;

            Unbound.Instance.StartCoroutine(SpawnAIs(GetNumberOfMinions(player), delayBetweenSpawns, player, gun, gunAmmo, data, health, gravity, block, characterStats));
            Unbound.Instance.ExecuteAfterSeconds(GetNumberOfMinions(player) * delayBetweenSpawns + 1f, () => { AIsDoneSpawning = true; });
        }
        private IEnumerator SpawnAIs(int N, float delay, Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            for (int i = 0; i < N; i++)
            {
                AIMinionHandler.CreateAIWithStats(player.data.view.IsMine, player.playerID, player.teamID, player.data.view.ControllerActorNr, GetAISkill(player), GetAIAggression(player), GetAI(player), GetMaxHealth(player), GetBlockStats(player), GetGunAmmoStats(player), GetGunStats(player), GetCharacterStats(player), GetGravityModifier(player), GetEffects(player), GetValidCards(player), CardsAreReassigned(player), GetAISpawnLocation(player), 63, new Vector2(0f, -0.5f), 19, new Vector2(0f, -0.5f), 14, new Vector2(0f, 1.1f), 0, new Vector2(0f, 0f), AIMinionHandler.sandbox, Finalizer: (mID, aID) => SetBandanaColor(mID, aID, GetBandanaColor(player)));
                yield return new WaitForSecondsRealtime(delay);
            }
            yield break;
        }

        private static IEnumerator SetBandanaColor(int minionID, int actorID, Color bandanaColor)
        {
            Player minion = ModdingUtils.Utils.FindPlayer.GetPlayerWithActorAndPlayerIDs(actorID, minionID);
            yield return new WaitUntil(() => minion.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.gameObject.name.Contains("P_A_X6")).Any());
            yield return new WaitForSecondsRealtime(0.5f);
            NetworkingManager.RPC(typeof(MinionCardBase), nameof(RPCA_SetBandanaColor), new object[] { minion.data.view.ViewID, bandanaColor.r, bandanaColor.g, bandanaColor.b, bandanaColor.a });
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_SetBandanaColor(int viewID, float r, float g, float b, float a)
        {
            GameObject minion = PhotonView.Find(viewID).gameObject;
            minion.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.gameObject.name.Contains("P_A_X6")).First().color = new Color(r,g,b,a);
        }
        public override void OnRemoveCard()
        {
        }

        public override string GetModName()
        {
            return "TTG";
        }
        internal static void callback(CardInfo card)
        {
            card.gameObject.AddComponent<ExtraName>();
        }
        
        internal static IEnumerator WaitForAIs(IGameModeHandler gm)
        {
            yield return new WaitUntil(() => AIsDoneSpawning);
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
