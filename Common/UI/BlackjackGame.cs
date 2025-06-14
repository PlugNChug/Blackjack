using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;
using Terraria.Localization;
using Blackjack.Common.Config;
using Terraria.GameContent.UI.Elements;

namespace Blackjack.Common.UI
{
    /// <summary>
    /// Card dealing animation helper
    /// </summary>
    public class DealingCard
    {
        // Index in the deck that identifies which card image to draw
        public int CardIndex;
        // Starting position of the animation
        public Vector2 StartPos;
        // Final destination when the card reaches the table
        public Vector2 EndPos;
        // Progress of the animation from 0 (start) to 1 (finished)
        public float Progress;
        // Determines if the card is dealt to the player or the dealer
        public bool ToPlayer;
    }

    /// <summary>
    /// UIElement representing a single blackjack game session.
    /// Handles card shuffling, dealing, drawing and determining winners.
    /// </summary>
    public class BlackjackGame : UIElement
    {
        private float uiScale;
        private int cardWidth;
        private int cardHeight;
        private int stackHeight;

        // Item placed into the betting slot
        internal Item BetItem;
        private BetItemSlot betSlot;

        public BlackjackGame(float scale = 1f)
        {
            BetItem = new Item();
            BetItem.TurnToAir();
            uiScale = scale;
            cardWidth = (int)(ModContent.GetInstance<Appearance>().BlackjackCardScale * uiScale);
            cardHeight = (int)(cardWidth * 1.422f);
            stackHeight = (int)(cardWidth * 1.509f);
        }

        public void ApplyScale(float scale)
        {
            uiScale = scale;
            cardWidth = (int)(ModContent.GetInstance<Appearance>().BlackjackCardScale * uiScale);
            cardHeight = (int)(cardWidth * 1.422f);
            stackHeight = (int)(cardWidth * 1.509f);
        }

        // Holds a numeric representation of a standard 52 card deck
        private List<int> cardList;
        // Tracks which card to draw next from the shuffled deck
        private int cardIndex = 0;

        // Player/dealer card fields
        // Lists storing each card currently held by the player and dealer
        private List<int> playerCards = new List<int>();
        private List<int> dealerCards = new List<int>();
        private bool dealerFirstCardRevealed = false; // Has the dealer's first card been shown
        private bool flippingDealerCard = false;       // Is the reveal animation running
        private float dealerCardFlipProgress = 0f;      // Animation progress value
        private const float dealerCardFlipSpeed = 0.15f;

        // Hand values for calculations
        private int playerHandValue = 0;
        private int dealerHandValue = 0;

        // Active game boolean
        private bool isGameActive = false;

        // Animation fields
        // Cards waiting to be animated onto the table
        private Queue<DealingCard> dealingQueue = new Queue<DealingCard>();
        // Card currently in transit
        private DealingCard currentDealingCard = null;
        // Speed at which cards slide to their destination
        private float dealSpeed = 0.05f; // Adjust for faster/slower animation

        private bool dealerTurn = false;       // True when the dealer is drawing
        private int dealerDrawDelayTimer = 0;  // Countdown before the next dealer draw
        private const int DealerDrawDelay = 60; // Frames of delay between dealer draws
        private bool pendingDealerNatural = false; // Waits for dealer flip before payout

        // Game status text and panel
        private DynamicSpriteFont font = FontAssets.ItemStack.Value;
        private DynamicSpriteFont fontBig = FontAssets.DeathText.Value;
        private string gameStatus = "";

        private StatusPanel statusPanel;

        // Small helper panel that draws the status text above itself
        private class StatusPanel : UIPanel
        {
            private readonly DynamicSpriteFont _font;
            private string _text = string.Empty;

            public StatusPanel(DynamicSpriteFont font)
            {
                _font = font;
                BackgroundColor = Color.Black * 0.8f;
                BorderColor = Color.Black;
                SetPadding(0);
            }

