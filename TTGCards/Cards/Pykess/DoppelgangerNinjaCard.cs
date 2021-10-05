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
    public class DoppelgangerNinjaCard : MinionCardBase
    {
        public override Color GetBandanaColor(Player player)
        {
            return new Color(1f,0f,0f,1f);
        }
        public override AIMinionHandler.AISkill GetAISkill(Player player)
        {
            return AIMinionHandler.AISkill.Expert;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            Player[] enemyPlayers = PlayerManager.instance.players.Where(p => p.teamID != player.teamID).ToArray();

            if (enemyPlayers.Length == 0)
            {
                return null;
            }
            else
            {
                Player enemy = enemyPlayers[UnityEngine.Random.Range(0, enemyPlayers.Length)];
                return enemy.data.currentCards;
            }
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
            return "Doppelganger Ninja";
        }
    }
}
