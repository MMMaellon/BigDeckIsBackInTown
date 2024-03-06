# Big Deck Is Back In Town

VRChat prefab for performant (hopefully) playing cards.
https://github.com/MMMaellon/BigDeckIsBackInTown

# Ask questions on my [Discord Server](https://discord.gg/S5sDC4PnFp)

# Download Instructions

Install through VCC:
<https://mmmaellon.github.io/BigDeckIsBackInTown/>

Install all my prefabs through VCC:
<https://mmmaellon.github.io/MMMaellonVCCListing/>

# Setup Instructions

Drag in one of the prefabs, based on what you want.
If you want to add or remove cards, make sure to update the "CARDS" array on the deck to reflect your changes. Same goes for adding or removing CardPlacementSpots.

CardPlacementSpots are the places where a card will be placed if you throw a card or press and hold the "Deal Card" buttons. You can make custom versions of
them to support different game types. For example, if you download the [example project](#example) you can see an example of a CardPlacementSpot that deals cards in a row of 5 instead of just one spot. This can be useful for games like poker where each player needs 5 cards.

You can toggle off the throwing on the CardPlacementSpots if you just want the buttons.

On each button is a "ClickAndHoldButton" script. To remove the click and hold mechanic, remove the script and call the udon event that the script is configured to call directly.

# Example Project (#example)

Follow these steps to open up the example project:
1. Click Window > Package Manager
2. Set the drop-down in the top left to "In Project" and select "Big Deck Is Back In Town" under the "custom" category
3. Import one of the example scenes

