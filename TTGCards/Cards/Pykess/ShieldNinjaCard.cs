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
using ModdingUtils.Extensions;

namespace TTGC.Cards
{
    public class ShieldNinjaCard: MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(1f,1f,1f, 1f);
        }
        public override AIMinionHandler.AIAggression GetAIAggression(Player player)
        {
            return AIMinionHandler.AIAggression.Suicidal;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            CardInfo defender = Cards.allCards.Where(card => card.cardName.ToLower() == "defender").First();
            CardInfo shockwave = Cards.allCards.Where(card => card.cardName.ToLower() == "shockwave").First();
            CardInfo echo = Cards.allCards.Where(card => card.cardName.ToLower() == "echo").First();

            return new List<CardInfo>() { echo, shockwave };
        }
        public override CharacterStatModifiersModifier GetCharacterStats(Player player)
        {
            return new CharacterStatModifiersModifier()
            {
                movementSpeed_mult = 1f/3f
            };
        }
        public override GunStatModifier GetGunStats(Player player)
        {
            return new GunStatModifier()
            {
                attackSpeed_mult = float.MaxValue,
                damage_mult = 0f,
            };
        }
        public override GunAmmoStatModifier GetGunAmmoStats(Player player)
        {
            return new GunAmmoStatModifier()
            {
                reloadTimeMultiplier_mult = float.MaxValue,
                maxAmmo_mult = 0
            };
        }
        public override BlockModifier GetBlockStats(Player player)
        {
            return new BlockModifier()
            {
                cdMultiplier_mult = 0.5f
            };
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
            return CardThemeColor.CardThemeColorType.DefensiveBlue;
        }

        protected override string GetTitle()
        {
            return "Shield Ninja";
        }
    }
}
