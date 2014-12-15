using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World.Entities;
using Aura.Channel.World;
using Aura.Channel.Util;
using Aura.Shared.Mabi;
using Aura.Shared.Util;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using System.Threading;

using System;

namespace Aura.Channel.Skills.Transformation
{
	[Skill(SkillId.SoulOfChaos)]
	public class SoulOfChaosHandler : StartStopSkillHandler
	{
		public ushort Title;
		private byte TransformId = 2;
		Timer DetransTimer = null; // Length of disarm to detrans.
		Timer TransTimer = null; // Counts down till disarm starts.

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
			Send.TitleUpdate(creature);
			this.AddStatBonus(creature, skill);
			Send.StatUpdateDefault(creature);
			Send.EffectDelayed(61, 6500, creature);
			Send.Notice(creature, "The special skills of Dark Knight have been activated.", 0, 6500);
			creature.FullHeal();

			if (ChannelServer.Instance.Conf.World.DkSoundFix && StartStopResult.Okay == StartStopResult.Okay)
			{
				var t = new Thread(() =>
				{
					Send.PlaySound(creature, "data/sound/Glasgavelen_blowaway_endure.wav");
					Thread.Sleep(420);
					Send.PlaySound(creature, "data/sound/g1_darkmagic_0.wav");
					Thread.Sleep(2050);
					Send.PlaySound(creature, "data/sound/g1_scene_change.wav");
					Thread.Sleep(470);
					Send.PlaySound(creature, "data/sound/g1_scene_change.wav");
				});
				t.Start();
			}

			this.HandleTimer(creature, skill);


			return StartStopResult.Okay;
		}

		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			Send.Transform(creature, skill, false, TransformId);


			if (!creature.Conditions.Has(ConditionsA.CancelDarkKnight))
			{
				TransTimer.Dispose();
			}

			else
			{
				creature.Conditions.Deactivate(ConditionsA.CancelDarkKnight);
				DetransTimer.Dispose();
			}
			creature.Regens.Remove("Disarm");
			
