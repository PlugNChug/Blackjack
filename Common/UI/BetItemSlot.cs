using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Blackjack.Common.UI
{
    // Simple UIElement wrapper around Terraria.UI.ItemSlot logic.
    internal class BetItemSlot : UIElement
    {
        internal Item item;
        private readonly int context;
        private readonly float scale;

        public BetItemSlot(Item boundItem, int context = ItemSlot.Context.BankItem, float scale = 1f)
        {
            item = boundItem;
            this.context = context;
            this.scale = scale;
            Width.Set(52f * scale, 0f);
            Height.Set(52f * scale, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        // Draw the slot with built-in ItemSlot logic
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            ItemSlot.Draw(spriteBatch, ref item, context, dims.Position());
        }
    }
}
