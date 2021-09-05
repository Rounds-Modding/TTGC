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

namespace TTGC.Cards
{
    public abstract class MinionCardBase : CustomCard
    {
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
        public virtual AIPlayer.AISkill? GetAISkill(Player player)
        { return null; }
        public virtual AIPlayer.AIAggression? GetAIAggression(Player player)
        { return null; }
        public virtual AIPlayer.AI? GetAI(Player player)
        { return null; }
        public virtual Color GetBandanaColor(Player player)
        {
            return new Color(0.3679f, 0.2169f, 0.2169f, 1f);
        }
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {

        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // AI's can't have AI's
            if (player.data.GetAdditionalData().isAI)
            {
                return;
            }

            // add AI player
            Player minion = AIPlayer.CreateAIWithStats(player.playerID, player.teamID, GetAISkill(player), GetAIAggression(player), GetAI(player), GetMaxHealth(player), GetBlockStats(player), GetGunAmmoStats(player), GetGunStats(player), GetCharacterStats(player), GetGravityModifier(player), AIPlayer.sandbox);
        
            // add cards to AI
            if (GetCards(player) != null)
            {
                Unbound.Instance.ExecuteAfterSeconds(0.5f, () =>
                {
                    ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(minion, GetCards(player).ToArray(), CardsAreReassigned(player), addToCardBar: false);

                    minion.data.view.RPC("RPCA_SetFace", RpcTarget.All, new object[] {63, new Vector2(0f, -0.5f), 19, new Vector2(0f,-0.5f), 14, new Vector2(0f,1.1f), 0, new Vector2(0f,0f) });

                    minion.gameObject.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.gameObject.name.Contains("P_A_X6")).First().color = GetBandanaColor(player);

                });
            }
        
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

        private static IEnumerator SpawnWhenReady(Player minionToSpawn, Vector3 position)
        {
            yield return new WaitUntil(() => MinionCardBase.readyToSpawn);
            minionToSpawn.GetComponentInChildren<AIPlayer.EnableDisablePlayer>().Enable(position);
            minionToSpawn.GetComponentInChildren<AIPlayer.EnableDisablePlayer>().ReviveAndSpawn(position);
        }
        private static bool readyToSpawn = false;
        private static float baseOffset = 0.5f;
        internal static IEnumerator CreateAllAIs(IGameModeHandler gm)
        {
            readyToSpawn = false;
            foreach (Player player in PlayerManager.instance.players)
            {
                int minionNum = 0;
                foreach (Player minion in player.data.GetAdditionalData().minions)
                {
                    minionNum++;
                    Unbound.Instance.StartCoroutine(SpawnWhenReady(minion, player.gameObject.transform.position - minionNum * baseOffset * new Vector3(UnityEngine.Mathf.Sign(player.gameObject.transform.position.x), 0f, 0f)));
                }
            }
            yield return new WaitForEndOfFrame();
            readyToSpawn = true;
            yield return new WaitForSecondsRealtime(0.1f);
            yield break;
        }
        private static IEnumerator RemoveWhenReady(Player minionToRemove)
        {
            yield return new WaitUntil(() => MinionCardBase.readyToRemove);
            minionToRemove.GetComponentInChildren<AIPlayer.EnableDisablePlayer>().Disable();
        }
        private static bool readyToRemove = false;
        internal static IEnumerator RemoveAllAIs(IGameModeHandler gm)
        {
            foreach (Player player in PlayerManager.instance.players)
            {
                foreach (Player minion in player.data.GetAdditionalData().minions)
                {
                    Unbound.Instance.StartCoroutine(RemoveWhenReady(minion));
                }
            }
            yield return new WaitForSecondsRealtime(0.5f);
            readyToRemove = true;
            yield return new WaitForSecondsRealtime(0.1f);
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
