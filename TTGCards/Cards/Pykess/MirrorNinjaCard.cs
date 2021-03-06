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

namespace TTGC.Cards
{
    public class MirrorNinjaCard : MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(0.5f,0.5f,0.5f, 1f);
        }
        public override AIMinionHandler.AISkill GetAISkill(Player player)
        {
            return AIMinionHandler.AISkill.Expert;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            return player.data.currentCards;
        }
        public override bool CardsAreReassigned(Player player)
        {
            return true;
        }

        protected override GameObject GetCardArt()
        {
            return TTGC.ArtAssets_Pykess.LoadAsset<GameObject>("C_MirrorNinja");
        }

        protected override string GetDescription()
        {
            return "Recruit a ninja that copies all your current cards.";
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
        }

        protected override CardInfoStat[] GetStats()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
        }

        protected override string GetTitle()
        {
            return "Mirror Ninja";
        }
    }
}
