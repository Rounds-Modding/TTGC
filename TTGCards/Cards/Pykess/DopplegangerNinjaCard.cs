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
    public class DopplegangerNinjaCard : MinionCardBase
    {
        public override AIPlayer.AISkill? GetAISkill(Player player)
        {
            return AIPlayer.AISkill.Expert;
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
            return null;
        }

        protected override string GetDescription()
        {
            return null;
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
            return CardThemeColor.CardThemeColorType.DestructiveRed;
        }

        protected override string GetTitle()
        {
            return "Friendly Doppleganger Ninja";
        }
    }
}
