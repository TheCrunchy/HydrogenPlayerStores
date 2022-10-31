﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydrogenPlayerStores.Models;
using NLog.Fluent;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using VRage.Game;

namespace HydrogenPlayerStores.Helper
{
    public static class TankHelper
    {
        public static float RemoveGasFromTanksInGroup(TankGroup group, float gasToRemove)
        {
            foreach (var gas2 in group.TanksInGroup)
            {
                if (!(gasToRemove > 0)) continue;
                var tank = gas2 as MyGasTank;

                var num = (float)(tank.FilledRatio) * tank.GasCapacity;

                if (gasToRemove >= num)
                {
                    tank.ChangeFillRatioAmount(0);
                    gasToRemove -= num;
                }
                else
                {
                    var newAmount = num - gasToRemove;
                    tank.ChangeFillRatioAmount(newAmount / tank.GasCapacity);
                    gasToRemove = 0;
                }
            }
            return gasToRemove;
        }
        public static float AddGasToTanksInGroup(TankGroup group, float amountToUse)
        {
            float GasRemoved = 0f;
            foreach (var tank in group.TanksInGroup)
            {
                if (!(amountToUse > 0)) continue;
                var tank2 = tank as MyGasTank;

                var num = (float)(1.0 - tank2.FilledRatio) * tank2.GasCapacity;

                if (amountToUse >= num)
                {
                    tank2.ChangeFillRatioAmount(tank2.FilledRatio + (num / tank2.GasCapacity));
                    GasRemoved += num;
                    amountToUse -= num;
                }
                else
                {
                    tank2.ChangeFillRatioAmount(tank2.FilledRatio + (amountToUse / tank2.GasCapacity));
                    var newNum = num - amountToUse;
                    GasRemoved += newNum;
                    amountToUse -= newNum;
                }
            }
            //      price += (long)(amountToUse / 1000) * storeItem.PricePerUnit;
            return GasRemoved;
        }

        public static TankGroup MakeTankGroup(List<IMyGasTank> tanks, long ownerId, long ignoredId)
        {
            var gas = new VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties { SubtypeName = "Hydrogen" };
            var group = new TankGroup();

            foreach (var tank in tanks)
            {
                if (tank.OwnerId == ignoredId)
                {
                    continue;
                }
                var relation = tank.GetUserRelationToOwner(ownerId);
                if (!tank.Enabled) continue;

                if (tank.OwnerId != ownerId && relation != MyRelationsBetweenPlayerAndBlock.FactionShare &&
                    relation != MyRelationsBetweenPlayerAndBlock.Neutral) continue;
                if (tank.Stockpile)
                {
                    continue;
                }
                group.TanksInGroup.Add(tank);
                var tankk = tank as MyGasTank;
                if (tankk.BlockDefinition.StoredGasId != MyDefinitionId.FromContent(gas)) continue;
                if (tankk.FilledRatio > 0)
                {
                    group.GasInTanks += (float)(tankk.FilledRatio) * tankk.GasCapacity;
                }
                group.Capacity += (float)(1.0 - tankk.FilledRatio) * tankk.GasCapacity;
            }
            return group;
        }
    }
}
