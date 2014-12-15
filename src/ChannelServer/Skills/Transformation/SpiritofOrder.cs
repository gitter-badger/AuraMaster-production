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
	[Skill(SkillId.SpiritOfOrder)]
	public class SpiritOfOrderHandler : StartStopSkillHandler
	{

		public ushort Title;
		Timer warningTimer = null;
		Timer transTimer = null;

		private byte TransformId = 1;
		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{
			if (!creature.IsHuman)
			{
				Send.Notice(creature, "You can't use this skill.");
				return StartStopResult.Fail;
			}

			creature.Temp.PreviousTitle = creature.Titles.SelectedTitle;
			Send.Transform(creature, skill, true, TransformId);
			this.TitleSwitch(creature, skill);
			this.AddStatBonus(creature, skill);
			Send.StatUpdateDefault(creature);
			this.HandleTimer(creature, skill);
			creature.FullHeal();

			return StartStopResult.Okay;
		}

		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			Send.Transform(creature, skill, false, TransformId);
			warningTimer.Dispose();
			transTimer.Dispose();
			this.RemoveStatBonus(creature, skill);
			creature.Titles.ChangeTitle(creature.Temp.PreviousTitle, false);
			Send.StatUpdateDefault(creature);

			return StartStopResult.Okay;
		}

		public virtual void TitleSwitch(Creature creature, Skill skill)
		{

			if (skill.Info.Rank >= SkillRank.RA && skill.Info.Rank < SkillRank.R5)
			{
				Title = 40002;
			}

			else if (skill.Info.Rank >= SkillRank.R5 && skill.Info.Rank < SkillRank.R1)
			{
				Title = 40003;
			}
			else if (skill.Info.Rank >= SkillRank.R1)
				Title = 40004;
			else
				Title = 40001;

			creature.Titles.ChangeTitle(Title, false);
		}

		public virtual void AddStatBonus(Creature creature, Skill skill)
		{

			creature.StatMods.Add(Stat.LifeMaxMod, skill.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Add(Stat.ManaMaxMod, skill.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Add(Stat.StaminaMaxMod, skill.RankData.Var3, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Add(Stat.DefenseBaseMod, skill.RankData.Var4, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods
			creature.StatMods.Add(Stat.ProtectionBaseMod, skill.RankData.Var5, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods

			creature.StatMods.Add(Stat.MagicDefenseMod, skill.RankData.Var7, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods
			creature.StatMods.Add(Stat.MagicProtectMod, skill.RankData.Var9, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // likely same case as attack mods

			var Power = creature.Skills.Get(SkillId.PowerofOrder);
			var Eye = creature.Skills.Get(SkillId.EyeofOrder);
			var Sword = creature.Skills.Get(SkillId.SwordofOrder);

			if (creature.Skills.Has(SkillId.PowerofOrder, SkillRank.RF)) // Strength and Will
			{
				creature.StatMods.Add(Stat.StrMod, Power.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.WillMod, Power.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.EyeofOrder, SkillRank.RF)) // Dex and Balance
			{
				creature.StatMods.Add(Stat.DexMod, Eye.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.BalanceBaseMod, Eye.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // doesn't seem to work..
			}

			if (creature.Skills.Has(SkillId.SwordofOrder, SkillRank.RF)) // Damage and Injury
			{
				creature.StatMods.Add(Stat.AttackMaxMod, Sword.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window
				creature.StatMods.Add(Stat.AttackMinMod, Sword.RankData.Var1, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id); // seems to work, but doesn't register as changed in char window
				// Is Injury even partially implemented server side? It doesn't come up at all under stats.. Still needed.

			}
		}

		public virtual void RemoveStatBonus(Creature creature, Skill skill)
		{
			// Life, Mana, Stam, Def, Prot, Magic Def, and Magic Prot.

			creature.StatMods.Remove(Stat.LifeMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.ManaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.StaminaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Remove(Stat.DefenseBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.ProtectionBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			creature.StatMods.Remove(Stat.MagicDefenseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.MagicProtectMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);


			if (creature.Skills.Has(SkillId.PowerofOrder, SkillRank.RF)) // Strength and Will.
			{
				creature.StatMods.Remove(Stat.StrMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.WillMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.EyeofOrder, SkillRank.RF)) // Dex and Balance.
			{

				creature.StatMods.Remove(Stat.DexMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.BalanceBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.SwordofOrder, SkillRank.RF)) // Damage and Injury.
			{
				creature.StatMods.Remove(Stat.AttackMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.AttackMinMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				// Needs Injury.
			}

		}

		protected virtual void HandleTimer(Creature creature, Skill skill)
		{
			// Timer

			var character = (PlayerCreature)creature;
			var maxDuration = 360000; // 6 minutes; in milliseconds. 1000 ms = 1 second, 60 seconds to a minute. 60 * 6 = 360 * 1000 = 360000.
			var defaultDuration = 225000; // 3 minutes, 45 seconds. At age 10 with no rebirths.
			var AgeMod = (creature.Age - 10) * 2000;
			var Duration = (defaultDuration - AgeMod) + (character.RebirthCount * 7000);

			if (Duration >= maxDuration)
			{
				Duration = maxDuration;
			}


			warningTimer = new Timer(_ =>
			{
				Send.Notice(creature, Localization.Get("The transformation skill will end in 10 seconds."));
				GC.KeepAlive(warningTimer);
			}
			, null, Duration - 10000, Timeout.Infinite);


			transTimer = new Timer(_ =>
			{
				this.Stop(creature, skill);
				GC.KeepAlive(transTimer);
			}
			, null, Duration, Timeout.Infinite);

		}
	}



}