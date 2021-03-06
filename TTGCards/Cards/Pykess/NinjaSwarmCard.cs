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
    public class NinjaSwarmCard : MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            // random bandana color with 0 or 1 in each RGB field
            return new Color(UnityEngine.Random.Range(0,2), UnityEngine.Random.Range(0, 2), UnityEngine.Random.Range(0, 2), 1f);
        }
        public override AIMinionHandler.SpawnLocation GetAISpawnLocation(Player player)
        {
            return AIMinionHandler.SpawnLocation.Random;
        }
        public override AIMinionHandler.AISkill GetAISkill(Player player)
        {
            return AIMinionHandler.AISkill.Normal;
        }
        public override float? GetMaxHealth(Player player)
        {
            return 1f;
        }
        public override int GetNumberOfMinions(Player player)
        {
            return 10;
        }
        
        public override CharacterStatModifiersModifier GetCharacterStats(Player player)
        {
            CharacterStatModifiersModifier charStats = new CharacterStatModifiersModifier()
            {
                movementSpeed_mult = 1.5f
            };

            return charStats;
        }
        public override GunAmmoStatModifier GetGunAmmoStats(Player player)
        {
            GunAmmoStatModifier gunAmmoStats = new GunAmmoStatModifier
            {
                maxAmmo_add = -2,
                reloadTimeMultiplier_mult = 1f / 3f
            };
            return gunAmmoStats;
        }
        public override GunStatModifier GetGunStats(Player player)
        {
            GunStatModifier gunStats = new GunStatModifier
            {
                damage_mult = 0.25f
            };
            return gunStats;
        }
        
        public override List<System.Type> GetEffects(Player player)
        {
            return new List<System.Type>() { typeof(AntSquishEffect) };
        }

        protected override GameObject GetCardArt()
        {
            return TTGC.ArtAssets_Pykess.LoadAsset<GameObject>("C_NinjaSwarm");
        }

        protected override string GetDescription()
        {
            return "Recruit a swarm of angry little ninjas.";
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
            return "Ninja Swarm";
        }
    }
}
