using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.Session;
using Torch.Managers;

namespace HydrogenPlayerStores
{
    public class HydrogenPlugin : TorchPluginBase
    {
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.AddOverrideMod(2493525535L);
        }
    }
}

