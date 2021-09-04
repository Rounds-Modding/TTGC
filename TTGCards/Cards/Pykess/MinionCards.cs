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
        public virtual ModdingUtils.Extensions.BlockModifier GetBlockStats()
        { return null; }
        public virtual ModdingUtils.Extensions.GunAmmoStatModifier GetGunAmmoStats()
        { return null; }
        public virtual ModdingUtils.Extensions.GunStatModifier GetGunStats()
        { return null; }
        public virtual ModdingUtils.Extensions.CharacterStatModifiersModifier GetCharacterStats()
        { return null; }
        public virtual ModdingUtils.Extensions.GravityModifier GetGravityModifier()
        { return null; }
        public virtual float? GetMaxHealth()
        { return null; }
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {

        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // in sandbox mode? spawn AI immediately
            if (AIPlayer.sandbox)
            {
                AIPlayer.SpawnAI(player.playerID, player.teamID, activeNow: true);
            }
            else
            {
                AIPlayer.SpawnAI(player.playerID, player.teamID, activeNow: false);
            }
        }
        public override void OnRemoveCard()
        {
        }

        protected override string GetTitle()
        {
            return "TTG Minion";
        }
        protected override string GetDescription()
        {
            return "";
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Common;
        }

        protected override CardInfoStat[] GetStats()
        {
            return null;
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
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
