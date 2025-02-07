﻿using Newtonsoft.Json;

namespace TsRandomizer.Settings.GameSettingObjects
{
	public abstract class GameSetting
	{
		public object CurrentValue { get; set; }

		[JsonIgnore]
		public string Name { get; }
		[JsonIgnore]
		public string Description { get; }
		[JsonIgnore]
		public object DefaultValue { get; }
		[JsonIgnore]
		public bool CanBeChangedInGame { get; }

		protected GameSetting(string name, string description, object defaultValue, bool canBeChangedInGame)
		{
			Name = name;
			Description = description;
			DefaultValue = defaultValue;
			CanBeChangedInGame = canBeChangedInGame;
			CurrentValue = DefaultValue;
		}

		// ReSharper disable once PublicConstructorInAbstractClass
		public GameSetting()
		{
		}

		public virtual void SetValue(object input) => CurrentValue = input;
	}

	public abstract class GameSetting<T> : GameSetting
	{
		[JsonIgnore]
		public T Value => (T)CurrentValue;

		protected GameSetting(string name, string description, T defaultValue, bool canBeChangedInGame)
			: base(name, description, defaultValue, canBeChangedInGame)
		{
		}

		// ReSharper disable once PublicConstructorInAbstractClass
		public GameSetting()
		{
		}

		public virtual void SetValue(T input) => base.SetValue(input);
	}
}
