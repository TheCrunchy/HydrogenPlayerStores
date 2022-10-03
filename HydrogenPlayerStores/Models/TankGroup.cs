using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;

namespace HydrogenPlayerStores.Models
{
    public class TankGroup
    {
        public float Capacity;
        public float GasInTanks;
        public List<IMyGasTank> TanksInGroup = new List<IMyGasTank>();
    }
}
