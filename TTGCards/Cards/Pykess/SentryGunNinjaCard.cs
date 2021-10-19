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
    public class SentryGunNinjaCard: MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(0.25f,0.25f,0.25f, 1f);
        }
        public override AIMinionHandler.AISkill GetAISkill(Player player)
        {
            return AIMinionHandler.AISkill.Expert;
        }
        public override AIMinionHandler.SpawnLocation GetAISpawnLocation(Player player)
        {
            return AIMinionHandler.SpawnLocation.Center;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            List<string> sentryCards = new List<string>() { "spray", "quick reload" };
            return ModdingUtils.Utils.Cards.all.Where(card => sentryCards.Contains(card.cardName.ToLower())).ToList();
        }
        public override CharacterStatModifiersModifier GetCharacterStats(Player player)
        {
            return new CharacterStatModifiersModifier()
            {
                movementSpeed_mult = 0f,
                jump_mult = 0f
            };
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override string GetDescription()
        {
            return "Recruit a ninja that really wants to be a sentry gun.";
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
            return CardThemeColor.CardThemeColorType.FirepowerYellow;
        }

        protected override string GetTitle()
        {
            return "Sentry Gun Ninja";
        }
    }
}
