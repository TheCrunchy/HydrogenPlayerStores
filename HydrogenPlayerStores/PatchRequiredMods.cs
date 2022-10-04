using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Networking;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Library.Utils;

namespace HydrogenPlayerStores
{
    [PatchShim]
    public static class PatchRequiredMods
    {
        private static readonly List<ulong> RequiredMods = new List<ulong>();
        internal static readonly MethodInfo update =
            typeof(MyLocalCache).GetMethod("LoadCheckpoint", BindingFlags.Public | BindingFlags.Static) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo mods =
            typeof(PatchRequiredMods).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update).Suffixes.Add(mods);
        }

        public static void Postfix(
            ref MyObjectBuilder_Checkpoint __result,
            string sessionDirectory,
            ulong sizeInBytes,
            MyGameModeEnum? forceGameMode = null,
            MyOnlineModeEnum? forceOnlineMode = null)
        {
            __result.Mods = AddRequiredMods(__result.Mods);
        }

        public static List<MyObjectBuilder_Checkpoint.ModItem> AddRequiredMods(List<MyObjectBuilder_Checkpoint.ModItem> existingMods)
        {
            PatchRequiredMods.RequiredMods.Add(2493525535UL);
            foreach (ulong requiredMod in PatchRequiredMods.RequiredMods)
            {
                ulong Mod = requiredMod;
                MyObjectBuilder_Checkpoint.ModItem modItem = new MyObjectBuilder_Checkpoint.ModItem(Mod, (string)null);
                if (!existingMods.Any<MyObjectBuilder_Checkpoint.ModItem>((Func<MyObjectBuilder_Checkpoint.ModItem, bool>)(x => (long)Mod == (long)x.PublishedFileId)))
                    existingMods.Add(modItem);
            }
          
            return existingMods;
        }
    }
}