            public void SetText(string text)
            {
                _text = text;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (string.IsNullOrEmpty(_text))
                    return;

                CalculatedStyle dims = GetDimensions();
                Vector2 size = _font.MeasureString(_text);
                Vector2 pos = new Vector2(
                    dims.X + dims.Width / 2f - size.X / 2f,
                    dims.Y + dims.Height / 2f - size.Y / 2f);
                spriteBatch.DrawString(_font, _text, pos, Color.White);
            }
        }

        // Hand value text
        private string dealerStatus1 = Language.GetTextValue("Mods.Blackjack.UI.DealerHand");
        private string dealerStatus2;
        private string playerStatus1 = Language.GetTextValue("Mods.Blackjack.UI.PlayerHand");
        private string playerStatus2;


        // Returns true if a game is currently in progress
        public bool GetActiveGame() => isGameActive;

        // Returns the player's current hand value
        public int GetPlayerHandValue() => playerHandValue;

        // True while a card animation or dealer flip animation is occurring
        public bool IsAnimating => currentDealingCard != null || dealingQueue.Count > 0 || flippingDealerCard;

        public override void OnInitialize()
        {
            base.OnInitialize();
            // Initialize a standard deck of 52 cards
            cardList = new List<int>();
            for (int i = 0; i < 52; i++)
            {
                cardList.Add(i);
            }

            statusPanel = new StatusPanel(fontBig);
        }

        public void SetBetItemSlot(BetItemSlot slotObject)
        {
            betSlot = slotObject;
        }

        public void WithdrawBetItem(Player player)
        {
            if (betSlot.item != null && !betSlot.item.IsAir)
            {
                player.QuickSpawnItem(player.GetSource_Misc("Blackjack"), betSlot.item.type, betSlot.item.stack);
                betSlot.item.TurnToAir();
            }
            betSlot.EnableInteract();
        }

        public void ResetGame()
        {
            isGameActive = false;
            dealerTurn = false;
            dealerDrawDelayTimer = 0;
            playerCards.Clear();
            dealerCards.Clear();
            dealingQueue.Clear();
            currentDealingCard = null;
            playerHandValue = 0;
            dealerHandValue = 0;
            dealerFirstCardRevealed = false;
            flippingDealerCard = false;
            dealerCardFlipProgress = 0f;
            gameStatus = string.Empty;
            WithdrawBetItem(Main.LocalPlayer);
        }

        public void ShuffleCards()
        {
            dealerStatus2 = "...";
            playerStatus2 = "...";
            betSlot.DisableInteract();
            // Randomly shuffle the deck before a new game starts
            Random rng = new Random();
            int n = cardList.Count;
            // Classic Fisher–Yates shuffle algorithm
            while (n > 1)
            {
                int k = rng.Next(n--);
                int temp = cardList[n];
                cardList[n] = cardList[k];
                cardList[k] = temp;
            }

            // RiggedShuffle(cardList);

            // Reset card index
            cardIndex = 0;
        }

        // For debugging purposes. Kept here for reference
        private void RiggedShuffle(List<int> l)
        {
            // This sequence sets the player up for the highest possible number of cards without busting. UI edge case
            l[0] = 0;
            l[1] = 51;
            l[2] = 13;
            l[3] = 50;
            l[4] = 26;
            l[5] = 39;
            l[6] = 1;
            l[7] = 14;
            l[8] = 27;
            l[9] = 40;
            l[10] = 2;
            l[11] = 15;
            l[12] = 28;

        }

