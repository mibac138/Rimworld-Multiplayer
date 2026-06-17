using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Multiplayer.Client.Patches
{
    [HarmonyPatch]
    static class AreaSource_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(AreaSource), nameof(AreaSource.ComputeAll));
            yield return AccessTools.DeclaredMethod(typeof(AreaSource), nameof(AreaSource.UpdateIncrementally));
        }
        static void Prefix(AreaSource __instance, ref AreaManager __state)
        {
            if (Multiplayer.Client == null || !Multiplayer.GameComp.multifaction) return;
            __state = __instance.map.areaManager;
            __instance.map.areaManager = __instance.map.MpComp().AllAreaManager();
        }
        static void Finalizer(AreaSource __instance, AreaManager __state)
        {
            if (Multiplayer.Client == null || !Multiplayer.GameComp.multifaction) return;

            // restore original
            __instance.map.areaManager = __state;
        }
    }
}
