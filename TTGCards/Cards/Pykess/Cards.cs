using UnboundLib.Utils;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;

namespace TTGC.Cards
{
    public static class Cards
    {
        public static List<CardInfo> activeCards
        {
            get
            {
                return ((ObservableCollection<CardInfo>)typeof(CardManager).GetField("activeCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToList();

            }
            set { }
        }
        public static List<CardInfo> inactiveCards
        {
            get
            {
                return (List<CardInfo>)typeof(CardManager).GetField("inactiveCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            }
            set { }
        }
        public static List<CardInfo> allCards
        {
            get
            {
                return activeCards.Concat(inactiveCards).ToList();
            }
            set { }
        }
    }
}