        public void InitialDeal()
        {
            // Called when the player presses Play.
            // Sets up the hands and deals the opening two cards.
            // Set game as active
            isGameActive = true;

            // Clear game status text
            gameStatus = "";

            // Reset dealing queue
            dealingQueue.Clear();
            currentDealingCard = null;
            dealerTurn = false;
            flippingDealerCard = false;
            dealerCardFlipProgress = 0f;

            // Deal one card to player and one to dealer and repeat
            playerCards.Clear();
            dealerCards.Clear();
            for (int i = 0; i < 2; i++)
            {
                if (cardIndex < cardList.Count)
                {
                    int playerCard = cardList[cardIndex++];
                    Vector2 playerEndPos = GetCardEndPos(true, playerCards.Count);
                    QueueDealCard(playerCard, true, playerEndPos);

                    int dealerCard = cardList[cardIndex++];
                    Vector2 dealerEndPos = GetCardEndPos(false, dealerCards.Count);
                    QueueDealCard(dealerCard, false, dealerEndPos);
                }
            }
            dealerFirstCardRevealed = false;
        }

        public void DealCard()
        {
            // Deal one card
            if (cardIndex < cardList.Count)
            {
                int card = cardList[cardIndex++];
                Vector2 endPos = GetCardEndPos(true, playerCards.Count);
                QueueDealCard(card, true, endPos);
            }

            // Further logic handled when the card finishes animating
        }

        public void DealerLogic()
        {
            // Called when the player stands. Begins the dealer's drawing phase.
            dealerTurn = true;
            dealerDrawDelayTimer = DealerDrawDelay;
            StartDealerFlip();
            TryQueueDealerCard();
        }

        private void TryQueueDealerCard()
        {
            // Handles the dealer drawing additional cards while respecting a small delay between draws
            if (dealerDrawDelayTimer > 0)
            {
                dealerDrawDelayTimer--;
                return;
            }

            if (dealerHandValue < 17 || (dealerHandValue == 17 && IsSoftHand(dealerCards)))
            {
                if (cardIndex < cardList.Count)
                {
                    int card = cardList[cardIndex++];
                    Vector2 endPos = GetCardEndPos(false, dealerCards.Count);
                    QueueDealCard(card, false, endPos);
                    dealerDrawDelayTimer = DealerDrawDelay;
                }
            }
            else
            {
                DetermineWinner();
                dealerTurn = false;
            }
        }

        private void Payout(string outcome)
        {
            switch (outcome) {
                case "Blackjack":
                    // 3 to 2 payout
                    betSlot.item.stack = betSlot.item.stack + (int) (betSlot.item.stack * 1.5f);
                    OverstackHelper(betSlot.item);
                    break;
                case "Win":
                    // 1 to 1 payout
                    betSlot.item.stack = betSlot.item.stack * 2;
                    OverstackHelper(betSlot.item);
                    break;
                case "Lose":
                    // Lose the items
                    betSlot.item.TurnToAir();
                    break;
                case "Push":
                    // Nothing happens
                    break;
                default:
                    break;
            }
            betSlot.EnableInteract();
        }

        private void OverstackHelper(Item item)
        {
            while (item.stack > item.maxStack)
            {
                if (item.stack - item.maxStack > item.maxStack)
                {
                    Item.NewItem(item.GetSource_Misc("Blackjack"), Main.LocalPlayer.position, item.type, item.maxStack);
                    item.stack -= item.maxStack;
                }
                else
                {
                    Item.NewItem(item.GetSource_Misc("Blackjack"), Main.LocalPlayer.position, item.type, item.stack - item.maxStack);
                    item.stack = item.maxStack;
                }
            }

        }

        private void DetermineWinner()
        {
            // Compare hand values and announce the result
            StartDealerFlip();
            int betValue = BetItem?.value ?? 0;
            if (dealerHandValue > 21)
            {
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.DealerBust");
                Payout("Win");
            }    
            else if (playerHandValue > dealerHandValue)
            {
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.PlayerWin");
                Payout("Win");
            }
            else if (playerHandValue < dealerHandValue)
            {
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.DealerWin");
                Payout("Lose");
            }
            else
            {
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.Push");
                Payout("Push");
            }
            isGameActive = false;
        }

