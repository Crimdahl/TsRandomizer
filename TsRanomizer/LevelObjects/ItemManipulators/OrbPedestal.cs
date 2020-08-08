﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Timespinner.Core;
using Timespinner.Core.Specifications;
using Timespinner.GameAbstractions;
using Timespinner.GameAbstractions.GameObjects;
using Timespinner.GameAbstractions.Gameplay;
using Timespinner.GameAbstractions.Inventory;
using Timespinner.GameObjects.BaseClasses;
using TsRandomizer.Extensions;
using TsRandomizer.IntermediateObjects;
using TsRandomizer.Randomisation;
using TsRandomizer.Screens;

namespace TsRandomizer.LevelObjects.ItemManipulators
{
	[TimeSpinnerType("Timespinner.GameObjects.Events.Treasure.OrbPedestalEvent")]
	[AlwaysSpawn(EEventTileType.OrbPedestal, ignoreArgument: true)]
	// ReSharper disable once UnusedMember.Global
	class OrbPedestal : ItemManipulator<Mobile>, ICustomSpwanMethod
	{
		static readonly MethodInfo GetOrbGlowColorByTypeMethod = TimeSpinnerType
			.Get("Timespinner.GameObjects.Heroes.Orbs.LunaisOrb")
			.GetPrivateStaticMethod("GetOrbGlowColorByType", typeof(EInventoryOrbType));

		readonly SpriteSheet menuIcons;
		
		List<Appendage> Appendages => Object._appendages;
		int appendagesCount;
		bool hasDroppedLoot;

		public OrbPedestal(Mobile typedObject, ItemLocation itemInfo) : base(typedObject, itemInfo)
		{
			if(Level != null)
				menuIcons = Level.GCM.SpMenuIcons;
		}
		
		protected override void Initialize()
		{
			if (ItemInfo == null)
				return;

			if (IsPickedUp)
			{
				Level.RequestRemoveObject(TypedObject);
				return;
			}

			UpdateContainedLootSprite();
			
			appendagesCount = Appendages.Count;
		}

		void UpdateContainedLootSprite()
		{
			if (ItemInfo.LootType == LootType.Orb)
			{
				Object._orbType = ItemInfo.OrbType;
				UpdateOrbGlowColor();

				if (ItemInfo.OrbSlot == EOrbSlot.Melee)
					UpdateMeleeOrbSprite();
				else
					UpdateSprite();
			}
			else
			{
				UpdateSprite();
				UpdateGlowColor();
			}
		}

		void SpawnItemInMiddleOfRoom()
		{
			var itemDropPickupType = TimeSpinnerType.Get("Timespinner.GameObjects.Items.ItemDropPickup");
			var itemPosition = new Point(266, 208);
			var itemDropPickup = Activator.CreateInstance(itemDropPickupType, ItemInfo.BestiaryItemDropSpecification, Level, itemPosition, -1);

			var levelReflected = Level.AsDynamic();
			levelReflected.RequestAddObject((Item)itemDropPickup);
		}

		protected override void OnUpdate(GameplayScreen gameplayScreen)
		{
			if (ItemInfo == null || hasDroppedLoot)
				return;

			if (Appendages.Count != appendagesCount)
			{
				appendagesCount = Appendages.Count;
				UpdateSprite();
			}

			if (!Object.HasBeenPickedUp) 
				return;

			if (Scripts.Count == 0) //it didnt spawn since you already own contained orb
			{
				SpawnItemInMiddleOfRoom();
				hasDroppedLoot = true;
				return;
			}

			Scripts.UpdateRelicOrbGetToastToItem(Level, ItemInfo);

			AwardContainedItem();
			hasDroppedLoot = true;
		}

		void UpdateMeleeOrbSprite()
		{
			const int wierdOffset = 19;
			if (Appendages.Count == 0)
				((Animate)~Object).ChangeAnimation((int)ItemInfo.OrbType + wierdOffset);
			else
				((Appendage)Object._orbAppendage).ChangeAnimation((int)ItemInfo.OrbType + wierdOffset);
		}

		void UpdateSprite()
		{
			if (Appendages.Count == 0)
			{
				Object._sprite = menuIcons;
				((Animate)~Object).ChangeAnimation(ItemInfo.AnimationIndex);
			}
			else
			{
				Appendage orbAppendage = (Appendage)Object._orbAppendage;
				orbAppendage.AnchorOffset = new Point(-4, -36); //TODO fix glow position
				((Animate)Object._orbAppendage).AsDynamic()._sprite = menuIcons;
				((Animate)Object._orbAppendage).ChangeAnimation(ItemInfo.AnimationIndex);
			}
		}

		void UpdateOrbGlowColor()
		{
			var orbGlowColorVector = (Vector4)GetOrbGlowColorByTypeMethod.InvokeStatic(ItemInfo.OrbType);
			var orbGlowColor = new Color(orbGlowColorVector);
			Object._baseOrbGlowColorAsVector = orbGlowColorVector;
			Object._baseOrbGlowColorAsColor = orbGlowColor;
			((object)Object._glowCircle).AsDynamic().BaseColor = orbGlowColor;
			((OrbPedestalLeakParticleSystem)Object._pixelLeakParticleSystem).BaseColor = orbGlowColorVector;
		}

		void UpdateGlowColor()
		{
			var orbGlowColorVector = new Vector4(0.75f);
			var orbGlowColor = new Color(orbGlowColorVector);
			Object._baseOrbGlowColorAsVector = orbGlowColorVector;
			Object._baseOrbGlowColorAsColor = orbGlowColor;
			((object)Object._glowCircle).AsDynamic().BaseColor = orbGlowColor;
			((OrbPedestalLeakParticleSystem)Object._pixelLeakParticleSystem).BaseColor = orbGlowColorVector;
		}

		public Mobile Spawn(Level level, ObjectTileSpecification specification)
		{
			var timeSpinnerType = TimeSpinnerType.Get("Timespinner.GameObjects.Events.Treasure.OrbPedestalEvent");
			var point = new Point(specification.X * 16 + 8, specification.Y * 16 + 16);
			var gameEvent = (GameEvent)Activator.CreateInstance(timeSpinnerType, level, point, -1, specification);

			gameEvent.AsDynamic().DoesSpawnDespiteBeingOwned = true;

			return gameEvent;
		}
	}
}
