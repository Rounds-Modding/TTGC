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
    public class AgileNinjaCard : MinionCardBase
    {
        
        public override Color GetBandanaColor(Player player)
        {
            return new Color(0.5f,0.5f,0f,1f);
        }
        public override AIMinionHandler.AISkill GetAISkill(Player player)
        {
            return AIMinionHandler.AISkill.Normal;
        }
        public override GunStatModifier GetGunStats(Player player)
        {
            return new GunStatModifier()
            {
                projectileSpeed_mult = 2f,
                attackSpeed_mult = 0.5f,
                damage_mult = 0.5f
            };
        }
        public override CharacterStatModifiersModifier GetCharacterStats(Player player)
        {
            return new CharacterStatModifiersModifier()
            {
                movementSpeed_mult = 2f
            };
        }
        public override BlockModifier GetBlockStats(Player player)
        {
            return new BlockModifier()
            {
                cdMultiplier_mult = 0.5f
            };
        }
        public override float? GetMaxHealth(Player player)
        {
            return 75f;
        }

        protected override GameObject GetCardArt()
        {
            return TTGC.ArtAssets_Pykess.LoadAsset<GameObject>("C_AgileNinja");
        }

        protected override string GetDescription()
        {
            return "Recruit a ninja that sprints after enemies.";
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
            return CardThemeColor.CardThemeColorType.NatureBrown;
        }

        protected override string GetTitle()
        {
            return "Agile Ninja";
        }
    }
}
