using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Blackjack.Common.UI
{

    [Autoload(Side = ModSide.Client)]

    // System responsible for loading and updating the blackjack user interface.
    public class BlackjackOverlayUISystem : ModSystem
    {
        private UserInterface blackjackInterface;
        internal BlackjackOverlayUIState blackjackUI;

        // Blackjack UI visiblity setters (called from Blackjack.cs)
        public void ShowUI()
        {
            blackjackInterface?.SetState(blackjackUI);
        }

        public void HideUI()
        {
            blackjackInterface?.SetState(null);
        }
        public override void Load()
        {
            // Create custom interface which can swap between different UIStates
            blackjackInterface = new UserInterface();
            // Creating custom UIState
            blackjackUI = new BlackjackOverlayUIState();

            // Activate calls Initialize() on the UIState if not initialized, then calls OnActivate and then calls Activate on every child element
            blackjackUI.Activate();
        }

        public override void OnWorldUnload()
        {

            base.OnWorldUnload();
            HideUI();
        }

        public override void OnWorldLoad()
        {
            base.OnWorldLoad();
            HideUI();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Here we call .Update on our custom UI and propagate it to its state and underlying elements
            if (blackjackInterface?.CurrentState != null)
            {
                blackjackInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Blackjack: Overlay",
                    delegate {
                        if (blackjackInterface?.CurrentState != null)
                        {
                            blackjackInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

    }
}
