# ASKA HarvestSpawner Notes
Some notes of what I learned(and how I specifically interpreted things which may not be using the correct terminology but I'm trying to make it as accurate as possible).

## **HarvestSpawner Loot Components**

These are some things I learned while modifying the physical crawler eggs and the iron deposits.

### **chanceComponent**

Found in: `HarvestSpawner.pieceLoot[index].chanceComponent`
  - (The index is a number identifying a specific `HarvestSpawner.Loot` entry)

The mod initally didn't work after modifying the values, despite the debug-to-console string in bepinex console telling me they were successfully modified for each harvest. (Also, the crawler eggs did not have fullLootStack like how the iron deposit items had)

I tested the mod by configuring all four items to 100% and only the slime dropped each time.

Looking at the egg's within Unity Explorer while in the game showed that the slime(which has a default drop rate of 100%) showed that chanceComponent = false, while the others = true.

I used Unity Explorer to manually change that egg's chanceComponent to false for each of the three items, then harvested it and all four items dropped.

I created a separate Harmony patch that targeted HarvestSpawner.Awake(because I couldn't find a path to do this from ItemInfoDatabase) and targeted it to force all values to equal false.

Then I recompiled the mod and reopened the game and tested it out. It was successful. **BUT!!** the next day I tried it by using the games default values, and the only thing that dropped was the slime.

I dug around more and reasoned that chanceComponent needs to equal false ONLY if it's 100%. Anything else while false didn't work.

I did give AI the ISIL files to try to confirm this and also make sure that it didn't affect anything else but that didn't conclude any useful results other than what I already discovered.

In the Better Mines mod, I handled this with: `chanceComponent = dropChance < 1.0f;` with `dropChance` being the .Value of the item variable. 

Also, unlike the iron ore patch in the Better Mines mod, Unity Explorer showed that .bitLoot didn't contain any indexed item components, which is why I only target .pieceLoot

### **fullLootStack**

Found in: `HarvestSpawner.bitLoot[index].fullLootStack` and `HarvestSpawner.pieceLoot[index].fullLootStack`
  - (The index is a number identifying a specific `HarvestSpawner.Loot` entry)

This flag affects how the awarded item quantity gets spawned, rather than the probability of receiving that item.

The ASKA methods I investigated related to this was:
  - `HarvestSpawner._GetNextItemComponent()`
  - `HarvestSpawner._GetAwardedLootCount()`
  - `HarvestSpawner._OnPieceHarvested()`
  - `HarvestSpawner._OnBitHarvested()`

I first adjusted the quantity of the ores by using `ItemInfoDatabase.Initialize` and filtering through to target `("Item_Stone_IronDeposit")` then `.components[index]`.
`[0]` = Large Stone and `[1]` = iron ore. 
While in the game, using Unity Explorer, I could see each indexed item had .quantity and .randomQuantity which I interpreted as a minimum and maximum. 
Changing these still achieved no difference in output, despite my debug.Log telling me they were changed. 
I saw the fullLootStack boolean and thought maybe changing that would help to drop the determined quantity as a single loot stack with multiple items within.
This wasn't achievable with `ItemInfoDatabase`, so I used `._OnBitHarvested()` for the boolean change. This actually produced the original results I wanted, but didn't place it in one stack, it dropped all items individually.
`._OnBitHarvested` wasn't changing the large stones, though. Through Unity Explorer I could see that Iron Deposits Loot item Large Stones contained drops related to bitLoots 10-14 and pieceLoots 1-4, while iron ore was tied to bitLoots 1-14 and pieceLoots 1-4.
  - I determined that these are the little 'mining rewards' after a certain amount of damage is done throughout the entire Iron Deposit's health. This is why the mod changes fullLootStack on pieceLoot and bitLoot.

Having separate patches for these booleans does help make it so these booleans only trigger and change when activated and only target the item specified.













