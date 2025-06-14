using Blackjack.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blackjack.Content.Items
{
	public class GiantBlackjackChip : ModItem
	{
		public override void SetDefaults() {
			Item.width = 50; // The item texture's width.
			Item.height = 50; // The item texture's height.

			Item.useStyle = ItemUseStyleID.Shoot; // The useStyle of the Item.
			Item.useTime = 20; // The time span of using the weapon. Remember in terraria, 60 frames is a second.
			Item.useAnimation = 20; // The time span of the using animation of the weapon, suggest setting it the same as useTime.
			Item.autoReuse = false; // Whether the weapon can be used more than once automatically by holding the use button.

			Item.value = Item.buyPrice(gold: 1); // The value of the weapon in copper coins.
			Item.rare = ItemRarityID.Green;
			Item.UseSound = SoundID.Item1; // The sound when the weapon is being used.
		}

        // When the item is used, display the blackjack interface
        public override void UseAnimation(Player player)
        {
			if (player.whoAmI == Main.myPlayer)
			{
                ModContent.GetInstance<BlackjackOverlayUISystem>().ShowUI();
            }
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(10f, -10f);
        }

        public override void AddRecipes() {
                CreateRecipe()
                .AddIngredient(ItemID.ClayBlock, 20)
				.AddIngredient(ItemID.BlackDye)
				.AddTile(TileID.WorkBenches)
                .Register();
        }
	}
}