        private void CalculateHandValues()
        {
            // Update cached totals for both hands
            playerHandValue = CalculateValue(playerCards);
            dealerHandValue = CalculateValue(dealerCards);

            if (!isGameActive)
                return;

            // Check for automatic push due to both the player and dealer having 21 with two cards
            if (playerCards.Count == 2 && dealerCards.Count == 2 && playerHandValue == 21 && dealerHandValue == 21)
            {
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.PushBlackjack");
                Payout("Push");
                isGameActive = false;
                dealerTurn = false;
                StartDealerFlip();
            }
            // Then, check for player natural - only evaluate once the dealer has received their second card
            else if (playerCards.Count == 2 && dealerCards.Count >= 2 && playerHandValue == 21)
            {
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.PlayerBlackjack");
                SoundEngine.PlaySound(SoundID.Meowmere);
                Payout("Blackjack");
                isGameActive = false;
                dealerTurn = false;
                StartDealerFlip();
            }
            else if (dealerCards.Count == 2 && dealerHandValue == 21)
            {
                // Reveal the dealer's hidden card before resolving the round
                StartDealerFlip();
                gameStatus = Language.GetTextValue("Mods.Blackjack.UI.DealerBlackjack");
                pendingDealerNatural = true;
                isGameActive = false;
                dealerTurn = false;
            }
        }

        private int CalculateValue(List<int> hand)
        {
            // Computes the best numeric value for a list of cards
            int value = 0;
            int aceCount = 0;

            foreach (int card in hand)
            {
                int rank = card % 13;
                if (rank >= 10) // Face cards
                {
                    value += 10;
                }
                else if (rank == 0) // Ace
                {
                    aceCount++;
                    value += 11; // Initially count ace as 11
                }
                else
                {
                    value += rank + 1; // Number cards
                }
            }

            // Adjust for aces if value is over 21
            while (value > 21 && aceCount > 0)
            {
                value -= 10;
                aceCount--;
            }

            return value;
        }

        private bool IsSoftHand(List<int> hand)
        {
            // Determines if the provided hand's value counts any ace as 11
            int minValue = 0;

            foreach (int card in hand)
            {
                int rank = card % 13;
                if (rank >= 10)
                {
                    minValue += 10;
                }
                else if (rank == 0)
                {
                    minValue += 1;
                }
                else
                {
                    minValue += rank + 1;
                }
            }

            int bestValue = CalculateValue(hand);
            return bestValue > minValue;
        }

        // Calculates the destination position for a card in a centered hand
        private Vector2 GetCardEndPos(bool toPlayer, int cardIndexInHand)
        {
            CalculatedStyle dims = GetDimensions();
            float centerX = dims.X + dims.Width / 2f;
            float cardWidth = ModContent.GetInstance<Appearance>().BlackjackCardScale * uiScale;
            int totalCards = (toPlayer ? playerCards.Count : dealerCards.Count) + 1;
            float average = (1 + totalCards) / 2f;
            float x = centerX - ((cardIndexInHand + 1 - average) * 150) - (cardWidth / 2f);
            float y = dims.Y + (toPlayer ? 340 * uiScale : 30 * uiScale);
            return new Vector2(x, y);
        }

        private int GetCardWidth()
        {
            return cardWidth;
        }

        private int GetCardHeight()
        {
            return cardHeight;
        }

        private void StartDealerFlip()
        {
            // Begins the animation that reveals the dealer's hidden card
            if (flippingDealerCard || dealerFirstCardRevealed)
                return;
            flippingDealerCard = true;
            dealerCardFlipProgress = 0f;
        }

