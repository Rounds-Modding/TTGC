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
using ModdingUtils.Utils;

namespace TTGC.Cards
{
    public class ColdNinjaCard : MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(0.5f * 66f / 255f, 0.5f *  209f / 255f, 0.5f * 245f / 255f, 1f);
        }
        public override AIMinionHandler.AI GetAI(Player player)
        {
            return AIMinionHandler.AI.Petter;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            List<string> coldCards = new List<string>() { "chilling presence", "chase" };

            return ModdingUtils.Utils.Cards.all.Where(card => coldCards.Contains(card.cardName.ToLower())).ToList();
        }
        public override GunAmmoStatModifier GetGunAmmoStats(Player player)
        {
            GunAmmoStatModifier gunAmmoStats = new GunAmmoStatModifier();
            gunAmmoStats.maxAmmo_add = -3;
            return gunAmmoStats;
        }
        public override float? GetMaxHealth(Player player)
        {
            return 75f;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override string GetDescription()
        {
            return "Recruit a ninja with a cold aura that slows your enemies down.";
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
            return CardThemeColor.CardThemeColorType.ColdBlue;
        }

        protected override string GetTitle()
        {
            return "Cold Ninja";
        }
    }
}
