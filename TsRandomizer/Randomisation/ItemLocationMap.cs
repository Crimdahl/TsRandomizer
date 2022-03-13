using System.Collections.Generic;
using System.Linq;
using Timespinner.GameAbstractions.Gameplay;
using Timespinner.GameAbstractions.Inventory;
using Timespinner.GameAbstractions.Saving;
using Timespinner.GameObjects.BaseClasses;
using TsRandomizer.IntermediateObjects;
using TsRandomizer.ReplacementObjects;
using R = TsRandomizer.Randomisation.Requirement;

namespace TsRandomizer.Randomisation
{
	class ItemLocationMap : LookupDictionary<ItemKey, ItemLocation>
	{
		internal R OculusRift;
		internal R MawGassMask;

		internal Gate AccessToPast;
		internal Gate AccessToLakeDesolation;

		internal Gate MultipleSmallJumpsOfNpc;
		internal Gate DoubleJumpOfNpc;
		internal Gate ForwardDashDoubleJump;
		internal Gate LowerLakeDesolationBridge;

		//past
		internal Gate LeftSideForestCaves;
		internal Gate UpperLakeSirine;
		internal Gate LowerLakeSirine;
		internal Gate LowerCavesOfBanishment;
		internal Gate UpperCavesOfBanishment;
		internal Gate CastleRamparts;
		internal Gate CastleKeep;
		internal Gate RoyalTower;
		internal Gate MidRoyalTower;
		internal Gate UpperRoyalTower;
		internal Gate KillMaw;
		//future
		internal Gate UpperLakeDesolation;
		internal Gate LeftLibrary;
		internal Gate UpperLeftLibrary;
		internal Gate IfritsLair;
		internal Gate MidLibrary;
		internal Gate UpperRightSideLibrary;
		internal Gate RightSideLibraryElevator;
		internal Gate LowerRightSideLibrary;
		internal Gate SealedCavesLeft;
		internal Gate SealedCavesLower;
		internal Gate SealedCavesSirens;
		internal Gate MilitaryFortress;
		internal Gate RavenlordsLair;
		internal Gate MilitaryFortressHangar;
		internal Gate RightSideMilitaryFortressHangar;
		internal Gate TheLab;
		internal Gate TheLabPoweredOff;
		internal Gate UpperLab;
		internal Gate EmperorsTower;
		//pyramid
		internal Gate TemporalGyre;
		internal Gate LeftPyramid;
		internal Gate Nightmare;

		public new ItemLocation this[ItemKey key] => GetItemLocationBasedOnKeyOrRoomKey(key);

		protected readonly ItemInfoProvider ItemProvider;
		protected readonly ItemUnlockingMap UnlockingMap;
		protected readonly SeedOptions SeedOptions;

		string areaName;

		public ItemLocationMap(ItemInfoProvider itemInfoProvider, ItemUnlockingMap itemUnlockingMap, SeedOptions options)
			: base(CalculateCapacity(options), l => l.Key)
		{
			ItemProvider = itemInfoProvider;
			UnlockingMap = itemUnlockingMap;
			SeedOptions = options;

			SetupGates();

			AddPresentItemLocations();
			AddPastItemLocations();
			AddPyramidItemLocations();

			if (options.GyreArchives)
				AddGyreItemLocations();

			if (options.DownloadableItems)
				AddDownloadTerminals();

			if (options.Cantoran)
				AddCantoran();

			if (options.LoreChecks)
				AddLoreLocations();

			if (options.StartWithTalaria)
				Add(new ExteralItemLocation(itemInfoProvider.Get(EInventoryRelicType.Dash)));
		}

		void SetupGates()
		{
			OculusRift = (SeedOptions.RequireEyeOrbRing)
				? R.OculusRift
				: R.None;

			MawGassMask = (SeedOptions.GassMaw)
				? R.GassMask
				: R.None;

			AccessToLakeDesolation = (!SeedOptions.Inverted)
				? (Gate)R.None
				: R.GateLakeDesolation
				| R.GateKittyBoss
				| R.GateLeftLibrary
				| R.GateSealedCaves
				| (R.GateSealedSirensCave & R.CardE)
				| (R.GateMilitaryGate & (R.CardE | R.CardB));

			LowerLakeDesolationBridge = AccessToLakeDesolation & (R.TimeStop | R.ForwardDash | R.GateKittyBoss | R.GateLeftLibrary);


			AccessToPast = (SeedOptions.Inverted)
				? (Gate)R.None
				: (
					R.TimespinnerWheel & R.TimespinnerSpindle
					& (
						(LowerLakeDesolationBridge & R.CardD)
						| (R.GateSealedSirensCave & R.CardE)
						| (R.GateMilitaryGate & (R.CardB | R.CardE))
					)
				) //libraryTimespinner
				| R.GateLakeSereneLeft
				| R.GateAccessToPast
				| R.GateLakeSereneRight
				| R.GateRoyalTowers
				| R.GateCastleRamparts
				| R.GateCastleKeep
				| (MawGassMask & (R.GateCavesOfBanishment | R.GateMaw));

			MultipleSmallJumpsOfNpc = (Gate)(R.TimespinnerWheel | R.UpwardDash);
			DoubleJumpOfNpc = (R.DoubleJump & R.TimespinnerWheel) | R.UpwardDash;
			ForwardDashDoubleJump = (R.ForwardDash & R.DoubleJump) | R.UpwardDash;

			//past
			LeftSideForestCaves = (AccessToPast & (R.TimeStop | R.ForwardDash)) | R.GateLakeSereneRight | R.GateLakeSereneLeft;
			UpperLakeSirine = (LeftSideForestCaves & (R.TimeStop | R.Swimming)) | R.GateLakeSereneLeft;
			LowerLakeSirine = (LeftSideForestCaves | R.GateLakeSereneLeft) & R.Swimming;
			LowerCavesOfBanishment = LowerLakeSirine | R.GateCavesOfBanishment | (R.GateMaw & R.DoubleJump);
			UpperCavesOfBanishment = AccessToPast;
			CastleRamparts = AccessToPast;
			CastleKeep = CastleRamparts;
			RoyalTower = (CastleKeep & R.DoubleJump) | R.GateRoyalTowers;
			MidRoyalTower = RoyalTower & (MultipleSmallJumpsOfNpc | ForwardDashDoubleJump);
			UpperRoyalTower = MidRoyalTower & R.DoubleJump;
			KillMaw = (LowerLakeSirine | R.GateCavesOfBanishment | R.GateMaw) & MawGassMask;
			var killTwins = CastleKeep & R.TimeStop;
			var killAelana = UpperRoyalTower;

			//future
			UpperLakeDesolation = AccessToLakeDesolation & UpperLakeSirine & R.AntiWeed;
			LeftLibrary = UpperLakeDesolation | LowerLakeDesolationBridge | R.GateLeftLibrary | R.GateKittyBoss | (R.GateSealedSirensCave & R.CardE) | (R.GateMilitaryGate & (R.CardB | R.CardE));
			MidLibrary = (LeftLibrary & R.CardD) | (R.GateSealedSirensCave & R.CardE) | (R.GateMilitaryGate & (R.CardB | R.CardE));
			UpperLeftLibrary = LeftLibrary & (R.DoubleJump | R.ForwardDash);
			IfritsLair = UpperLeftLibrary & R.Kobo & AccessToPast;
			UpperRightSideLibrary = (MidLibrary & (R.CardC | (R.CardB & R.CardE))) | ((R.GateMilitaryGate | R.GateSealedSirensCave) & R.CardE);
			RightSideLibraryElevator = R.CardE & ((MidLibrary & (R.CardC | R.CardB)) | R.GateMilitaryGate | R.GateSealedSirensCave);
			LowerRightSideLibrary = (MidLibrary & R.CardB) | RightSideLibraryElevator | R.GateMilitaryGate | (R.GateSealedSirensCave & R.CardE);
			SealedCavesLeft = (AccessToLakeDesolation & R.DoubleJump) | R.GateSealedCaves;
			SealedCavesLower = SealedCavesLeft & R.CardA;
			SealedCavesSirens = (MidLibrary & R.CardB & R.CardE) | R.GateSealedSirensCave;
			MilitaryFortress = LowerRightSideLibrary & KillMaw & killTwins & killAelana;
			MilitaryFortressHangar = MilitaryFortress;
			RightSideMilitaryFortressHangar = MilitaryFortressHangar & R.DoubleJump;
			TheLab = MilitaryFortressHangar & R.CardB;
			TheLabPoweredOff = TheLab & DoubleJumpOfNpc;
			UpperLab = TheLabPoweredOff & ForwardDashDoubleJump;
			RavenlordsLair = UpperLab & R.MerchantCrow;
			EmperorsTower = UpperLab;

			//pyramid
			TemporalGyre = MilitaryFortress & R.TimespinnerWheel;
			LeftPyramid = UpperLab & (
				R.TimespinnerWheel & R.TimespinnerSpindle &
				R.TimespinnerPiece1 & R.TimespinnerPiece2 & R.TimespinnerPiece3);
			Nightmare = LeftPyramid & R.UpwardDash;
		}

