using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnboundLib; // requires UnboundLib.dll
using UnboundLib.Cards; // " "
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll, and UnityEngine.AssetBundleModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using Photon.Pun;
using Jotunn.Utils;
using UnboundLib.GameModes;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnboundLib.Networking;
using UnboundLib.Utils;
using TTGC.Cards;

// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace TTGC
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)] // necessary for most modding stuff here
    [BepInPlugin(ModId, ModName, "0.0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class TTGC : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // register credits with unbound
            Unbound.RegisterCredits(ModName, new string[] { "TimeToGrind", "Pykess" }, "","");

            //PCE.ArtAssets = AssetUtils.LoadAssetBundleFromResources("pceassetbundle", typeof(PCE).Assembly);
            //if (PCE.ArtAssets == null)
            //{
            //    UnityEngine.Debug.Log("Failed to load PCE art asset bundle");
            //}

            // build all cards

            CustomCard.BuildCard<MinionCardBase>(MinionCardBase.callback);

            GameModeManager.AddHook(GameModeHooks.HookBattleStart, MinionCardBase.CreateAllAIs);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, MinionCardBase.RemoveAllAIs);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, MinionCardBase.RemoveAllAIs);
            
        }

        private const string ModId = "ttg.rounds.plugins.ttgcards";

        private const string ModName = "Time To Grind Cards";
        //internal static AssetBundle ArtAssets;
    }
}
