using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World.Entities;
using Aura.Channel.World;
using Aura.Shared.Mabi;
using Aura.Shared.Util;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Channel.Util;
using System.Threading;
using System;

namespace Aura.Channel.Skills.Transformation
{
	[Skill(SkillId.FuryOfConnous)]
	public class FuryOfConnousHandler : StartStopSkillHandler
	{
		public ushort Title;
		private byte TransformId = 4;
		public ushort prevTitle;
		Timer WarningTimer = null;
		Timer TransTimer = null;

		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{

			if (!creature.IsElf)
			{
				Send.Notice(creature, "You can't use this skill.");
				return StartStopResult.Fail;
			}

			creature.Temp.PreviousTitle = creature.Titles.SelectedTitle;
			Send.Transform(creature, skill, true, TransformId);
			this.TitleSwitch(creature, skill);
			this.AddStatBonus(creature, skill);
			Send.StatUpdateDefault(creature);

			creature.FullHeal();

			this.HandleTimer(creature, skill);
			return StartStopResult.Okay;
		}

		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			if (WarningTimer != null)
			{
				WarningTimer.Dispose();
			}
			if (TransTimer != null)
			{
				TransTimer.Dispose();
			}

			Send.Transform(creature, skill, false, TransformId);
			this.RemoveStatBonus(creature, skill);
			creature.Titles.ChangeTitle(creature.Temp.PreviousTitle, false);
			Send.StatUpdateDefault(creature);

			return StartStopResult.Okay;
		}

		public virtual void TitleSwitch(Creature creature, Skill skill)
		{

			if (skill.Info.Rank >= SkillRank.RA && skill.Info.Rank < SkillRank.R5)
			{
				Title = 42002;
			}

			else if (skill.Info.Rank >= SkillRank.R5 && skill.Info.Rank < SkillRank.R1)
			{
				Title = 42003;
			}
			else if (skill.Info.Rank >= SkillRank.R1)
				Title = 42004;
			else
				Title = 42001;

			creature.Titles.ChangeTitle(Title, false);
		}

		public virtual void AddStatBonus(Creature creature, Skill skill)
		{
			var Armor = creature.Skills.Get(SkillId.ArmorofConnous);
			var Mind = creature.Skills.Get(SkillId.MindofConnous);
			var Sharpness = creature.Skills.Get(SkillId.SharpnessofConnous);

			// Life, Stam, Str, and Damage.

			creature.StatMods.Add(Stat.LifeMaxMod, skill.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Add(Stat.StaminaMaxMod, skill.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Add(Stat.StrMod, skill.RankData.Var7, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Add(Stat.AttackMaxMod, skill.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window
			creature.StatMods.Add(Stat.AttackMinMod, skill.RankData.Var4, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window

			if (creature.Skills.Has(SkillId.ArmorofConnous, SkillRank.RF)) // Will and Protection.
			{
				creature.StatMods.Add(Stat.WillMod, Armor.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.ProtectionBaseMod, Armor.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods
			}

			if (creature.Skills.Has(SkillId.MindofConnous, SkillRank.RF)) // Mana and Int.
			{
				creature.StatMods.Add(Stat.ManaMaxMod, Mind.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.IntMod, Mind.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			}

			if (creature.Skills.Has(SkillId.SharpnessofConnous, SkillRank.RF)) // Dex, Crit, Balance.
			{
				creature.StatMods.Add(Stat.DexMod, Sharpness.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.CriticalBase, Sharpness.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.BalanceBaseMod, Sharpness.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // doesn't seem to work..	

			}
		}

		public virtual void RemoveStatBonus(Creature creature, Skill skill)
		{
			// Life, Stam, Str, and Damage.

			creature.StatMods.Remove(Stat.LifeMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.StaminaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Remove(Stat.StrMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Remove(Stat.AttackMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window
			creature.StatMods.Remove(Stat.AttackMinMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window

			if (creature.Skills.Has(SkillId.ArmorofConnous, SkillRank.RF)) // Will and Protection.
			{
				creature.StatMods.Remove(Stat.WillMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.ProtectionBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods
			}

			if (creature.Skills.Has(SkillId.MindofConnous, SkillRank.RF)) // Mana and Int.
			{
				creature.StatMods.Remove(Stat.ManaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.IntMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			}

			if (creature.Skills.Has(SkillId.SharpnessofConnous, SkillRank.RF)) // Dex, Crit, Balance.
			{
				creature.StatMods.Remove(Stat.DexMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.CriticalBase, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.BalanceBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}
		}

		protected virtual void HandleTimer(Creature creature, Skill skill)
		{
			// Timer

			var Character = (PlayerCreature)creature;
			var MaxDuration = 360000; // 6 minutes; in milliseconds. 1000 ms = 1 second, 60 seconds to a minute. 60 * 6 = 360 * 1000 = 360000.
			var DefaultDuration = 225000; // 3 minutes, 45 seconds. At age 10 with no rebirths.
			var AgeMod = (creature.Age - 10) * 2000;
			var Duration = (DefaultDuration - AgeMod) + (Character.RebirthCount * 7000);

			if (Duration >= MaxDuration)
			{
				Duration = MaxDuration;
			}


			WarningTimer = new Timer(_ =>
			{
				Send.Notice(creature, Localization.Get("The transformation skill will end in 10 seconds."));
				GC.KeepAlive(WarningTimer);
			}
			, null, Duration - 10000, Timeout.Infinite);


			TransTimer = new Timer(_ =>
			{
				this.Stop(creature, skill);
				GC.KeepAlive(TransTimer);
			}
			, null, Duration, Timeout.Infinite);

		}
	}
}
