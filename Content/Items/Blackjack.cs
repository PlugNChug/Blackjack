using Blackjack.Common.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blackjack.Content.Items
{
	public class Blackjack : ModItem
	{
		public override void SetDefaults() {
			Item.width = 40; // The item texture's width.
			Item.height = 40; // The item texture's height.

			Item.useStyle = ItemUseStyleID.Swing; // The useStyle of the Item.
			Item.useTime = 20; // The time span of using the weapon. Remember in terraria, 60 frames is a second.
			Item.useAnimation = 20; // The time span of the using animation of the weapon, suggest setting it the same as useTime.
			Item.autoReuse = true; // Whether the weapon can be used more than once automatically by holding the use button.

			Item.DamageType = DamageClass.Melee; // Whether your item is part of the melee class.
			Item.damage = 50; // The damage your item deals.
			Item.knockBack = 6; // The force of knockback of the weapon. Maximum is 20
			Item.crit = 6; // The critical strike chance the weapon has. The player, by default, has a 4% critical strike chance.

			Item.value = Item.buyPrice(gold: 1); // The value of the weapon in copper coins.
			Item.rare = ItemRarityID.Master;
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

        // Basic melee effect: set enemies on fire when struck
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
                        // Inflict the OnFire debuff for 1 second onto any NPC/Monster that this hits.
                        // 60 frames = 1 second
                        target.AddBuff(BuffID.OnFire, 60);
                }

                // Simple recipe so the item can be crafted during testing
                public override void AddRecipes() {
                        CreateRecipe()
                                .AddIngredient(ItemID.Wood, 10)
                                .Register();
                }
	}
}
