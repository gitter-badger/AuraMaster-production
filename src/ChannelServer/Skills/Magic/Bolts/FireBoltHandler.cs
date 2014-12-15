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

namespace Aura.Channel.Skills.Magic.Bolts
{
	[Skill(SkillId.Firebolt)]
	public class FireBoltHandler : CombatSkillHandler, IInitiableSkillHandler
	{
		public const short UseStun = 500;
		public const short TargetStun = 3000; 

		public void Init()
		{
			//ChannelServer.Instance.Events.CreatureAttackedByPlayer += this.OnCreatureAttackedByPlayer;
		}

		public override void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{
			creature.StopMove();

			// Casting motion
			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.Casting).PutShort((short)skill.Info.Id).PutByte(0).PutByte(1).PutShort(0));

			if (creature.Conditions.Has(ConditionsD.InstanceCasting))
			{
				castTime = 0;
			}

			Send.SkillPrepare(creature, skill.Info.Id, castTime);
			creature.Skills.ActiveSkill = skill;
		}

		public override void Ready(Creature creature, Skill skill, Packet packet)
		{
			if (creature.ActiveSkillStacks < skill.RankData.StackMax)
			{
				// Stack
				if ((creature.Skills.Has(SkillId.ChainCasting)) || (creature.Conditions.Has(ConditionsD.InstanceCasting)))
					SkillHelper.FillStack(creature, skill);
				else
					SkillHelper.IncStack(creature, skill);

				creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.StackUpdate).PutString("firebolt").PutByte(creature.ActiveSkillStacks).PutByte(0));

				// Casting motion stop
				creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.Casting).PutShort((short)skill.Info.Id).PutByte(0).PutByte(2).PutShort(0));
			}

			if (creature.Conditions.Has(ConditionsD.InstanceCasting))
				creature.Conditions.Deactivate(ConditionsD.InstanceCasting);
			Send.SkillReady(creature, skill.Info.Id);
		}

		public override void Complete(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillComplete(creature, skill.Info.Id);

		}

		public override void Cancel(Creature creature, Skill skill)
		{
			SkillHelper.ClearStack(creature, skill);
			creature.Region.Broadcast(new Packet(Op.Effect, creature.EntityId).PutInt(Effect.StackUpdate).PutString("firebolt").PutByte(creature.ActiveSkillStacks).PutByte(0));

			creature.Region.Broadcast(new Packet(Op.MotionCancel2, creature.EntityId).PutByte(1));
		}

		public override CombatSkillResult Use(Creature attacker, Skill skill, long targetEntityId)
		{
			targetEntityId = attacker.Target.EntityId;
			var target = attacker.Region.GetCreature(targetEntityId);
			if (target == null)
				return CombatSkillResult.InvalidTarget;

			var targetPosition = target.GetPosition();
			if (!attacker.GetPosition().InRange(targetPosition, 1200))
				return CombatSkillResult.OutOfRange;

			attacker.StopMove();
			target.StopMove();

			attacker.Region.Broadcast(new Packet(Op.Effect, attacker.EntityId).PutInt(Effect.UseMagic).PutString("firebolt"));

			// Prepare combat actions
			var aAction = new AttackerAction(CombatActionType.HardHit, attacker, skill.Info.Id, targetEntityId);
			aAction.Set(AttackerOptions.Result | AttackerOptions.KnockBackHit2);

			var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
			tAction.Set(TargetOptions.Result | TargetOptions.Smash);

			var cap = new CombatActionPack(attacker, skill.Info.Id, aAction, tAction);

			var rnd = RandomProvider.Get();
			// Calculate damage
			var damage = attacker.GetMagicDamage(attacker.RightHand, rnd.Next((int)skill.RankData.Var1, (int)skill.RankData.Var2 + 1));
			damage *= (attacker.ActiveSkillStacks == 5 ? 6.5f : attacker.ActiveSkillStacks);

			var critChance = attacker.GetCritChanceFor(target);

			// Critical Hit
			SkillHelper.HandleCritical(attacker, critChance, ref damage, tAction);

			// Subtract target def/prot
			SkillHelper.HandleDefenseProtection(target, ref damage);

			// Mana Shield
			SkillHelper.HandleManaShield(target, ref damage, tAction);

			// Apply damage
			target.TakeDamage(tAction.Damage = damage, attacker);

			if (target.IsDead)
				tAction.Set(TargetOptions.FinishingHit | TargetOptions.Finished);

			// Set Stun/Knockback
			attacker.Stun = aAction.Stun = UseStun;
			target.Stun = tAction.Stun = TargetStun;

			if (!target.IsDead)
			{
				if (tAction.Type != CombatActionType.Defended)
				{
					target.KnockBack += 100;
					if (target.KnockBack >= 100 && target.Is(RaceStands.KnockBackable))
						tAction.Set(tAction.Has(TargetOptions.Critical) ? TargetOptions.KnockDown : TargetOptions.KnockBack);
				}
			}
			else
			{
				tAction.Set(TargetOptions.FinishingKnockDown);
			}

			// React to knock back
			if (tAction.IsKnockBack)
			{
				var newPos = attacker.GetPosition().GetRelative(target.GetPosition(), 450);

				Position intersection;
				if (target.Region.Collisions.Find(target.GetPosition(), newPos, out intersection))
					newPos = target.GetPosition().GetRelative(intersection, -50);

				target.SetPosition(newPos.X, newPos.Y);
			}


			Send.SkillUseStun(attacker, skill.Info.Id, UseStun, 1);

			SkillHelper.ClearStack(attacker, skill);

			// Action!
			cap.Handle();

			return CombatSkillResult.Okay;
		}

		/// <summary>
		/// Called when the skill user is attacked by another creature.
		/// </summary>
		/// <param name="tAction"></param>
		//public void OnCreatureAttack(TargetAction tAction)
		//{

		//	var targetSkill = tAction.Creature.Skills.Get(SkillId.Firebolt);

		//	if (targetSkill != null)
		//	{
		//		if (tAction.Creature.KnockBack >= 100)
		//		{
		//			switch (targetSkill.Info.Rank)
		//			{
		//				case SkillRank.R1:
		//					break;
		//					// Nothing happens. In other words, you don't lose any stacks.

		//				default:
		//					SkillHelper.DecStack(tAction.Creature, targetSkill);
		//					if (tAction.Creature.ActiveSkillStacks < 1)
		//						tAction.Creature.Skills.ActiveSkill = null;

		//					break;
		//			}
		//		}
		//	}
		//}
	}
}
