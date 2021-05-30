using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace HydrogenPlayerStores
{
    public class HydrogenPlugin : TorchPluginBase
    {
        public override void Init(ITorchBase torch)
        {

            base.Init(torch);


        }
        public static void SendMessage(string author, string message, Color color, long steamID)
        {


            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId((ulong)steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
       public static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties gas = new VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties { SubtypeName = "Hydrogen" };
        public static Dictionary<long, long> GridsSellingHydrogen = new Dictionary<long, long>();
        public static Dictionary<long, long> Prices = new Dictionary<long, long>();
        public static Dictionary<long, DateTime> cooldowns = new Dictionary<long, DateTime>();
        public static MyDefinitionId gasDefinition = MyDefinitionId.FromContent(gas);

        public object MyApiGateway { get; private set; }
        public int ticks = 0;
        public override void Update()
        {
            ticks++;
            if (ticks % 32 == 0)
            {
                List<long> FuckTheseIds = new List<long>();
                foreach (KeyValuePair<long, long> hydrogen in GridsSellingHydrogen)
                {
                    if (cooldowns.ContainsKey(hydrogen.Value))
                    {
                        DateTime time = cooldowns[hydrogen.Value];
                        if (DateTime.Now < time)
                        {
                            return;
                        }
                        else
                        {
                            cooldowns[hydrogen.Value] = DateTime.Now.AddSeconds(5);
                        }
                    }
                    else
                    {
                        cooldowns.Add(hydrogen.Value, DateTime.Now.AddSeconds(5));
                    }
                    long Price = Prices[hydrogen.Key];
                  //  MyStorePatch.Log.Info(Price);
                    IMyEntity entity = MyAPIGateway.Entities.GetEntityById(hydrogen.Value);

                    var parentGrid = entity as MyCubeGrid;
                    if (entity == null)
                    {
                        FuckTheseIds.Add(hydrogen.Key);
                        SendMessage("Hydrogen Store", "The store has no hydrogen to sell! Cancelling sales.", Color.Red, (long)MySession.Static.Players.TryGetSteamId(hydrogen.Key));
                        break;
                    }



                    if (parentGrid != null)
                    {
                        List<IMyGasTank> storeTanks = new List<IMyGasTank>();
                        List<IMyGasTank> playerTanks = new List<IMyGasTank>();
                        List<IMyGasTank> tanks = new List<IMyGasTank>();
                        BoundingSphereD sphere = new BoundingSphereD(parentGrid.PositionComp.GetPosition(), 1000);
                        double totalGas = 0f;
                        double playerCapacity = 0f;
                        MyGasTank PlayerTankToFill = null;
                        
                        Sandbox.Game.Entities.MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(parentGrid.EntityId) as Sandbox.Game.Entities.MyCubeGrid;
                        foreach (IMyGasTank temp in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<IMyGasTank>())
                        {
                            tanks.Add(temp);
                        }
                        double amountToUse = 15000;
                        foreach (IMyGasTank gasTank in tanks)
                        {
                            if (gasTank.OwnerId == hydrogen.Key && gasTank.CubeGrid.EntityId == hydrogen.Value)
                            {
                                storeTanks.Add(gasTank);
                                MyGasTank tankk = gasTank as MyGasTank;
                                if (tankk.FilledRatio > 0 && tankk.BlockDefinition.StoredGasId == gasDefinition)
                                {
                                    totalGas += (tankk.FilledRatio) * (double)tankk.Capacity;
                                }
                            }
                            else
                            {
                                if (!gasTank.Stockpile)
                                    continue;

                                playerTanks.Add(gasTank);
                            }
                        }
                        foreach (IMyGasTank gasTank in playerTanks)
                        {
                        //    MyStorePatch.Log.Info("1");
                            MyGasTank tankk = gasTank as MyGasTank;
                            amountToUse = tankk.Capacity * 0.1;

                            double tempCapacity = 0f;
                            tempCapacity += (1.0 - tankk.FilledRatio) * (double)tankk.Capacity;
                            if (tankk.FilledRatio == 1)
                            {
                                continue;

                            }

                            if (amountToUse >= totalGas)
                                amountToUse = totalGas;

                            if (amountToUse >= tempCapacity)
                                amountToUse = tempCapacity;

                         //   MyStorePatch.Log.Info(amountToUse);
                            double tempPrice = Price * amountToUse / 1000;
                            if (EconUtils.getBalance(tankk.OwnerId) >= tempPrice)
                            {
                                PlayerTankToFill = tankk as MyGasTank;
                                playerCapacity += (1.0 - tankk.FilledRatio) * (double)tankk.Capacity;
                            }
                            else
                            {
                                continue;
                            }


                        }

                        if (totalGas == 0)
                        {
                            FuckTheseIds.Add(hydrogen.Key);
                            return;
                        }
                        if (PlayerTankToFill == null)
                        {
                            return;
                        }
                        double gasToRemove = 0;
                        long TotalPrice = Price * (long)(amountToUse / 1000);
                        double num = (1.0 - PlayerTankToFill.FilledRatio) * (double)PlayerTankToFill.Capacity;

                        if (amountToUse >= num)
                        {
                            //   Log.Info("Filling 1");
                            PlayerTankToFill.ChangeFillRatioAmount(PlayerTankToFill.FilledRatio + (num / PlayerTankToFill.Capacity));
                        }
                        else
                        {
                            //   Log.Info("Filling 2");
                            PlayerTankToFill.ChangeFillRatioAmount(PlayerTankToFill.FilledRatio + (amountToUse / PlayerTankToFill.Capacity));
                        }
                        gasToRemove = amountToUse;

                        foreach (IMyGasTank gas2 in storeTanks)
                        {
                            if (gasToRemove > 0)
                            {
                                MyGasTank tank = gas2 as MyGasTank;

                                double num2 = (tank.FilledRatio) * (double)tank.Capacity;

                                if (gasToRemove >= num2)
                                {
                                    MyStorePatch.Log.Info("Taking 1");
                                    tank.ChangeFillRatioAmount(0);
                                    gasToRemove -= num2;

                                }
                                else
                                {
                                    double newAmount = num2 - gasToRemove;
                                    MyStorePatch.Log.Info(gasToRemove);
                                    tank.ChangeFillRatioAmount(newAmount / tank.Capacity);
                                    gasToRemove = 0;
                                }
                            }

                        }
                        EconUtils.takeMoney(PlayerTankToFill.OwnerId, TotalPrice);
                        EconUtils.addMoney(hydrogen.Key, TotalPrice);
                    }

                }
                foreach (long id in FuckTheseIds)
                {
                    cooldowns.Remove(id);
                    Prices.Remove(id);
                    GridsSellingHydrogen.Remove(id);
                }
            }
        }
    }
}

