using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Blackjack.Common.UI
{
    // Simple UIElement wrapper around Terraria.UI.ItemSlot logic.
    public class BetItemSlot : UIElement
    {
        internal Item item;
        private readonly int context;
        private bool interactable = true;
        Asset<Texture2D> itemSlotTexture = ModContent.Request<Texture2D>($"Blackjack/Assets/CustomItemSlot");
        Asset<Texture2D> emptySlotTexture = ModContent.Request<Texture2D>($"Blackjack/Assets/CustomItemSlotEmpty");

        public BetItemSlot(Item boundItem, int context = ItemSlot.Context.CreativeSacrifice, float size = 88f)
        {
            item = boundItem;
            this.context = context;
            Width.Set(size, 0f);
            Height.Set(size, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public void DisableInteract()
        {
            interactable = false;
        }

        public void EnableInteract()
        {
            interactable = true;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            // Then check if the item is a currency, ore, bar, or gem
            if (interactable)
            {
                if (Main.mouseItem.IsCurrency || CustomMouseItemCheck(Main.mouseItem) || Main.mouseItem.IsAir)
                {
                    Item temp = item.Clone();
                    item = Main.mouseItem.Clone();
                    Main.mouseItem = temp;
                    SoundEngine.PlaySound(SoundID.Grab);
                }
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
            CalculatedStyle dims = GetDimensions();
            spriteBatch.Draw(itemSlotTexture.Value, new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height), Color.White);
            if (!item.IsAir)
            {
                Main.instance.LoadItem(item.type);
                spriteBatch.Draw(TextureAssets.Item[item.type].Value, new Rectangle((int)(dims.X + dims.Width / 4), (int)(dims.Y + dims.Height / 4), (int)dims.Width / 2, (int)dims.Height / 2), Color.White);
            }
            else
            {
                spriteBatch.Begin(SamplerState.PointClamp); // Supposedly prevents interpolation when scaling images
                spriteBatch.Draw(emptySlotTexture.Value, new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height), Color.White);
            }
        }
    }
}
