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
    public class PoisonNinjaCard: MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(0f,1f, 0f, 1f);
        }
        public override AIMinionHandler.AISkill GetAISkill(Player player)
        {
            return AIMinionHandler.AISkill.Normal;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            return ModdingUtils.Utils.Cards.all.Where(card => card.cardName.ToLower() == "poison").ToList();
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override string GetDescription()
        {
            return null;
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }

        protected override CardInfoStat[] GetStats()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.PoisonGreen;
        }

        protected override string GetTitle()
        {
            return "Poison Ninja";
        }
    }
}
