using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Blackjack.Common.UI
{
    // Simple UIElement wrapper around Terraria.UI.ItemSlot logic.
    public class BetItemSlot : UIElement
    {
        internal Item item;
        private readonly int context;
        private readonly float scale;

        public BetItemSlot(Item boundItem, int context = ItemSlot.Context.BankItem, float size = 52f)
        {
            item = boundItem;
            this.context = context;
            scale = 2f;
            Width.Set(size * scale, 0f);
            Height.Set(size * scale, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            // Then check if the item is a currency, ore, bar, or gem
            if (Main.mouseItem.IsCurrency || CustomMouseItemCheck(Main.mouseItem))
            {
                Item temp = item.Clone();
                item = Main.mouseItem.Clone();
                Main.mouseItem = temp;
            }
        }

        public bool CustomMouseItemCheck(Item item)
        {
            int[] items = [ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar, ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar, ItemID.DemoniteBar, ItemID.CrimtaneBar, ItemID.HellstoneBar];
            if (items.Contains(item.type))
                return true;
            return false;
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
