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
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.playerjumppatch", BepInDependency.DependencyFlags.HardDependency)] // fixes multiple jumps
    [BepInDependency("pykess.rounds.plugins.legraycasterspatch", BepInDependency.DependencyFlags.HardDependency)] // fixes physics for small players
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
            Unbound.RegisterCredits(ModName, new string[] { "TimeToGrind", "Pykess" }, new string[] { "github", "commission your own mod from pykess" },new string[] { "https://github.com/Rounds-Modding/TTGC", "https://www.buymeacoffee.com/Pykess" });

            TTGC.ArtAssets_Pykess = AssetUtils.LoadAssetBundleFromResources("ttgcpykessassetbundle", typeof(TTGC).Assembly);
            if (TTGC.ArtAssets_Pykess == null)
            {
                UnityEngine.Debug.Log("Failed to load TTGC art asset bundle for Pykess-made cards");
            }

            // build all cards

            CustomCard.BuildCard<ColdNinjaCard>(ColdNinjaCard.callback);
            CustomCard.BuildCard<MirrorNinjaCard>(MirrorNinjaCard.callback);
            CustomCard.BuildCard<DoppelgangerNinjaCard>(DoppelgangerNinjaCard.callback);
            CustomCard.BuildCard<NinjaSwarmCard>(NinjaSwarmCard.callback);
            CustomCard.BuildCard<ShieldNinjaCard>(ShieldNinjaCard.callback);
            CustomCard.BuildCard<PoisonNinjaCard>(PoisonNinjaCard.callback);
            CustomCard.BuildCard<SentryGunCard>(SentryGunCard.callback);
            CustomCard.BuildCard<AgileNinjaCard>(AgileNinjaCard.callback);
            CustomCard.BuildCard<FloatingNinjaCard>(FloatingNinjaCard.callback);

            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, MinionCardBase.WaitForAIs);


        }

        private const string ModId = "ttg.rounds.plugins.ttgcards";

        private const string ModName = "Time To Grind Cards";
        internal static AssetBundle ArtAssets_Pykess;
    }
}
