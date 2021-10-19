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
using ModdingUtils.AIMinion.Extensions;
using ModdingUtils.AIMinion;
using UnboundLib.GameModes;
using ModdingUtils;
using ModdingUtils.Extensions;

namespace TTGC.Cards
{
    public class FloatingNinjaCard : MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(1f, 0.5f, 1f, 0.3792f);
        }
        public override AIMinionHandler.SpawnLocation GetAISpawnLocation(Player player)
        {
            return AIMinionHandler.SpawnLocation.Random;
        }
        public override AIMinionHandler.AIAggression GetAIAggression(Player player)
        {
            return AIMinionHandler.AIAggression.Peaceful;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            List<string> floatCards = new List<string>() { "tank", "tank", "tank" };

            return ModdingUtils.Utils.Cards.all.Where(card => floatCards.Contains(card.cardName.ToLower())).ToList();
        }
        public override GunAmmoStatModifier GetGunAmmoStats(Player player)
        {
            GunAmmoStatModifier gunAmmoStats = new GunAmmoStatModifier();
            gunAmmoStats.maxAmmo_add = -3;
            return gunAmmoStats;
        }
        public override GravityModifier GetGravityModifier(Player player)
        {
            return new GravityModifier()
            {
                gravityForce_mult = -0.025f,
                exponent_mult = 0f
            };
        }
        public override CharacterStatModifiersModifier GetCharacterStats(Player player)
        {
            return new CharacterStatModifiersModifier()
            {
                movementSpeed_mult = 0.25f,
            };
        }
        public override float? GetMaxHealth(Player player)
        {
            return 100f;
        }
        public override List<System.Type> GetEffects(Player player)
        {
            return new List<System.Type>() {typeof(TransparentEffect)};
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override string GetDescription()
        {
            return "Recruit a ninja that floats in front of your enemies, blocking their shots.";
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
            return CardThemeColor.CardThemeColorType.MagicPink;
        }

        protected override string GetTitle()
        {
            return "Floating Ninja";
        }

        private class TransparentEffect : MonoBehaviour
        {
            private Player player = null;
            private Color baseColorMax;
            private Color baseColorMin;
            private void OnEnable()
            {
                Unbound.Instance.StartCoroutine(AddColorEffect());
            }
            private IEnumerator AddColorEffect()
            {
                yield return new WaitForSecondsRealtime(0.5f);
                player = this.gameObject.GetComponent<Player>();
                if (player == null) { yield break; }

                baseColorMax = ModdingUtils.Extensions.GetPlayerColor.GetColorMax(player);
                baseColorMin = ModdingUtils.Extensions.GetPlayerColor.GetColorMin(player);

                ModdingUtils.MonoBehaviours.ReversibleColorEffect colorEffect = player.gameObject.AddComponent<ModdingUtils.MonoBehaviours.ReversibleColorEffect>();

                colorEffect.SetColorMax(new Color(baseColorMax.r, baseColorMax.g, baseColorMax.b, 0.025f));
                colorEffect.SetColorMin(new Color(baseColorMin.r, baseColorMin.g, baseColorMin.b, 0.025f));

                yield break;
            }
        }
    }
}
