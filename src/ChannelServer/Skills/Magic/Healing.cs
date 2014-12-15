// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Util;
using Aura.Channel.World;
using Aura.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.Magic
{
	[Skill(SkillId.Healing)]
	public class HealingHandler : ISkillHandler, IPreparable, IReadyable, ICompletable, IUseable, ICancelable
	{
		public void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{

			Send.SkillHealingEffect(creature);

			if (creature.Conditions.Has(ConditionsD.InstanceCasting))
			{
				castTime = 0;
				//skill.Info.ManaCost = 0;
			}

			Send.SkillPrepare(creature, skill.Info.Id, castTime);
			creature.Skills.ActiveSkill = skill;
		}

		public void Ready(Creature creature, Skill skill, Packet packet)
		{
			SkillHelper.FillStack(creature, skill);

			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.StackUpdate).PutString("healing_stack").PutByte(creature.ActiveSkillStacks).PutByte(0));
			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.HoldMagic).PutString("healing"));

			if (creature.Conditions.Has(ConditionsD.InstanceCasting))
				creature.Conditions.Deactivate(ConditionsD.InstanceCasting);
			Send.SkillReady(creature, skill.Info.Id);
		}

		public void Complete(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillComplete(creature, skill.Info.Id);
			if (creature.ActiveSkillStacks > 0)
				Send.SkillReady(creature, skill.Info.Id);
		}

		public void Cancel(Creature creature, Skill skill)
		{
			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.StackUpdate).PutString("healing_stack").PutByte(0).PutByte(0));

		}

		public void Use(Creature creature, Skill skill, Packet packet)
		{
			var targetId = packet.GetLong();
			var target = creature.Region.GetCreature(targetId);
			if (target == null)
				return;

			if (creature != target && !creature.GetPosition().InRange(target.GetPosition(), 1000))
				return;

			// Reduce Stamina equal to healing amount if a player
			// is using the skill on himself.
			if (creature == target && creature is PlayerCreature)
			{
				var amount = Math.Min(skill.RankData.Var1, creature.LifeInjured - creature.Life);
				if (creature.Stamina < amount)
					return;

				creature.Stamina -= amount;
			}

			target.Life += skill.RankData.Var1;
			Send.StatUpdateDefault(target);

			SkillHelper.DecStack(creature, skill);

			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.UseMagic).PutString("healing").PutLong(target.EntityId));
			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.StackUpdate).PutString("healing_stack").PutByte(creature.ActiveSkillStacks).PutByte(0));

			Send.SkillUseHeal(creature, skill.Info.Id, targetId);

		}
	}
}