			this.RemoveStatBonus(creature, skill);
			creature.Titles.ChangeTitle(creature.Temp.PreviousTitle, false);
			Send.StatUpdateDefault(creature);

			
			return StartStopResult.Okay;
		}

		public virtual void TitleSwitch(Creature creature, Skill skill)
		{

			if (skill.Info.Rank >= SkillRank.RA && skill.Info.Rank < SkillRank.R5)
			{
				Title = 41002;
			}

			else if (skill.Info.Rank >= SkillRank.R5 && skill.Info.Rank < SkillRank.R1)
			{
				Title = 41003;
			}

			else if (skill.Info.Rank >= SkillRank.R1)
				Title = 41004;

			else
				Title = 41001;

			creature.Titles.ChangeTitle(Title, false);
		}

		public virtual void AddStatBonus(Creature creature, Skill skill)
		{
			var rnd = RandomProvider.Get();
			var LifeMod = rnd.Next((int)skill.RankData.Var1, (int)skill.RankData.Var1 * 2 + 1);
			var StaminaMod = rnd.Next((int)skill.RankData.Var6, (int)skill.RankData.Var5 + 1);
			var ManaMod = rnd.Next((int)skill.RankData.Var2 / (int)2, (int)skill.RankData.Var2 * 2 + 1); // < wrong 

			// Life, Mana, and Stam.

			creature.StatMods.Add(Stat.LifeMaxMod, LifeMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Add(Stat.ManaMaxMod, ManaMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Add(Stat.StaminaMaxMod, StaminaMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			var Body = creature.Skills.Get(SkillId.BodyofChaos);
			var Mind = creature.Skills.Get(SkillId.MindofChaos);
			var Hands = creature.Skills.Get(SkillId.HandsofChaos);

			var StrRollOne = (int)Body.RankData.Var6; // 180%
			var StrRollTwo = (int)Body.RankData.Var1; // 100%
			var StrRollThree = (int)Body.RankData.Var5; // 60%

			var IntRollOne = (int)Mind.RankData.Var7; // 180%
			var IntRollTwo = (int)Mind.RankData.Var1; // 100%
			var IntRollThree = (int)Mind.RankData.Var6; // 60%

			var DexRollOne = (int)Hands.RankData.Var7; // 180%
			var DexRollTwo = (int)Hands.RankData.Var1; // 100%
			var DexRollThree = (int)Hands.RankData.Var6; // 60%

			var CritRollOne = (int)Mind.RankData.Var9; // 180%
			var CritRollTwo = (int)Mind.RankData.Var8; // 100%
			var CritRollThree = (int)Mind.RankData.Var5; // 60%

			var StrMod = 0;
			var IntMod = 0;
			var dexMod = 0;
			var CritMod = 0;

			var DiceRollOne = rnd.Next(100);
			var DiceRollTwo = rnd.Next(1, 50 + 1);
			var DiceRollThree = rnd.NextDouble();

			if (DiceRollOne > 66) // 180%
			{
				StrMod = (int)StrRollOne;

				if (DiceRollTwo <= 25)
				{
					IntMod = IntRollThree; // 60%
					dexMod = DexRollTwo; // 100%
				}

				else
				{
					IntMod = IntRollTwo; // 100%
					dexMod = DexRollThree; // 60%
				}
			}


			else if (DiceRollOne <= 66 & DiceRollOne > 33) // 100%
			{
				StrMod = (int)StrRollTwo;

				if (DiceRollTwo <= 25)
				{
					IntMod = IntRollOne; // 180%
					dexMod = DexRollThree; // 60%
				}

				else
				{
					IntMod = IntRollThree; // 60%
					dexMod = DexRollOne; // 180%
				}
			}

			else if (DiceRollOne <= 33) // 60%
			{
				StrMod = (int)StrRollThree;

				if (DiceRollTwo > 25) //
				{
					IntMod = IntRollOne; // 180%
					dexMod = DexRollTwo; // 100%
				}

				else
				{
					IntMod = IntRollTwo; // 100%
					dexMod = DexRollOne; // 180%
				}
			}


			if (DiceRollThree > 0.66) // 180%
			{
				CritMod = CritRollOne;
			}

			else if (DiceRollThree <= 0.66 && DiceRollThree > 0.33) // 100%
			{
				CritMod = CritRollTwo;
			}

			else if (DiceRollThree <= 0.33)// 60%
			{
				CritMod = CritRollThree;
			}

			if (creature.Skills.Has(SkillId.BodyofChaos, SkillRank.RF)) // Str and Balance.
			{
				creature.StatMods.Add(Stat.StrMod, StrMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.BalanceBaseMod, Body.RankData.Var2, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			}

			if (creature.Skills.Has(SkillId.MindofChaos, SkillRank.RF)) // Int and Crit.
			{
				creature.StatMods.Add(Stat.IntMod, IntMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Add(Stat.CriticalBase, CritMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			}

			if (creature.Skills.Has(SkillId.HandsofChaos, SkillRank.RF)) // Dex and Injury.
			{
				creature.StatMods.Add(Stat.DexMod, dexMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);


			}

		}

		public virtual void RemoveStatBonus(Creature creature, Skill skill)
		{

			creature.StatMods.Remove(Stat.LifeMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.ManaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
			creature.StatMods.Remove(Stat.StaminaMaxMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			if (creature.Skills.Has(SkillId.BodyofChaos, SkillRank.RF))
			{
				creature.StatMods.Remove(Stat.StrMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.BalanceBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			}
			if (creature.Skills.Has(SkillId.MindofChaos, SkillRank.RF))
			{
				creature.StatMods.Remove(Stat.IntMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				creature.StatMods.Remove(Stat.CriticalBaseMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);

			}
			if (creature.Skills.Has(SkillId.HandsofChaos, SkillRank.RF))
			{
				creature.StatMods.Remove(Stat.DexMod, World.Entities.Creatures.StatModSource.Skill, skill.Info.Id);
				// Not sure on injury stat..
			}
		}

		protected virtual void HandleDisarm(Creature creature, Skill skill)
		{
			creature.Conditions.Activate(ConditionsA.CancelDarkKnight);
			creature.Regens.Add("Disarm", Stat.Life, -10, creature.LifeMax);
			Send.Notice(creature, Localization.Get("The power of darkness has begun to press upon my body. The armor is draining me of my life!"));


			DetransTimer = new Timer(_ =>
			{
				this.Stop(creature, skill);
				GC.KeepAlive(DetransTimer);
			}
			, null, (long)creature.Life * 100 + 1800, Timeout.Infinite); // need a more accurate number...prolly works, though

			// Seeing as 1000 milliseconds are a second, and you lose 10 hp a second, it seemed appropriate to make the duration your life x 100. Say you have 1300 hp; it'd take 130 seconds to hit zero hp. 1300 x 100 is 1300000 ms or 130 seconds.
			// So I don't see how my math is off. Technically, you detrans at deadly state. So it should be 1/10th of a second more to hit -1 or deadly, or 100 milliseconds. Yet I keep coming up short, even with over a second more on the timer.
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

			TransTimer = new Timer(_ =>
			{
				this.HandleDisarm(creature, skill);
				GC.KeepAlive(TransTimer);
			}
			, null, Duration, Timeout.Infinite);
		}
	}
}
