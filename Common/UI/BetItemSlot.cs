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
                ItemSlot.Handle(ref item, context);
                if (!item.IsAir && item.value <= 0)
                {
                    item.TurnToAir();
                }
            }
        }

        // Draw is performed manually by the owning UIElement
        public void DrawSlot(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            ItemSlot.Draw(spriteBatch, ref item, context, dims.Position(), default, scale);
        }
    }
}
