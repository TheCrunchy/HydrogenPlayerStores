using Sandbox.Game.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace HydrogenPlayerStores
{
    [Category("hydrogen")]
    public class Commands : CommandModule
    {
        [Command("sell", "start selling hydrogen")]
        [Permission(MyPromoteLevel.None)]
        public void SellHydrogen(string pricePer1000)
        {
            Int64 amount;
            pricePer1000 = pricePer1000.Replace(",", "");
            pricePer1000 = pricePer1000.Replace(".", "");
            pricePer1000 = pricePer1000.Replace(" ", "");
            try
            {
                amount = Int64.Parse(pricePer1000);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            if (Context.Player == null)
            {
                Context.Respond("Player only.");
                return;
            }
            long gridId = 0;
            ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroupMechanical(Context.Player.Character);


            List<MyCubeGrid> grids = new List<MyCubeGrid>();
            foreach (var item in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Node groupNodes in item.Nodes)
                {
                    if (gridId > 0)
                    {
                        break;
                    }
                    MyCubeGrid grid = groupNodes.NodeData;
                    if (FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, false))
                    {
                        gridId = grid.EntityId;
                        break;
                    }
                }
            }

     
            if (!HydrogenPlugin.GridsSellingHydrogen.ContainsKey(Context.Player.IdentityId))
            {
                HydrogenPlugin.GridsSellingHydrogen.Add(Context.Player.IdentityId, gridId);
                HydrogenPlugin.Prices.Add(Context.Player.IdentityId, amount);
                Context.Respond("Now selling hydrogen to grids within 1000m with stockpile on for " + String.Format("{0:n0}", amount) + " SC per 1000L");
            }
            else
            {
                Context.Respond("You are already selling hydrogen from a grid!");
            }
     
        }
        [Command("stop", "stop selling hydrogen")]
        [Permission(MyPromoteLevel.None)]
        public void StopHydrogen()
        {

            if (HydrogenPlugin.GridsSellingHydrogen.ContainsKey(Context.Player.IdentityId))
            {
                HydrogenPlugin.GridsSellingHydrogen.Remove(Context.Player.IdentityId);
                HydrogenPlugin.Prices.Remove(Context.Player.IdentityId);
                HydrogenPlugin.cooldowns.Remove(Context.Player.IdentityId);
                Context.Respond("No longer selling hydrogen");
            }
            else
            {
                Context.Respond("You need to be selling hydrogen to stop.");
            }

        }
    }
}
