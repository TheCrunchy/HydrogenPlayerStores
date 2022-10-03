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
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace HydrogenPlayerStores
{
    public class HydrogenPlugin : TorchPluginBase
    {
        public void Init(ITorchBase torch)
        {
            base.Init(torch);
        }
    }
}

