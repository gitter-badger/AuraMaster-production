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
	[Skill(SkillId.DemonOfPhysis)]
	public class DemonOfPhysisHandler : StartStopSkillHandler
	{
		public ushort Title;
		private byte TransformId = 5;
		Timer WarningTimer = null;
		Timer TransTimer = null;

		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{
			if (!creature.IsGiant)
			{
				Send.Notice(creature, "You can't use this skill.");
				return StartStopResult.Fail;
			}

			else
			{
				creature.Temp.PreviousTitle = creature.Titles.SelectedTitle;
				Send.Transform(creature, skill, true, TransformId);
				this.TitleSwitch(creature, skill);

				this.AddStatBonus(creature, skill);

				Send.StatUpdateDefault(creature);
				creature.FullHeal();
				this.HandleTimer(creature, skill);
				return StartStopResult.Okay;
			}
		}

		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			WarningTimer.Dispose();
			TransTimer.Dispose();
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
				Title = 43002;
			}

			else if (skill.Info.Rank >= SkillRank.R5 && skill.Info.Rank < SkillRank.R1)
			{
				Title = 43003;
			}
			else if (skill.Info.Rank >= SkillRank.R1)
				Title = 43004;
			else
				Title = 43001;

			creature.Titles.ChangeTitle(Title, false);
		}

		public virtual void AddStatBonus(Creature creature, Skill skill)
		{
			var Shield = creature.Skills.Get(SkillId.ShieldofPhysis);
			var Life = creature.Skills.Get(SkillId.LifeofPhysis);
			var Spell = creature.Skills.Get(SkillId.SpellofPhysis);

			// Dex, Balance, and Attack.

			creature.StatMods.Add(Stat.DexMod, skill.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Add(Stat.BalanceBaseMod, skill.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // doesn't seem to work..

			creature.StatMods.Add(Stat.AttackMaxMod, skill.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window
			creature.StatMods.Add(Stat.AttackMinMod, skill.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window

			if (creature.Skills.Has(SkillId.ShieldofPhysis, SkillRank.RF)) // Life and Protection.
			{
				creature.StatMods.Add(Stat.LifeMaxMod, Shield.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.ProtectionBaseMod, Shield.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods
			}

			if (creature.Skills.Has(SkillId.SpellofPhysis, SkillRank.RF)) // Mana, Int, Critical.
			{
				creature.StatMods.Add(Stat.ManaMaxMod, Spell.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.IntMod, Spell.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.CriticalBase, Spell.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.LifeofPhysis, SkillRank.RF)) // Str, Will, Stamina.
			{
				creature.StatMods.Add(Stat.StrMod, Life.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.WillMod, Life.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.StaminaMaxMod, Life.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}
		}

		public virtual void RemoveStatBonus(Creature creature, Skill skill)
		{
			// Dex, Balance, Damage.

			creature.StatMods.Remove(Stat.DexMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.BalanceBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Remove(Stat.AttackMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.AttackMinMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			if (creature.Skills.Has(SkillId.ShieldofPhysis, SkillRank.RF)) // Life and Protection
			{
				creature.StatMods.Remove(Stat.LifeMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.ProtectionBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.SpellofPhysis, SkillRank.RF)) // Mana, Int, Critical
			{
				creature.StatMods.Remove(Stat.ManaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.CriticalBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.IntMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.LifeofPhysis, SkillRank.RF)) // Str, Will, Stamina
			{
				creature.StatMods.Remove(Stat.StrMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.WillMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.StaminaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
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