		static int CalculateCapacity(SeedOptions options)
		{
			var capacity = 166;

			if (options.StartWithTalaria)
				capacity += 1;
			if (options.DownloadableItems)
				capacity += 14;
			if (options.GyreArchives)
				capacity += 9;
			if (options.Cantoran)
				capacity += 1;
			if (options.LoreChecks)
				capacity += 22;

			return capacity;
		}

		void AddPresentItemLocations()
		{
			areaName = "Tutorial";
			Add(ItemKey.TutorialMeleeOrb, "Yo Momma", ItemProvider.Get(EInventoryOrbType.Blue, EOrbSlot.Melee));
			Add(ItemKey.TutorialSpellOrb, "Yo Momma", ItemProvider.Get(EInventoryOrbType.Blue, EOrbSlot.Spell));
			areaName = "Lake Desolation";
			Add(new ItemKey(1, 1, 1528, 144), "Desolation Start lakebed", ItemProvider.Get(EInventoryUseItemType.FuturePotion), AccessToLakeDesolation);
			Add(new ItemKey(1, 15, 264, 144), "Desolation Start upper entrance", ItemProvider.Get(EInventoryEquipmentType.OldCoat), AccessToLakeDesolation);
			Add(new ItemKey(1, 25, 296, 176), "Desolation Start warp gate", ItemProvider.Get(EInventoryUseItemType.FutureHiPotion), AccessToLakeDesolation);
			Add(new ItemKey(1, 9, 600, 144 + TimespinnerWheel.YOffset), "Timespinner Wheel room", ItemProvider.Get(EInventoryRelicType.TimespinnerWheel), AccessToLakeDesolation);
			Add(new ItemKey(1, 14, 40, 176), "Desolation behind azure fire", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), UpperLakeDesolation);
			Add(new RoomItemKey(1, 5), "Feline Sentry", ItemProvider.Get(EInventoryOrbType.Blade, EOrbSlot.Melee), UpperLakeDesolation | LowerLakeDesolationBridge);
			areaName = "Lower Lake Desolation";
			Add(new ItemKey(1, 2, 1016, 384), "Lower Desolation T chest", ItemProvider.Get(EItemType.MaxSand), AccessToLakeDesolation & R.TimeStop);
			Add(new ItemKey(1, 11, 72, 240), "Lower Desolation secret", ItemProvider.Get(EItemType.MaxHP), LowerLakeDesolationBridge & OculusRift);
			Add(new ItemKey(1, 3, 56, 176), "Desolation End frozen Cheveur Tank", ItemProvider.Get(EItemType.MaxAura), AccessToLakeDesolation & R.TimeStop);
			areaName = "Upper Lake Desolation";
			Add(new ItemKey(1, 17, 152, 96), "Upper Desolation Cheveur Tanks", ItemProvider.Get(EInventoryUseItemType.GoldRing), UpperLakeDesolation);
			Add(new ItemKey(1, 21, 200, 144), "Upper Desolation secret", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), UpperLakeDesolation & OculusRift);
			Add(new ItemKey(1, 20, 232, 96), "Upper Desolation double top", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), UpperLakeDesolation & R.DoubleJump);
			Add(new ItemKey(1, 20, 168, 240), "Upper Desolation double bottom", ItemProvider.Get(EInventoryUseItemType.FuturePotion), UpperLakeDesolation);
			Add(new ItemKey(1, 22, 344, 160), "Upper Desolation 3 Sparrows", ItemProvider.Get(EInventoryUseItemType.FutureHiPotion), UpperLakeDesolation);
			Add(new ItemKey(1, 18, 1320, 189), "Desolation Crash Site pedestal", ItemProvider.Get(EInventoryOrbType.Moon, EOrbSlot.Melee), UpperLakeDesolation);
			Add(new ItemKey(1, 18, 1272, 192), "Desolation Crash Site chest", ItemProvider.Get(EInventoryEquipmentType.CaptainsCap), UpperLakeDesolation & R.GassMask & KillMaw);
			Add(new ItemKey(1, 18, 1368, 192), "Desolation Crash Site chest", ItemProvider.Get(EInventoryEquipmentType.CaptainsJacket), UpperLakeDesolation & R.GassMask & KillMaw);
			areaName = "Library";
			Add(new ItemKey(2, 60, 328, 160), "Library entrance", ItemProvider.Get(EItemType.MaxHP), LeftLibrary);
			Add(new ItemKey(2, 54, 296, 176), "Library warp gate", ItemProvider.Get(EInventoryRelicType.ScienceKeycardD), LeftLibrary);
			Add(new ItemKey(2, 41, 404, 246), "Librarian", ItemProvider.Get(EInventoryRelicType.Tablet), LeftLibrary);
			Add(new ItemKey(2, 44, 680, 368), "Library central nook", ItemProvider.Get(EInventoryRelicType.FoeScanner), LeftLibrary);
			Add(new ItemKey(2, 47, 216, 208), "Library D-lock", ItemProvider.Get(EInventoryUseItemType.Ether), LeftLibrary & R.CardD);
			Add(new ItemKey(2, 47, 152, 208), "Library D-lock", ItemProvider.Get(EInventoryOrbType.Blade, EOrbSlot.Passive), LeftLibrary & R.CardD);
			Add(new ItemKey(2, 47, 88, 208), "Library D-lock", ItemProvider.Get(EInventoryOrbType.Blade, EOrbSlot.Spell), LeftLibrary & R.CardD);
			areaName = "Library Top";
			Add(new ItemKey(2, 56, 168, 192), "Backer room", ItemProvider.Get(EInventoryUseItemType.GoldNecklace), UpperLeftLibrary);
			Add(new ItemKey(2, 56, 392, 192), "Backer room", ItemProvider.Get(EInventoryUseItemType.GoldRing), UpperLeftLibrary);
			Add(new ItemKey(2, 56, 616, 192), "Backer room", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), UpperLeftLibrary);
			Add(new ItemKey(2, 56, 840, 192), "Backer room", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), UpperLeftLibrary);
			Add(new ItemKey(2, 56, 1064, 192), "Backer room", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), UpperLeftLibrary);
			areaName = "Varndagroth Tower Left";
			Add(new ItemKey(2, 34, 232, 1200), "Left Varndagray outside elevator", ItemProvider.Get(EInventoryUseItemType.FiligreeTea), MidLibrary); //Default item is Jerky, got replaced by FiligreeTea
			Add(new ItemKey(2, 40, 344, 176), "Varndagray Timespinner room", ItemProvider.Get(EInventoryRelicType.ScienceKeycardC), MidLibrary);
			Add(new ItemKey(2, 32, 328, 160), "Left Varndagray lower C-lock", ItemProvider.Get(EInventoryUseItemType.GoldRing), MidLibrary & R.CardC);
			Add(new ItemKey(2, 7, 232, 144), "Varndagray secret", ItemProvider.Get(EItemType.MaxAura), MidLibrary & OculusRift);
			Add(new ItemKey(2, 25, 328, 192), "Left Varndagray elevator chest", ItemProvider.Get(EItemType.MaxSand), MidLibrary & R.CardE);
			areaName = "Varndagroth Tower Right";
			Add(new ItemKey(2, 15, 760, 192), "Varndagray tower bridge", ItemProvider.Get(EInventoryUseItemType.FuturePotion), UpperRightSideLibrary);
			Add(new ItemKey(2, 20, 72, 1200), "Right Varndagray elevator chest", ItemProvider.Get(EInventoryUseItemType.Jerky), RightSideLibraryElevator);
			Add(new ItemKey(2, 23, 72, 560), "Right Varndagray vents bottom", ItemProvider.Get(EInventoryUseItemType.FutureHiPotion), UpperRightSideLibrary & (R.CardE | R.DoubleJump)); //needs only UpperRightSideLibrary but requires Elevator Card | Double Jump to get out
			Add(new ItemKey(2, 23, 1112, 112), "Right Varndagray vents right", ItemProvider.Get(EInventoryUseItemType.FutureHiPotion), UpperRightSideLibrary & (R.CardE | R.DoubleJump)); //needs only UpperRightSideLibrary but requires Elevator Card | Double Jump to get out
			Add(new ItemKey(2, 23, 136, 304), "Right Varndagray vents left", ItemProvider.Get(EInventoryRelicType.ElevatorKeycard), UpperRightSideLibrary & (R.CardE | R.DoubleJump)); //needs only UpperRightSideLibrary but requires Elevator Card | Double Jump to get out
			Add(new ItemKey(2, 11, 104, 192), "Right Varndagray bottom floor", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), LowerRightSideLibrary);
			Add(new ItemKey(2, 29, 280, 222 + TimespinnerSpindle.YOffset), "Varndagroth", ItemProvider.Get(EInventoryRelicType.TimespinnerSpindle), RightSideLibraryElevator & R.CardC);
			Add(new RoomItemKey(2, 52), "Varndagray spider hell", ItemProvider.Get(EInventoryRelicType.TimespinnerGear2), RightSideLibraryElevator & R.CardA);
			areaName = "Sealed Caves (Xarion)";
			Add(new ItemKey(9, 10, 248, 848), "Sealed Cave Skeleton", ItemProvider.Get(EInventoryRelicType.ScienceKeycardB), SealedCavesLeft);
			Add(new ItemKey(9, 19, 664, 704), "Sealed Cave Fungus jump", ItemProvider.Get(EInventoryUseItemType.Antidote), SealedCavesLower & R.TimeStop);
			Add(new ItemKey(9, 39, 88, 192), "Sealed Cave Fungus and Ichor", ItemProvider.Get(EInventoryUseItemType.Antidote), SealedCavesLower);
			Add(new ItemKey(9, 41, 312, 192), "Sealed Cave mini jackpot", ItemProvider.Get(EInventoryUseItemType.GalaxyStone), SealedCavesLower & ForwardDashDoubleJump);
			Add(new ItemKey(9, 42, 328, 192), "Sealed Cave waterfall mid", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), SealedCavesLower);
			Add(new ItemKey(9, 12, 280, 160), "Sealed Cave secret", ItemProvider.Get(EItemType.MaxHP), SealedCavesLower & OculusRift);
			Add(new ItemKey(9, 48, 104, 160), "Sealed Cave beside secret", ItemProvider.Get(EInventoryUseItemType.FutureEther), SealedCavesLower);
			Add(new ItemKey(9, 15, 248, 192), "Sealed Cave last chest", ItemProvider.Get(EInventoryUseItemType.FutureEther), SealedCavesLower & R.DoubleJump);
			Add(new RoomItemKey(9, 13), "Xarion", ItemProvider.Get(EInventoryRelicType.TimespinnerGear3), SealedCavesLower);
			areaName = "Sealed Caves (Sirens)";
			Add(new ItemKey(9, 5, 88, 496), "Upper Sealed Cave underwater hook", ItemProvider.Get(EItemType.MaxSand), SealedCavesSirens & R.Swimming);
			Add(new ItemKey(9, 3, 1848, 576), "Upper Sealed Cave sirens right", ItemProvider.Get(EInventoryEquipmentType.BirdStatue), SealedCavesSirens & R.Swimming);
			Add(new ItemKey(9, 3, 744, 560), "Upper Sealed Cave sirens left", ItemProvider.Get(EItemType.MaxAura), SealedCavesSirens & R.Swimming);
			Add(new ItemKey(9, 2, 184, 176), "Upper Sealed Cave end", ItemProvider.Get(EInventoryUseItemType.WarpCard), SealedCavesSirens);
			Add(new ItemKey(9, 2, 104, 160), "Upper Sealed Cave end", ItemProvider.Get(EInventoryRelicType.WaterMask), SealedCavesSirens);
			areaName = "Military Fortress";
			Add(new ItemKey(10, 3, 264, 128), "Hangar Bombers chest", ItemProvider.Get(EItemType.MaxSand), MilitaryFortress & DoubleJumpOfNpc & R.TimespinnerWheel); //can be reached with just upward dash but not with lightwall unless you got timestop
			Add(new ItemKey(10, 11, 296, 192), "Hangar straight from entrance", ItemProvider.Get(EItemType.MaxAura), MilitaryFortress);
			Add(new ItemKey(10, 4, 1064, 176), "Hangar bridge left", ItemProvider.Get(EInventoryUseItemType.FutureHiPotion), MilitaryFortressHangar);
			Add(new ItemKey(10, 10, 104, 192), "Hangar Giantess room", ItemProvider.Get(EInventoryRelicType.AirMask), MilitaryFortressHangar);
			Add(new ItemKey(10, 8, 1080, 176), "Hangar bridge right", ItemProvider.Get(EInventoryEquipmentType.LabGlasses), MilitaryFortressHangar);
			Add(new ItemKey(10, 7, 104, 192), "Hangar B-lock", ItemProvider.Get(EInventoryUseItemType.PlasmaIV), RightSideMilitaryFortressHangar & R.CardB);
			Add(new ItemKey(10, 7, 152, 192), "Hangar B-lock", ItemProvider.Get(EItemType.MaxSand), RightSideMilitaryFortressHangar & R.CardB);
			Add(new ItemKey(10, 18, 280, 189), "Hangar pedestal", ItemProvider.Get(EInventoryOrbType.Gun, EOrbSlot.Melee), RightSideMilitaryFortressHangar & (DoubleJumpOfNpc | ForwardDashDoubleJump));
			areaName = "The Lab";
			Add(new ItemKey(11, 36, 312, 192), "Lab coffee break", ItemProvider.Get(EInventoryUseItemType.FoodSynth), TheLab);
			Add(new ItemKey(11, 3, 1528, 192), "Lab lower trash right", ItemProvider.Get(EItemType.MaxHP), TheLab & R.DoubleJump);
			Add(new ItemKey(11, 3, 72, 192), "Lab lower trash left", ItemProvider.Get(EInventoryUseItemType.FuturePotion), TheLab & R.UpwardDash); //when lab power is on, it only requires DoubleJumpOfNpc, but we cant code for the power state
			Add(new ItemKey(11, 25, 104, 192), "Lab bottom solo Turret", ItemProvider.Get(EItemType.MaxAura), TheLab & R.DoubleJump);
			Add(new ItemKey(11, 18, 824, 128), "Lab frozen trash room", ItemProvider.Get(EInventoryUseItemType.ChaosHeal), TheLabPoweredOff);
			Add(new RoomItemKey(11, 39), "Lab's power supply", ItemProvider.Get(EInventoryOrbType.Eye, EOrbSlot.Melee), TheLabPoweredOff);
			Add(new RoomItemKey(11, 21), "Genza", ItemProvider.Get(EInventoryRelicType.ScienceKeycardA), UpperLab);
			Add(new RoomItemKey(11, 1), "Experiment #13", ItemProvider.Get(EInventoryRelicType.Dash), TheLabPoweredOff);
			Add(new ItemKey(11, 6, 328, 192), "Lab terminal and chest room chest", ItemProvider.Get(EInventoryEquipmentType.LabCoat), UpperLab);
			Add(new ItemKey(11, 27, 296, 160), "Lab secret", ItemProvider.Get(EItemType.MaxSand), UpperLab & OculusRift);
			Add(new RoomItemKey(11, 26), "Lab spider gell", ItemProvider.Get(EInventoryRelicType.TimespinnerGear1), TheLabPoweredOff & R.CardA);
			areaName = "Emperor's Tower";
			Add(new ItemKey(12, 5, 344, 192), "Emperor's courtyard bottom", ItemProvider.Get(EItemType.MaxAura), EmperorsTower);
			Add(new ItemKey(12, 3, 200, 160), "Emperor's secret", ItemProvider.Get(EInventoryEquipmentType.LachiemCrown), EmperorsTower & R.UpwardDash & OculusRift);
			Add(new ItemKey(12, 25, 360, 176), "Emperor's courtyard top", ItemProvider.Get(EInventoryEquipmentType.EmpressCoat), EmperorsTower & R.UpwardDash);
			Add(new ItemKey(12, 22, 56, 192), "Emperor's Galactic Sages", ItemProvider.Get(EItemType.MaxSand), EmperorsTower);
			Add(new ItemKey(12, 9, 344, 928), "Right Emperor's Tower bottom", ItemProvider.Get(EInventoryUseItemType.FutureHiEther), EmperorsTower);
			Add(new ItemKey(12, 19, 72, 192), "Right Emperor's Tower top", ItemProvider.Get(EInventoryEquipmentType.FiligreeClasp), EmperorsTower & DoubleJumpOfNpc);
			Add(new ItemKey(12, 13, 120, 176), "Left Emperor's Tower balcony", ItemProvider.Get(EItemType.MaxHP), EmperorsTower);
			Add(new ItemKey(12, 11, 264, 208), "Emperor's room chest", ItemProvider.Get(EInventoryRelicType.EmpireBrooch), EmperorsTower);
			Add(new ItemKey(12, 11, 136, 205), "Emperor's room pedestal", ItemProvider.Get(EInventoryOrbType.Empire, EOrbSlot.Melee), EmperorsTower);
		}

		void AddCantoran()
		{
			areaName = "Upper Lake Serene";
			Add(new RoomItemKey(7, 5), "Cantoran", ItemProvider.Get(EInventoryOrbType.Barrier, EOrbSlot.Melee), LeftSideForestCaves);
		}

		void AddPastItemLocations()
		{
			areaName = "Refugee Camp";
			Add(new RoomItemKey(3, 0), "Gift from Neliste", ItemProvider.Get(EInventoryOrbType.Flame, EOrbSlot.Melee), AccessToPast);
			Add(new ItemKey(3, 30, 296, 176), "Refugee camp storage", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), AccessToPast);
			Add(new ItemKey(3, 30, 232, 176), "Refugee camp storage", ItemProvider.Get(EInventoryUseItemType.GoldNecklace), AccessToPast);
			Add(new ItemKey(3, 30, 168, 176), "Refugee camp storage", ItemProvider.Get(EInventoryRelicType.JewelryBox), AccessToPast);
			areaName = "Forest";
			Add(new ItemKey(3, 3, 648, 272), "Refugee camp roof", ItemProvider.Get(EInventoryUseItemType.Herb), AccessToPast);
			Add(new ItemKey(3, 15, 248, 112), "Forest banishment overhang chest", ItemProvider.Get(EItemType.MaxAura), AccessToPast & (DoubleJumpOfNpc | ForwardDashDoubleJump | (R.TimeStop & R.ForwardDash)));
			Add(new ItemKey(3, 21, 120, 192), "Forest secret", ItemProvider.Get(EItemType.MaxSand), AccessToPast & OculusRift);
			Add(new ItemKey(3, 12, 776, 560), "Forest three-way chest", ItemProvider.Get(EInventoryEquipmentType.PointyHat), AccessToPast);
			Add(new ItemKey(3, 11, 392, 608), "Waterfall", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), AccessToPast & R.Swimming);
			Add(new ItemKey(3, 5, 184, 192), "Waterfall cave", ItemProvider.Get(EInventoryEquipmentType.Pendulum), AccessToPast & R.Swimming);
			Add(new ItemKey(3, 2, 584, 368), "Forest stairs", ItemProvider.Get(EInventoryUseItemType.Potion), AccessToPast);
			Add(new ItemKey(3, 29, 248, 192), "Serene entrance bat cave", ItemProvider.Get(EItemType.MaxHP), LeftSideForestCaves);
			areaName = "Upper Lake Serene";
			Add(new ItemKey(7, 16, 152, 96), "Upper Serene rat nest", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), UpperLakeSirine);
			Add(new ItemKey(7, 19, 248, 96), "Upper Serene double bottom", ItemProvider.Get(EItemType.MaxAura), UpperLakeSirine & R.DoubleJump);
			Add(new ItemKey(7, 19, 168, 240), "Upper Serene double top", ItemProvider.Get(EInventoryEquipmentType.TravelersCloak), UpperLakeSirine);
			Add(new ItemKey(7, 27, 184, 144), "Upper Serene secret", ItemProvider.Get(EInventoryFamiliarType.Griffin), UpperLakeSirine & OculusRift);
			Add(new RoomItemKey(7, 28), "Serene save room", ItemProvider.Get(EInventoryUseItemType.AlchemistTools), UpperLakeSirine);
			Add(new ItemKey(7, 13, 56, 176), "Serene Queen's ledge", ItemProvider.Get(EInventoryUseItemType.WarpCard), UpperLakeSirine);
			Add(new ItemKey(7, 30, 296, 176), "Serene Queen's warp room", ItemProvider.Get(EInventoryRelicType.PyramidsKey), UpperLakeSirine);
			Add(new ItemKey(7, 3, 120, 204), "Serene Frozen Cheveur ledge", null, UpperLakeSirine);
			areaName = "Lower Lake Serene";
			Add(new ItemKey(7, 3, 440, 1232), "Lower Serene East", ItemProvider.Get(EInventoryUseItemType.Potion), LowerLakeSirine);
			Add(new ItemKey(7, 7, 1432, 576), "Lower Serene under bridge", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), LowerLakeSirine);
			Add(new ItemKey(7, 20, 248, 96), "Lower Serene cave under bridge", ItemProvider.Get(EItemType.MaxSand), LowerLakeSirine);
			Add(new ItemKey(7, 6, 520, 496), "Lower Serene ledge above spikes", ItemProvider.Get(EInventoryUseItemType.Potion), LowerLakeSirine);
			Add(new ItemKey(7, 11, 88, 240), "Lower Serene secret", ItemProvider.Get(EItemType.MaxHP), LowerLakeSirine & OculusRift);
			Add(new ItemKey(7, 2, 1016, 384), "Lower Serene T chest", ItemProvider.Get(EInventoryUseItemType.Ether), LowerLakeSirine);
			Add(new ItemKey(7, 9, 584, 189), "Underwater pedestal", ItemProvider.Get(EInventoryOrbType.Ice, EOrbSlot.Melee), LowerLakeSirine);
			areaName = "Caves of Banishment (Maw)";
			Add(new ItemKey(8, 19, 664, 704), "Lower COB Shroom jump", ItemProvider.Get(EInventoryUseItemType.SilverOre), LowerCavesOfBanishment & R.DoubleJump);
			Add(new ItemKey(8, 12, 280, 160), "Lower COB secret", ItemProvider.Get(EItemType.MaxHP), LowerCavesOfBanishment & OculusRift);
			Add(new ItemKey(8, 48, 104, 160), "Lower COB beside secret", ItemProvider.Get(EInventoryUseItemType.Spaghetti), LowerCavesOfBanishment); //Default item is Herb but got replaced by Spaghetti
			Add(new ItemKey(8, 39, 88, 192), "Lower COB Shrooms and Slime", ItemProvider.Get(EInventoryUseItemType.SilverOre), LowerCavesOfBanishment);
			Add(new ItemKey(8, 41, 168, 192), "Lower COB jackpot room", ItemProvider.Get(EInventoryUseItemType.GoldNecklace), LowerCavesOfBanishment & ForwardDashDoubleJump);
			Add(new ItemKey(8, 41, 216, 192), "Lower COB jackpot room", ItemProvider.Get(EInventoryUseItemType.GoldRing), LowerCavesOfBanishment & ForwardDashDoubleJump);
			Add(new ItemKey(8, 41, 264, 192), "Lower COB jackpot room", ItemProvider.Get(EInventoryUseItemType.EssenceCrystal), LowerCavesOfBanishment & ForwardDashDoubleJump);
			Add(new ItemKey(8, 41, 312, 192), "Lower COB jackpot room", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), LowerCavesOfBanishment & ForwardDashDoubleJump);
			Add(new ItemKey(8, 42, 216, 189), "Lower COB waterfall pedestal", ItemProvider.Get(EInventoryOrbType.Wind, EOrbSlot.Melee), LowerCavesOfBanishment);
			Add(new ItemKey(8, 15, 248, 192), "Lower COB final chest", ItemProvider.Get(EInventoryUseItemType.SilverOre), LowerCavesOfBanishment & R.DoubleJump);
			Add(new RoomItemKey(8, 21), "Lower COB Plasma Crystal", ItemProvider.Get(EInventoryUseItemType.RadiationCrystal), LowerCavesOfBanishment & (MawGassMask | R.ForwardDash));
			Add(new ItemKey(8, 31, 88, 400), "Mineshaft", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), LowerCavesOfBanishment & MawGassMask);
			areaName = "Caves of Banishment (Sirens)";
			Add(new ItemKey(8, 4, 664, 144), "Upper COB Wyverns", ItemProvider.Get(EInventoryUseItemType.SilverOre), UpperCavesOfBanishment);
			Add(new ItemKey(8, 3, 808, 144), "Upper COB sirens dry chest", ItemProvider.Get(EInventoryUseItemType.SilverOre), UpperCavesOfBanishment);
			Add(new ItemKey(8, 3, 744, 560), "Upper COB sirens underwater left", ItemProvider.Get(EInventoryUseItemType.SilverOre), UpperCavesOfBanishment & R.Swimming);
			Add(new ItemKey(8, 3, 1848, 576), "Upper COB sirens underwater right", ItemProvider.Get(EItemType.MaxAura), UpperCavesOfBanishment & R.Swimming);
			Add(new ItemKey(8, 3, 1256, 544), "Upper COB sirens underwater right", ItemProvider.Get(EInventoryUseItemType.SilverOre), UpperCavesOfBanishment & R.Swimming);
			Add(new ItemKey(8, 5, 88, 496), "Upper COB underwater hook", ItemProvider.Get(EItemType.MaxSand), UpperCavesOfBanishment & R.Swimming);
			areaName = "Castle Ramparts";
			Add(new ItemKey(4, 20, 264, 160), "Castle Ramparts moat cave", ItemProvider.Get(EItemType.MaxAura), AccessToPast);
			Add(new ItemKey(4, 1, 456, 160), "Castle Ramparts bombers", ItemProvider.Get(EItemType.MaxSand), CastleRamparts & MultipleSmallJumpsOfNpc);
			Add(new ItemKey(4, 3, 136, 144), "Castle Ramparts frozen engineer", ItemProvider.Get(EItemType.MaxHP), CastleRamparts & (R.TimeStop | R.ForwardDash));
			Add(new ItemKey(4, 10, 56, 192), "Castle Ramparts Giantess chest", ItemProvider.Get(EInventoryUseItemType.HiPotion), CastleRamparts);
			Add(new ItemKey(4, 11, 344, 192), "Castle Ramparts Knight and Archer chest", ItemProvider.Get(EInventoryUseItemType.HiPotion), CastleRamparts);
			Add(new ItemKey(4, 22, 104, 189), "Castle Ramparts pedestal", ItemProvider.Get(EInventoryOrbType.Iron, EOrbSlot.Melee), CastleRamparts);
			areaName = "Castle Keep";
			Add(new ItemKey(5, 9, 104, 189), "Castle Keep basement secret", ItemProvider.Get(EInventoryOrbType.Blood, EOrbSlot.Melee), CastleKeep & OculusRift);
			Add(new ItemKey(5, 10, 104, 192), "Castle Keep basement by secret", ItemProvider.Get(EInventoryFamiliarType.Sprite), CastleKeep);
			Add(new ItemKey(5, 14, 88, 208), "Aelana's room", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), CastleKeep & R.PinkOrb & R.DoubleJump);
			Add(new ItemKey(5, 44, 216, 192), "Castle Keep basement Giantess", ItemProvider.Get(EInventoryUseItemType.Potion), CastleKeep);
			Add(new ItemKey(5, 45, 104, 192), "Castle Keep basement Eggs and Arrows", ItemProvider.Get(EItemType.MaxHP), CastleKeep);
			Add(new ItemKey(5, 15, 296, 192), "Castle Keep basement solo egg", ItemProvider.Get(EItemType.MaxAura), CastleKeep);
			Add(new ItemKey(5, 41, 72, 160), "Chest under the Golden Idol", ItemProvider.Get(EInventoryEquipmentType.BuckleHat), CastleKeep);
			Add(new ItemKey(5, 20, 504, 48), "Castle Keep Royal Advisor ledge", null, CastleKeep & R.TimeStop);
			Add(new ItemKey(5, 22, 312, 176), "Castle Keep Royal Guard room", ItemProvider.Get(EItemType.MaxSand), CastleKeep & ((R.TimeStop & R.ForwardDash) | R.DoubleJump));
			Add(new RoomItemKey(5, 5), "Golden Idol", ItemProvider.Get(EInventoryRelicType.DoubleJump), CastleKeep & R.TimeStop);
			areaName = "Royal Towers";
			Add(new ItemKey(6, 19, 200, 176), "Royal Towers secret", ItemProvider.Get(EItemType.MaxAura), RoyalTower & R.DoubleJump & OculusRift);
			Add(new ItemKey(6, 27, 472, 384), "Royal Towers above the secret", ItemProvider.Get(EInventoryUseItemType.MagicMarbles), MidRoyalTower);
			Add(new ItemKey(6, 1, 1512, 288), "Royal Courtyard", ItemProvider.Get(EInventoryUseItemType.Potion), MidRoyalTower);
			Add(new ItemKey(6, 25, 360, 176), "Royal Courtyard Tower right chest", ItemProvider.Get(EInventoryUseItemType.HiEther), UpperRoyalTower & DoubleJumpOfNpc);
			Add(new ItemKey(6, 3, 120, 208), "Royal Courtyard Tower top", ItemProvider.Get(EInventoryFamiliarType.Demon), UpperRoyalTower & DoubleJumpOfNpc);
			Add(new ItemKey(6, 17, 200, 112), "Right Royal Tower pinnacle", ItemProvider.Get(EItemType.MaxHP), UpperRoyalTower & DoubleJumpOfNpc);
			Add(new ItemKey(6, 17, 56, 448), "Right Royal Tower chest below pinnacle", ItemProvider.Get(EInventoryEquipmentType.VileteCrown), UpperRoyalTower);
			Add(new ItemKey(6, 17, 360, 1840), "Right Royal Tower bottom", ItemProvider.Get(EInventoryEquipmentType.MidnightCloak), MidRoyalTower);
			Add(new ItemKey(6, 13, 120, 176), "Left Royal Tower balcony", ItemProvider.Get(EItemType.MaxSand), UpperRoyalTower);
			Add(new ItemKey(6, 22, 88, 208), "Left Royal Tower Royal Guard", ItemProvider.Get(EInventoryUseItemType.Ether), UpperRoyalTower);
			Add(new ItemKey(6, 11, 360, 544), "Before Aelana", ItemProvider.Get(EInventoryUseItemType.HiPotion), UpperRoyalTower);
			Add(new ItemKey(6, 23, 856, 208), "Aelana's attic", ItemProvider.Get(EInventoryEquipmentType.VileteDress), UpperRoyalTower & R.UpwardDash);
			Add(new ItemKey(6, 14, 136, 208), "Aelana's chest", ItemProvider.Get(EInventoryOrbType.Pink, EOrbSlot.Melee), UpperRoyalTower);
			Add(new ItemKey(6, 14, 184, 205), "Aelana's pedestal", ItemProvider.Get(EInventoryUseItemType.WarpCard), UpperRoyalTower);
		}

		void AddPyramidItemLocations()
		{
			areaName = "Ancient Pyramid";
			Add(new ItemKey(16, 14, 312, 192), "Pyramid entrance freebie", ItemProvider.Get(EItemType.MaxSand), LeftPyramid);
			Add(new ItemKey(16, 3, 88, 192), "Pyramid behind Conviction", ItemProvider.Get(EItemType.MaxHP), LeftPyramid);
			Add(new ItemKey(16, 22, 200, 192), "Pyramid secret", ItemProvider.Get(EItemType.MaxAura), LeftPyramid & OculusRift);
			Add(new ItemKey(16, 16, 1512, 144), "Pyramid secret secret", ItemProvider.Get(EInventoryRelicType.EssenceOfSpace), LeftPyramid & OculusRift);
			Add(new ItemKey(16, 5, 136, 192), "At Sandman's Door", ItemProvider.Get(EInventoryEquipmentType.SelenBangle), Nightmare);
		}

		void AddGyreItemLocations()
		{
			areaName = "Temporal Gyre";
			// Wheel is not strictly required, but is in logic for anti-frustration against Nethershades
			Add(new ItemKey(14, 14, 200, 832), "Gyre Chest 1", null, TemporalGyre);
			Add(new ItemKey(14, 17, 200, 832), "Gyre Chest 2", null, TemporalGyre);
			Add(new ItemKey(14, 20, 200, 832), "Gyre Chest 3", null, TemporalGyre);
			Add(new ItemKey(14, 8, 120, 176), "Ravenlord Entry", null, RavenlordsLair);
			Add(new ItemKey(14, 9, 200, 125), "Ravenlord Pedestal", null, RavenlordsLair);
			Add(new ItemKey(14, 9, 280, 176), "Ravenlord Exit", null, RavenlordsLair);
			// Ifrit is a strong early boss, access to the past is required as a safety check so that they do not block past access
			Add(new ItemKey(14, 6, 40, 208), "Ifrit Entry", null, IfritsLair);
			Add(new ItemKey(14, 7, 200, 205), "Ifrit Pedestal", null, IfritsLair);
			Add(new ItemKey(14, 7, 280, 208), "Ifrit Exit", null, IfritsLair);
		}

		void AddDownloadTerminals()
		{
			areaName = "Library";
			Add(new ItemKey(2, 44, 792, 592), "Library terminal 1", null, LeftLibrary & R.Tablet);
			Add(new ItemKey(2, 44, 120, 368), "Library terminal 2", null, LeftLibrary & R.Tablet);
			Add(new ItemKey(2, 44, 456, 368), "Library terminal 3", null, LeftLibrary & R.Tablet);
			Add(new ItemKey(2, 58, 152, 208), "V terminal 1", null, LeftLibrary & R.Tablet & R.CardV);
			Add(new ItemKey(2, 58, 232, 208), "V terminal 2", null, LeftLibrary & R.Tablet & R.CardV);
			Add(new ItemKey(2, 58, 312, 208), "V terminal 3", null, LeftLibrary & R.Tablet & R.CardV);
			areaName = "Library top";
			Add(new ItemKey(2, 44, 568, 176), "Terminal under Backer room", null, UpperLeftLibrary & R.Tablet);
			areaName = "Varndagroth Tower right";
			Add(new ItemKey(2, 18, 200, 192), "Varndagray terminal", null, RightSideLibraryElevator & R.CardB & R.Tablet);
			areaName = "The lab";
			Add(new ItemKey(11, 6, 200, 192), "Lab terminal and chest room terminal", null, UpperLab & R.Tablet);
			Add(new ItemKey(11, 16, 600, 192), "Sentry platform terminal", null, TheLabPoweredOff & R.Tablet);
			Add(new ItemKey(11, 34, 200, 192), "Lab terminal by Experiment 13", null, TheLab & R.Tablet);
			Add(new ItemKey(11, 37, 200, 192), "Lab Bottom terminal left", null, TheLab & R.Tablet);
			Add(new ItemKey(11, 15, 152, 176), "Lab Bottom terminal middle", null, TheLabPoweredOff & R.Tablet);
			Add(new ItemKey(11, 38, 120, 176), "Lab Bottom terminal right", null, TheLabPoweredOff & R.Tablet);
		}

		void AddLoreLocations()
		{
			// Memories
			areaName = "LakeDesolation";
			Add(new ItemKey(1, 10, 312, 81), "Lake Desolation memory (Time Messenger)", null, LowerLakeDesolationBridge);
			areaName = "Library";
			Add(new ItemKey(2, 5, 200, 145), "Library Waterway memory (A Message)", null, LeftLibrary);
			Add(new ItemKey(2, 45, 344, 145), "Library under Backer room memory (Lachiemi Sun)", null, UpperLeftLibrary);
			Add(new ItemKey(2, 51, 88, 177), "Library Backer room memory (Moonlit Night)", null, UpperLeftLibrary);
			areaName = "Varndagroth Tower Left";
			Add(new ItemKey(2, 25, 216, 145), "Left Varndagray Tower memory (Nomads)", null, MidLibrary & R.CardE);
			areaName = "Varndagroth Tower Right";
			Add(new ItemKey(2, 46, 200, 145), "Varndagray Sealed Caves elevator memory (Childhood)", null, MidLibrary & R.CardB);
			Add(new ItemKey(2, 11, 200, 161), "Right Varndagray bottom memory (Faron)", null, LowerRightSideLibrary);
			areaName = "Military Hangar";
			Add(new ItemKey(10, 3, 536, 97), "Military Hangar memory (A Solution)", null, MilitaryFortress & DoubleJumpOfNpc & R.TimespinnerWheel);
			areaName = "The Lab";
			Add(new ItemKey(11, 7, 248, 129), "Lab trash secret memory 1 (An Old Friend)", null, TheLab & OculusRift);
			Add(new ItemKey(11, 7, 296, 129), "Lab trash secret memory 2 (Twilight Dinner)", null, TheLab & OculusRift);
			areaName = "Emperor's Tower";
			Add(new ItemKey(12, 19, 56, 145), "Right Emperor's Tower top memory (Final Circle)", null, EmperorsTower & DoubleJumpOfNpc);
			// Letters
			areaName = "Forest";
			Add(new ItemKey(3, 12, 472, 161), "Forest three-way letter (Lachiem Expedition)", null, AccessToPast);
			Add(new ItemKey(3, 15, 328, 97), "Forest banishment overhang letter (Peace Treaty)", null, AccessToPast & (DoubleJumpOfNpc | ForwardDashDoubleJump | (R.TimeStop & R.ForwardDash)));
			areaName = "Castle Ramparts";
			Add(new ItemKey(4, 18, 456, 497), "Ramparts moat letter (Prime Edicts)", null, CastleRamparts);
			Add(new ItemKey(4, 11, 360, 161), "Ramparts Knight and Archer letter (Declaration of Independence)", null, CastleRamparts);
			areaName = "Castle Keep";
			Add(new ItemKey(5, 41, 184, 177), "Letter under the Golden Idol (Letter of Reference)", null, CastleKeep);
			Add(new ItemKey(5, 44, 264, 161), "Keep basement Giantess letter (Political Advice)", null, CastleKeep);
			Add(new ItemKey(5, 14, 568, 177), "Aelana's room letter (Diplomatic Missive)", null, CastleKeep & R.PinkOrb & R.DoubleJump);
			areaName = "Royal Towers";
			Add(new ItemKey(6, 17, 344, 433), "Right Royal Tower letter below pinnacle (War of the Sisters)", null, UpperRoyalTower);
			Add(new ItemKey(6, 14, 136, 177), "Letter beyond Aelana (Stained Letter)", null, UpperRoyalTower);
			Add(new ItemKey(6, 25, 152, 145), "Royal Courtyard letter (Mission Findings)", null, UpperRoyalTower & DoubleJumpOfNpc);
			areaName = "Caves of Banishment (Maw)";
			Add(new ItemKey(8, 36, 136, 145), "Caves of Banishment letter (Naïvety)", null, LowerCavesOfBanishment);
		}

		ItemLocation GetItemLocationBasedOnKeyOrRoomKey(ItemKey key)
		{
			return TryGetValue(key, out var itemLocation)
				? itemLocation
				: TryGetValue(key.ToRoomItemKey(), out var roomItemLocation)
					? roomItemLocation
					: null;
		}

		public virtual ProgressionChain GetProgressionChain()
		{
			var obtainedRequirements = R.None;
			IEnumerable<ItemLocation> alreadyKnownLocations = new ItemLocation[0];

			var progressionChain = new ProgressionChain();
			var currentProgressionChain = progressionChain;

			do
			{
				var previusObtainedRequirements = obtainedRequirements;

				var reachableProgressionItemLocations = GetReachableProgressionItemLocations(obtainedRequirements);
				obtainedRequirements = GetObtainedRequirements(reachableProgressionItemLocations);

				currentProgressionChain.Sub =
					new ProgressionChain { Locations = reachableProgressionItemLocations.Except(alreadyKnownLocations) };

				currentProgressionChain = currentProgressionChain.Sub;
				alreadyKnownLocations = reachableProgressionItemLocations;

				if (obtainedRequirements == previusObtainedRequirements)
					break;

			} while (true);

			return progressionChain.Sub;
		}

		ItemLocation[] GetReachableProgressionItemLocations(R obtainedRequirements)
		{
			return GetReachableLocations(obtainedRequirements)
				.Where(l => l.ItemInfo != null && l.ItemInfo.Unlocks != R.None)
				.ToArray();
		}

		public R GetAvailableRequirementsBasedOnObtainedItems()
		{
			var pickedUpProgressionItemLocations = this
				.Where(l => l.IsPickedUp && l.ItemInfo.Unlocks != R.None)
				.ToArray();

			var pickedUpSingleItemLocationUnlocks = pickedUpProgressionItemLocations
				.Where(l => !(l.ItemInfo is ProgressiveItemInfo))
				.Select(l => l.ItemInfo.Unlocks);

			var pickedUpProgressiveItemLocationUnlocks = pickedUpProgressionItemLocations
				.Where(l => l.ItemInfo is ProgressiveItemInfo)
				.Select(l => ((ProgressiveItemInfo)l.ItemInfo)
					.GetAllUnlockedItems()
					.Select(i => i.Unlocks)
					.Aggregate(R.None, (a, b) => a | b));

			return pickedUpSingleItemLocationUnlocks.Concat(pickedUpProgressiveItemLocationUnlocks)
				.Aggregate(R.None, (a, b) => a | b);
		}

		public virtual bool IsBeatable()
		{
			if ((!SeedOptions.GassMaw && !IsGassMaskReachableWithTheMawRequirements())
				|| ProgressiveItemsOfTheSameTypeAreInTheSameRoom())
				return false;

			var obtainedRequirements = R.None;

			do
			{
				var previusObtainedRequirements = obtainedRequirements;

				obtainedRequirements = GetObtainedRequirements(obtainedRequirements);

				if (obtainedRequirements == previusObtainedRequirements)
					return false;

			} while (!CanCompleteGame(obtainedRequirements));

			return true;
		}

		bool ProgressiveItemsOfTheSameTypeAreInTheSameRoom()
		{
			var progressiveItemLocationsPerType = this
				.Where(l => l.ItemInfo is ProgressiveItemInfo)
				.GroupBy(l => l.ItemInfo);

			return progressiveItemLocationsPerType.Any(
				progressiveItemLocationPerType => progressiveItemLocationPerType.Any(
					progressiveItemLocation => progressiveItemLocationPerType.Any(
						p => p.Key != progressiveItemLocation.Key && p.Key.ToRoomItemKey() == progressiveItemLocation.Key.ToRoomItemKey())));
		}

		bool IsGassMaskReachableWithTheMawRequirements()
		{
			//gassmask may never be placed in a gass effected place
			//the very basics to reach maw should also allow you to get gassmask
			//unless we run inverted, then we can garantee the user has the pyramid keys before entering lake desolation
			var gassmaskLocation = this.First(l => l.ItemInfo?.Identifier == new ItemIdentifier(EInventoryRelicType.AirMask));

			var levelIdsToAvoid = new List<int>(3) { 1 }; //lake desolation
			var mawRequirements = R.None;

			if (!SeedOptions.Inverted)
			{
				mawRequirements |= R.GateAccessToPast;

				//for non inverted seeds we dont know pyramid keys are required as it can be a classic past seed
				/*var isWatermaskRequiredForMaw = unlockingMap.PyramidKeysUnlock != R.GateMaw
				                                && unlockingMap.PyramidKeysUnlock != R.GateCavesOfBanishment;

				if (isWatermaskRequiredForMaw)
					mawRequirements |= R.Swimming;*/

				levelIdsToAvoid.Add(2); //library

				//if(unlockingMap.PyramidKeysUnlock != R.GateSealedCaves)
				levelIdsToAvoid.Add(9); //xarion skelleton
			}
			else
			{
				mawRequirements |= R.Swimming;
				mawRequirements |= UnlockingMap.PyramidKeysUnlock;
			}

			return !levelIdsToAvoid.Contains(gassmaskLocation.Key.LevelId) && gassmaskLocation.Gate.CanBeOpenedWith(mawRequirements);
		}

		R GetObtainedRequirements(R obtainedRequirements)
		{
			var reachableLocations = GetReachableLocations(obtainedRequirements)
				.Where(l => l.ItemInfo != null)
				.ToArray();

			var unlockedRequirements = reachableLocations
				.Where(l => !(l.ItemInfo is ProgressiveItemInfo))
				.Select(l => l.ItemInfo.Unlocks)
				.Aggregate(R.None, (current, unlock) => current | unlock);

			var progressiveItemsPerType = reachableLocations
				.Where(l => l.ItemInfo is ProgressiveItemInfo)
				.GroupBy(l => l.ItemInfo as ProgressiveItemInfo);

			foreach (var progressiveItemsType in progressiveItemsPerType)
			{
				var progressiveItem = progressiveItemsType.Key;
				var clone = progressiveItem.Clone();

				for (var i = 0; i < progressiveItemsType.Count(); i++)
				{
					unlockedRequirements |= clone.Unlocks;

					clone.Next();
				}
			}

			return unlockedRequirements;
		}

		static R GetObtainedRequirements(ItemLocation[] reachableLocations)
		{
			var unlockedRequirements = reachableLocations
				.Where(l => !(l.ItemInfo is ProgressiveItemInfo))
				.Select(l => l.ItemInfo.Unlocks)
				.Aggregate(R.None, (current, unlock) => current | unlock);

			var progressiveItemsPerType = reachableLocations
				.Where(l => l.ItemInfo is ProgressiveItemInfo)
				.GroupBy(l => l.ItemInfo as ProgressiveItemInfo);

			foreach (var progressiveItemsType in progressiveItemsPerType)
			{
				var progressiveItem = progressiveItemsType.Key;
				var clone = progressiveItem.Clone();

				for (var i = 0; i < progressiveItemsType.Count(); i++)
				{
					unlockedRequirements |= clone.Unlocks;

					clone.Next();
				}
			}

			return unlockedRequirements;
		}

		public IEnumerable<ItemLocation> GetReachableLocations(R obtainedRequirements)
			=> this.Where(l => l.Gate.CanBeOpenedWith(obtainedRequirements));

		bool CanCompleteGame(R obtainedRequirements)
			=> Nightmare.CanBeOpenedWith(obtainedRequirements);

		public virtual void Update(Level level)
		{
		}

		public virtual void Initialize(GameSave gameSave)
		{
			var progressiveItemInfos = this
				.Where(l => l.ItemInfo is ProgressiveItemInfo)
				.Select(l => (ProgressiveItemInfo)l.ItemInfo);

			foreach (var progressiveItem in progressiveItemInfos)
				progressiveItem.Reset();

			foreach (var itemLocation in this)
				itemLocation.BaseOnGameSave(gameSave);
		}

		protected void Add(ItemKey itemKey, string name, ItemInfo defaultItem)
			=> Add(new ItemLocation(itemKey, areaName, name, defaultItem));

		protected void Add(ItemKey itemKey, string name, ItemInfo defaultItem, Gate gate)
			=> Add(new ItemLocation(itemKey, areaName, name, defaultItem, gate));
	}

	class ProgressionChain
	{
		public IEnumerable<ItemLocation> Locations { get; set; }
		public ProgressionChain Sub { get; set; }
	}
}
