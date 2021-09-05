﻿using UnboundLib.Cards;
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
    public class ColdNinjaCard : MinionCardBase
    {
        public override AIPlayer.AIAggression? GetAIAggression(Player player)
        {
            return AIPlayer.AIAggression.Peaceful;
        }
        public override List<CardInfo> GetCards(Player player)
        {
            List<CardInfo> cards = new List<CardInfo>() { };

            foreach (CardInfo card in Cards.allCards)
            {
                if (card.cardName.ToLower() == "lifestealer" || card.cardName.ToLower() == "chilling presence" || card.cardName.ToLower() == "chase")
                {
                    cards.Add(card);
                }
            }

            return cards;
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