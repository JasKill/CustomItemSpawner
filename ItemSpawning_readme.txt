# How to use ItemSpawnInfo.txt

# Spawn Groups
# Spawn Groups are IDs used to link your 'Spawn Points' to the 'Items lists'.
# Example: This will tell the key: 'lcz_armory_frags' to spawn 3 Frag grenades. 
# lcz_armory_frags:25,25,25
# Note: The spawn points you make must use the same key 'lcz_armory_frags'.

# When the server spawns items, it will:
# Go through all the 'Spawn Groups' and tell each group to spawn an item until ALL groups have spawned their items (Or no more spawn points).

# Each 'Spawn group' will spawn its items in order:

# First: Spawn all items assigned to spawn. (* and 0 to 36)
# Example: This will force the HID room to spawn the MicroHID.
# [Spawn Groups]
# Hid:16

# Second: Spawn an item from a 'Queued List' (Until the Queued list is empty).
# Example, this will make sure at least 2 checkpoint key cards will spawn somewhere in LCZ.
# [Queued Lists]
# SpawnLCZ:3,3
# [Spawn Groups]
# LCZ_Toilets:SpawnLCZ
# LCZ_372:SpawnLCZ
# LCZ_Cafe:SpawnLCZ
# LCZ_173:SpawnLCZ

# Third: Any 'Item Lists' you attached to the 'Spawn Group' will spawn a random item from that list.
# Example: You can use this for rarities.
# [Item Lists]
# HighRarity:21,25
# LowRarity:12,14,15
# [Spawn Groups]
# LCZ_Armory:LowRarity,LowRarity,LowRarity,HighRarity,HighRarity
# (This will spawn 3 random items from the LowRarity list and 2 items from the HighRarity list in Light Containment Armory.

# -Again, the difference between a Queued List and Item List is: A Queued list will spawn all the items inside it, across all the groups it is attached to. Where an Item List will only spawn 1 random item inside the list.
# -You can add an Item List to a Queued List, but you can't add a Queued List to an Item List, or an Item List to an Item List.
# -For spawn points inside duplicate rooms (like Plant Room), the items will be split across those rooms.

# *NEW* Modifiers
# % Chance To Spawn -You can give your Lists and Items a chance to spawn (1-100%).
# # Copies          -You can create more than one item per spawn point (1-20).

# Example: When the game tells this group to spawn a pistol, there's a 50% chance it will spawn two pistols!
# RandomPistol:13%50#2

# [Items]

# *=Random Item
# 0=KeycardJanitor
# 1=KeycardScientist
# 2=KeycardScientistMajor
# 3=KeycardZoneManager
# 4=KeycardGuard
# 5=KeycardSeniorGuard
# 6=KeycardContainmentEngineer
# 7=KeycardNTFLieutenant
# 8=KeycardNTFCommander
# 9=KeycardFacilityManager
# 10=KeycardChaosInsurgency
# 11=KeycardO5
# 12=Radio
# 13=GunCOM15
# 14=Medkit
# 15=Flashlight
# 16=MicroHID
# 17=SCP500
# 18=SCP207
# 19=WeaponManagerTablet
# 20=GunE11SR
# 21=GunProject90
# 22=Ammo556
# 23=GunMP7
# 24=GunLogicer
# 25=GrenadeFrag
# 26=GrenadeFlash
# 27=Disarmer
# 28=Ammo762
# 29=Ammo9mm
# 30=GunUSP
# 31=SCP018
# 32=SCP268
# 33=Adrenaline
# 34=Painkillers
# 35=Coin
# 36=None

[Item Lists]
# Example lists below

Garbage:0,1,15,19,22,26,28,29,35
Common:2,3,4,12,14,23,25,33,34
Uncommon:5,6,17,18,24,30,31,32
Rare:7,8,9,13,20,21
VeryRare:10,11,16

[Queued Lists]
# Example queue below

SpawnOneOfEachItem:3,2,3,13,Garbage,Garbage,Uncommon,Common

[Spawn Groups]
# Group Name : Item Data (1,6,Rare,Uncommon,etc)

Cafe_Scientist_keycard:1
012_mScientist_keycard:3
toilet_keycard:1,0
Fireman:7
Servers:1
Nuke:4
LC_Armory:23
MicroHID:16
LC_Armory_Pistol:13,21
LC_Armory_Box:19,19
LC_Armory_Ammo:29,28,28,29
RandomPistol:13
LC_Armory_Smoke:26,26,26
LC_Armory_Positron:25,25,25
SFA_TCross:22,28,29,22,29,22,28,22,29,22
ArmTablet:19,19,19

[Containers]
# Containers have a max number of spawn points, listed below with a description.

medkit:14,14,14,14,14
# Max Spawns: [5] -First aid box.

glockere11:20
# Max Spawns: [1] -Large container that spawns GunE11SR.

glocker556:22,22,22,22,22
# Max Spawns: [5] -Ammo spawn point for the GunE11SR.

glocker-b-small:14%67,22%33#4,25%68#2,22%33#3,29%33#4,26%53,29%33#6,28%33#5,28%33#3,30%58,28,28,28#2,22#2
# Max Spawns: [9] -Large 3x3 locker. (Small boxes).

glocker-b-big:19,19%60,19%33,21%40,23%40,30%40,27,27,27,27,27
# Max Spawns: [6] -Large 3x3 locker. (big boxes).

pedestal:31,32,17,18,17%74,18%50,17,18%50,17,17,17
# Max Spawns: [8] -The glass boxes.

misclocker:14,15,15%50,1,1,1,3,14,14%50,34,34%50#2,28#4,29,33,33,22#3
# Max Spawns: [24] -The basic locker.
