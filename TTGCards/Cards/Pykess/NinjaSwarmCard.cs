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
    public class NinjaSwarmCard : MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(1f,1f,1f, 1f);
        }
        public override AIPlayer.AISkill GetAISkill(Player player)
        {
            return AIPlayer.AISkill.Normal;
        }
        public override float? GetMaxHealth(Player player)
        {
            return 10f;
        }
        public override int GetNumberOfMinions(Player player)
        {
            return 4;
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
            return CardThemeColor.CardThemeColorType.EvilPurple;
        }

        protected override string GetTitle()
        {
            return "Ninja Swarm";
        }
    }
}
