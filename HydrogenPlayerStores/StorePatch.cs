using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using System.Reflection;
using HydrogenPlayerStores.Helper;
using HydrogenPlayerStores.Models;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Torch.Mod.Messages;
using VRage.Game.Entity;
using Torch.Mod;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Torch.Server.Annotations;
using VRage.Game.ModAPI;

namespace HydrogenPlayerStores
{
    [PatchShim]
    public static class MyStorePatch
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        internal static readonly MethodInfo update =
            typeof(MyStoreBlock).GetMethod("BuyFromPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo sell =
            typeof(MyStoreBlock).GetMethod("SellToPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo storePatch =
            typeof(MyStorePatch).GetMethod(nameof(StorePatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo storePatchSell =
            typeof(MyStorePatch).GetMethod(nameof(StorePatchMethodSell), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(storePatch);
            ctx.GetPattern(sell).Prefixes.Add(storePatchSell);
            Log.Info("Patching Successful HydrogenStores");
        }

        public static bool StorePatchMethodSell(MyStoreBlock __instance, long id, int amount, long sourceEntityId, MyPlayer player)
        {
            var storeItem = __instance.PlayerItems.FirstOrDefault(playerItem => playerItem.Id == id);
            if (storeItem == null)
            {
                return true;
            }
            var isItem = false;

            if (storeItem.Item.Value.SubtypeName.Contains("HydrogenCredit"))
            {
                isItem = true;
            }
            else
            {
                return true;
            }

            var identity = MySession.Static.Players.TryGetIdentity(__instance.OwnerId);
            if (identity.IdentityId == player.Identity.IdentityId)
            {
                var m1 = new DialogMessage("Shop Error", "You cannot sell hydrogen to yourself!");
                ModCommunication.SendMessageTo(m1, player.Id.SteamId);
                return false;
            }

            var test = __instance.CubeGrid.GetGridGroup(GridLinkTypeEnum.Physical);
            var grids = new List<IMyCubeGrid>();
            var tanks = new List<IMyGasTank>();
            test.GetGrids(grids);
            foreach (var gridInGroup in grids)
            {
               tanks.AddRange(gridInGroup.GetFatBlocks<IMyGasTank>());
            }

            var storeTanks = TankHelper.MakeTankGroup(tanks, __instance.OwnerId);
            var playerTanks = TankHelper.MakeTankGroup(tanks, player.Identity.IdentityId);
            if (playerTanks.GasInTanks == 0)
            {
                var m1 = new DialogMessage("Shop Error", "You do not have any gas to sell in non-stockpile tanks!");
                ModCommunication.SendMessageTo(m1, player.Id.SteamId);
                return false;
            }

            var grid = __instance.CubeGrid;
       
            float amountToUse = amount * 1000;
            long price = 0;
            if (amountToUse >= playerTanks.GasInTanks)
                amountToUse = playerTanks.GasInTanks;

            if (amountToUse >= storeTanks.Capacity)
                amountToUse = storeTanks.Capacity;
            var BasePrice = (long)(amountToUse / 1000) * storeItem.PricePerUnit;
            if (MyBankingSystem.GetBalance(__instance.OwnerId) < BasePrice)
            {
                var m3 = new DialogMessage("Shop", "Shop cannot afford to buy that much.");
                ModCommunication.SendMessageTo(m3, player.Id.SteamId);
                return false;
            }
            TankHelper.AddGasToTanksInGroup(storeTanks, amountToUse);
            TankHelper.RemoveGasFromTanksInGroup(playerTanks, amountToUse);
            price = BasePrice;
            
            EconUtils.takeMoney(__instance.OwnerId, price);
            EconUtils.addMoney(player.Identity.IdentityId, price);
            var m = new DialogMessage("Shop", $"Sold some Hydrogen. {BasePrice * 1000}L");
            ModCommunication.SendMessageTo(m, player.Id.SteamId);

            return false;
        }

        public static bool StorePatchMethod(MyStoreBlock __instance, long id, int amount, long targetEntityId, MyPlayer player, MyAccountInfo playerAccountInfo)
        {

            var storeItem = __instance.PlayerItems.FirstOrDefault(playerItem => playerItem.Id == id);
            if (storeItem == null)
            {
                return true;
            }
            var isItem = false;

            if (storeItem.Item.Value.SubtypeName.Contains("HydrogenCredit"))
            {
                isItem = true;
            }
            else
            {
                return true;
            }

            var test = __instance.CubeGrid.GetGridGroup(GridLinkTypeEnum.Physical);
            var grids = new List<IMyCubeGrid>();
            var tanks = new List<IMyGasTank>();
            test.GetGrids(grids);
            foreach (var gridInGroup in grids)
            {
                tanks.AddRange(gridInGroup.GetFatBlocks<IMyGasTank>());
            }
            var storeTanks = TankHelper.MakeTankGroup(tanks, __instance.OwnerId);
            var playerTanks = TankHelper.MakeTankGroup(tanks, player.Identity.IdentityId);

            float totalGas = storeTanks.GasInTanks;
            var gas = new VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties { SubtypeName = "Hydrogen" };
            float playerCapacity = playerTanks.Capacity;
            //  Sandbox.Engine.Multiplayer.MyMultiplayer.RaiseEvent<MyStoreBlock, MyStoreSellItemResult>(this, (Func<MyStoreBlock, Action<MyStoreSellItemResult>>)(x => new Action<MyStoreSellItemResult>(x.OnSellItemResult)), storeSellItemResult, MyEventContext.Current.Sender);
            if (storeTanks.GasInTanks == 0)
            {
                var m1 = new DialogMessage("Shop Error", "Tanks have no gas to sell!");
                ModCommunication.SendMessageTo(m1, player.Id.SteamId);
                return false;
            }

            var grid = __instance.CubeGrid;
            var identity = MySession.Static.Players.TryGetIdentity(playerAccountInfo.OwnerIdentifier);
            float amountToUse = amount * 1000;
            long price = 0;
            if (amountToUse >= totalGas)
                amountToUse = totalGas;

            if (amountToUse >= playerCapacity)
                amountToUse = playerCapacity;

            var BasePrice = (long)(amountToUse / 1000) * storeItem.PricePerUnit;
            if (MyBankingSystem.GetBalance(player.Identity.IdentityId) < BasePrice)
            {
                var m3 = new DialogMessage("Shop", "Cannot afford to buy that much.");
                ModCommunication.SendMessageTo(m3, player.Id.SteamId);
                return false;
            }

            TankHelper.AddGasToTanksInGroup(playerTanks, amountToUse);
            TankHelper.RemoveGasFromTanksInGroup(storeTanks, amountToUse);
            price = BasePrice;

            EconUtils.takeMoney(__instance.OwnerId, price);
            EconUtils.addMoney(player.Identity.IdentityId, price);
            var m = new DialogMessage("Shop", $"Tanks filled. {BasePrice * 1000}L");
            ModCommunication.SendMessageTo(m, player.Id.SteamId);
            return false;
        }

    }
}