        private void QueueDealCard(int cardIndex, bool toPlayer, Vector2 endPos)
        {
            CalculatedStyle dims = GetDimensions();
            Vector2 start = new Vector2(dims.X + dims.Width - cardWidth - 20, dims.Y + dims.Height / 2 - stackHeight / 2);
            Vector2 finalPos = endPos;

            dealingQueue.Enqueue(new DealingCard
            {
                CardIndex = cardIndex,
                StartPos = start,
                EndPos = finalPos,
                Progress = 0f,
                ToPlayer = toPlayer
            });
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            Vector2 position = dims.Position();

            base.DrawSelf(spriteBatch);

            int centerX = (int)dims.Center().X;

            // Dealer hand value text. Should be rendered above the dealer's cards
            dealerStatus1 = Language.GetTextValue("Mods.Blackjack.UI.DealerHand");
            if (isGameActive && !dealerFirstCardRevealed && dealerCards.Count > 1)
            {
                dealerStatus2 = (dealerCards[1] % 13 >= 10 ? 10 : (dealerCards[1] % 13) + 1).ToString() + " + ???";
            }
            else if (dealerCards.Count <= 1)
            {
                dealerStatus2 = "...";
            }
            else
            {
                dealerStatus2 = dealerHandValue.ToString();
            }

            // Player hand value text. Should be rendered above the player's cards
            playerStatus1 = Language.GetTextValue("Mods.Blackjack.UI.PlayerHand");
            if (isGameActive && playerCards.Count > 0)
                playerStatus2 = playerHandValue.ToString();

            if (isGameActive)
            {
                spriteBatch.DrawString(font, dealerStatus1, dims.Position() + (new Vector2(20, 30) * uiScale), Color.White);
                spriteBatch.DrawString(font, dealerStatus2, dims.Position() + new Vector2(20, 50) * uiScale, Color.Yellow);
                spriteBatch.DrawString(font, playerStatus1, dims.Position() + new Vector2(20, 350) * uiScale, Color.White);
                spriteBatch.DrawString(font, playerStatus2, dims.Position() + new Vector2(20, 370) * uiScale, Color.Yellow);
            }

            // Render player cards
            for (int i = 0; i < playerCards.Count; i++)
            {
                // Get card texture
                int cardIndex = playerCards[i];
                Asset<Texture2D> cardTextureAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_{cardIndex}");

                // The rectangle to draw the card in
                // To be able to position these cards in the center, divide the card count by 2 and place accordingly
                float average = (1 + playerCards.Count) / 2f;
                Rectangle cardRectangle = new Rectangle((int)(centerX - ((i + 1 - average) * 150 * uiScale) - (cardWidth / 2)), (int)(dims.Y + 340 * uiScale), cardWidth, cardHeight);

                spriteBatch.Draw(cardTextureAsset.Value, cardRectangle, Color.White);
            }

            // Render dealer cards
            for (int i = 0; i < dealerCards.Count; i++)
            {
                // Get card texture
                int cardIndex = dealerCards[i];
                Asset<Texture2D> cardBackAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_back");
                Asset<Texture2D> cardFrontAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_{cardIndex}");
                Texture2D cardTexture;
                Rectangle cardRectangle;

                float dealerAverage = (1 + dealerCards.Count) / 2f;
                int baseX = (int)(centerX - ((i + 1 - dealerAverage) * 150 * uiScale) - (cardWidth / 2));

                if (i == 0 && (!dealerFirstCardRevealed || flippingDealerCard))
                {
                    float scale;
                    if (flippingDealerCard)
                    {
                        if (dealerCardFlipProgress < 0.5f)
                        {
                            cardTexture = cardBackAsset.Value;
                            scale = 1f - dealerCardFlipProgress * 2f;
                        }
                        else
                        {
                            cardTexture = cardFrontAsset.Value;
                            scale = (dealerCardFlipProgress - 0.5f) * 2f;
                        }
                    }
                    else
                    {
                        cardTexture = cardBackAsset.Value;
                        scale = 1f;
                    }

                    int width = (int)(cardWidth * MathHelper.Clamp(scale, 0f, 1f));
                    int x = baseX + ((int)cardWidth - width) / 2;
                    cardRectangle = new Rectangle(x, (int)(position.Y + 30 * uiScale), width, cardHeight);
                }
                else
                {
                    cardTexture = cardFrontAsset.Value;
                    cardRectangle = new Rectangle(baseX, (int)(position.Y + 30 * uiScale), cardWidth, cardHeight);
                }

                spriteBatch.Draw(cardTexture, cardRectangle, Color.White);
            }


            // Draw animated card
            if (currentDealingCard != null)
            {
                Vector2 pos = Vector2.Lerp(
                    currentDealingCard.StartPos,
                    currentDealingCard.EndPos,
                    currentDealingCard.Progress
                );
                Asset<Texture2D> cardFrontAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_back");
                Rectangle cardRectangle = new Rectangle((int)pos.X, (int)pos.Y, cardWidth, cardHeight);
                spriteBatch.Draw(cardFrontAsset.Value, cardRectangle, Color.White);
            }

            // Draw the card stack
            Asset<Texture2D> cardStackAsset = ModContent.Request<Texture2D>("Blackjack/Assets/Cards/card_stack");
            Rectangle stackRectangle = new Rectangle((int)(position.X + dims.Width - cardWidth - 20), (int)(position.Y + dims.Height / 2 - stackHeight / 2), cardWidth, stackHeight);
            spriteBatch.Draw(cardStackAsset.Value, stackRectangle, Color.White);

            // Render game status text centered on the panel
            Vector2 statusSize = fontBig.MeasureString(gameStatus);
            float panelLeft = dims.Width / 2f - statusSize.X / 2f - 20f;
            float panelTop = dims.Height / 2f - statusSize.Y / 2f - 10f;
            statusPanel.Left.Set(panelLeft, 0f);
            statusPanel.Top.Set(panelTop, 0f);
            statusPanel.Width.Set(statusSize.X + 40f, 0f);
            statusPanel.Height.Set(statusSize.Y + 20f, 0f);
            statusPanel.SetText(gameStatus);

            if (gameStatus.Length > 0)
            {
                if (statusPanel.Parent != this)
                    Append(statusPanel);
                statusPanel.Recalculate();
            }
            else if (statusPanel.Parent == this)
            {
                RemoveChild(statusPanel);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (flippingDealerCard)
            {
                dealerCardFlipProgress += dealerCardFlipSpeed;
                if (dealerCardFlipProgress >= 1f)
                {
                    dealerCardFlipProgress = 1f;
                    flippingDealerCard = false;
                    dealerFirstCardRevealed = true;
                }
            }

            if (pendingDealerNatural && !flippingDealerCard && dealerFirstCardRevealed)
            {
                Payout("Lose");
                pendingDealerNatural = false;
            }

            if (currentDealingCard == null && dealingQueue.Count > 0)
            {
                currentDealingCard = dealingQueue.Dequeue();
            }

            if (currentDealingCard != null)
            {
                currentDealingCard.Progress += dealSpeed;
                if (currentDealingCard.Progress >= 1f)
                {
                    // Animation done, add to hand
                    if (currentDealingCard.ToPlayer)
                        playerCards.Add(currentDealingCard.CardIndex);
                    else
                        dealerCards.Add(currentDealingCard.CardIndex);

                    CalculateHandValues();
                    DealingCard finished = currentDealingCard;
                    currentDealingCard = null;

                    if (finished.ToPlayer)
                    {
                        if (playerHandValue > 21)
                        {
                            gameStatus = Language.GetTextValue("Mods.Blackjack.UI.PlayerBust");
                            Payout("Lose");
                            isGameActive = false;
                            StartDealerFlip();
                        }
                        else if (playerHandValue == 21)
                        {
                            if (isGameActive)
                                DealerLogic();
                        }
                    }
                    else if (dealerTurn)
                    {
                        TryQueueDealerCard();
                    }
                }
            }

            if (dealerTurn && currentDealingCard == null && dealingQueue.Count == 0)
            {
                TryQueueDealerCard();
            }
        }

    }
}
