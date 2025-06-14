using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Blackjack.Common.Config
{
    public class Appearance : ModConfig
    {
        // ConfigScope.ClientSide should be used for client side, usually visual or audio tweaks.
        // ConfigScope.ServerSide should be used for basically everything else, including disabling items or changing NPC behaviors
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // Overall UI Scale
        [Range(0.1f, 1.5f)]
        [Increment(.05f)]
        [DrawTicks]
        [DefaultValue(0.8f)]
        [ReloadRequired]
        public float BlackjackUIScale;

        // Blackjack Card Scale
        [Range(60, 140)]
        [Increment(5)]
        [DrawTicks]
        [DefaultValue(120)]
        [ReloadRequired]
        public int BlackjackCardScale;
    }
}
