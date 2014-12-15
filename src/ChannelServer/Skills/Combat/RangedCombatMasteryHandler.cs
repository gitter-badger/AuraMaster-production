// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
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

namespace Aura.Channel.Skills.Combat
{
	[Skill(SkillId.RangedAttack)]
	public class RangeCombatMasteryHandler : CombatSkillHandler, IInitiableSkillHandler
	{
		/// To do:
		/// Fix Elven Ranged Attack (add second arrow and functioning running aim) 
		/// Add Training methods.
		/// Add aggro state..?
		/// Write up a proper movement event for aim reset.
		/// Shut up! I'm lazy..
		
		private const short StunTime = 2600;
		private const short AfterUseStun = 600;
		private const int KnockBackDistance = 450;

		public void Init()
		{
			//ChannelServer.Instance.Events.CreatureAttackedByPlayer += this.OnCreatureAttackedByPlayer;
		}

		public override void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{
			if (creature.IsElf)
				creature.IsWalking = false;

			Send.SkillFlashEffect(creature);
			Send.SkillPrepare(creature, skill.Info.Id, castTime);
			creature.Skills.ActiveSkill = skill;

		}

		public override void Ready(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillReady(creature, skill.Info.Id);
		}

		public override void Cancel(Creature creature, Skill skill)
		{
			if (creature.Target != null)
				Send.CombatSetAimR(creature, 0);

			Send.SkillUseStun(creature, skill.Info.Id, AfterUseStun, 1);

		}

		public override void Complete(Creature creature, Skill skill, Packet packet)
		{
			creature.Region.Broadcast(new Packet(Op.CombatSetAimR, creature.EntityId).PutByte(0));
			Send.SkillComplete(creature, skill.Info.Id);
		}

		public override CombatSkillResult Use(Creature attacker, Skill skill, long targetEntityId)
		{

			var target = attacker.Region.GetCreature(targetEntityId);
			if (target == null)
			{
				return CombatSkillResult.InvalidTarget;
			}

			if (attacker.Magazine == null || attacker.Magazine.Amount < 1)
			{
				return CombatSkillResult.None;
			}

			attacker.StopMove();

			// Prepare combat actions
			var aAction = new AttackerAction(CombatActionType.RangeHit, attacker, skill.Info.Id, targetEntityId);
			aAction.Set(AttackerOptions.Result);

			//	bool hit = false;
			var aimChance = attacker.GetAimPercent(skill.RankData.Var3);
			var Rnd = RandomProvider.Get();
			
			// Calculate damage
			var damage = attacker.GetRndRangeDamage();
			var critChance = attacker.GetCritChanceFor(target);

			// Send.Notice(attacker.Client, aimChance.ToString());

			if (Rnd.NextDouble() < Math.Min(aimChance / 100f, 1))
			{
				target.StopMove();

				var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
				tAction.Set(TargetOptions.Result);

				// Set Stun/Knockback
				attacker.Stun = aAction.Stun = AfterUseStun;
				target.Stun = tAction.Stun = StunTime;

				//	hit = true;

				//if (hit)
				
				//SkillHelper.CombatHelper.SetAggro(attacker, target);

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


				var weapon = attacker.Inventory.RightHand;

				if (!target.IsDead)
				{
					if (tAction.Type != CombatActionType.Defended)
					{
						target.KnockBack += this.GetKnockBack(weapon);
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
					var newPos = attacker.GetPosition().GetRelative(target.GetPosition(), KnockBackDistance);

					Position intersection;
					if (target.Region.Collisions.Find(target.GetPosition(), newPos, out intersection))
						newPos = target.GetPosition().GetRelative(intersection, -50);

					target.SetPosition(newPos.X, newPos.Y);

					aAction.Set(AttackerOptions.KnockBackHit2);
				}

				var cap = new CombatActionPack(attacker, skill.Info.Id, aAction, tAction);
				cap.Handle();
				attacker.Client.Send(new Packet(Op.CombatTargetUpdate, attacker.EntityId).PutLong(0));

			}

			else
			{
				var tAction = new TargetAction(CombatActionType.None, target, attacker, skill.Info.Id);
				var cap = new CombatActionPack(attacker, skill.Info.Id, aAction, tAction);
				cap.Handle();
			}

			Send.SkillUseStun(attacker, skill.Info.Id, AfterUseStun, 1);

			return CombatSkillResult.Okay;

		}

		/// <summary>
		/// Returns knock down increase for weapon.
		/// </summary>
		/// <remarks>
		/// http://wiki.mabinogiworld.com/view/Knock_down_gauge#Knockdown_Timer_Rates
		/// </remarks>
		/// <param name="weapon"></param>
		/// <returns></returns>
		public float GetKnockBack(Item weapon)
		{
			var count = weapon != null ? weapon.Info.KnockCount + 1 : 3;
			var speed = weapon != null ? (AttackSpeed)weapon.Data.AttackSpeed : AttackSpeed.Normal;

			switch (count)
			{
				default:
				case 1:
					return 100;
				case 2:
					switch (speed)
					{
						default:
						case AttackSpeed.VerySlow: return 70;
						case AttackSpeed.Slow: return 68;
						case AttackSpeed.Normal: return 68; // ?
						case AttackSpeed.Fast: return 68; // ?
					}
				case 3:
					switch (speed)
					{
						default:
						case AttackSpeed.VerySlow: return 60;
						case AttackSpeed.Slow: return 56; // ?
						case AttackSpeed.Normal: return 53;
						case AttackSpeed.Fast: return 50;
					}
				case 5:
					switch (speed)
					{
						default:
						case AttackSpeed.Fast: return 40; // ?
						case AttackSpeed.VeryFast: return 35; // ?
					}
			}
		}
	}
}