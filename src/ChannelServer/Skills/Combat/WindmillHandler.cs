// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World;
using Aura.Channel.World.Entities;
using Aura.Data.Database;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aura.Channel.Skills;

namespace Aura.World.Skills
{
	[Skill(SkillId.Windmill)]
	public class WindmillHandler : CombatSkillHandler, IInitiableSkillHandler, IUseable
	{

		public void Init()
		{
			//ChannelServer.Instance.Events.CreatureAttackedByPlayer += this.OnCreatureAttackedByPlayer;
		}


		public override void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{
			Send.SkillFlashEffect(creature);
			creature.StopMove();
			Send.SkillPrepare(creature, skill.Info.Id, castTime);

			creature.Skills.ActiveSkill = skill;
		}

		public override void Ready(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillReady(creature, skill.Info.Id);
		}

		public override void Complete(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillComplete(creature, skill.Info.Id);
		}

		// Really odd that when we use CombatSkillResult use, it returns as unimplemented, but works fine with the IUseable use method... We need skill result methods for IUseable.

		public void Use(Creature attacker, Skill skill, Packet packet)
		{
			var targetEntityId = packet.GetLong();

			// Determine range, doesn't seem to be included in rank info.
			var range = this.GetRange(skill);

			if (skill.Info.Rank >= SkillRank.R5)
				range += 100;
			if (skill.Info.Rank >= SkillRank.R1)
				range += 100;

			// Add attack range from race, range must increase depending on "size".
			range += attacker.RaceData.AttackRange;

			var enemies = attacker.Region.GetAttackableCreaturesInRange(attacker, range).ToList();

			if (enemies.Count < 1)
			{
				Send.Notice(attacker, Localization.Get("Unable to use when there is no target.")); // Unable to use when there is no target.
				Send.SkillUseSilentCancel(attacker);
				return;
				//return CombatSkillResult.OutOfRange;
			}

			var rnd = RandomProvider.Get();

			attacker.StopMove();

			// Spin motion
			Send.UseMotion(attacker, 8, 4);

			// One source action, target actions are added for every target
			// and then we send the pack on its way.

			var aAction = new AttackerAction(CombatActionType.Hit, attacker, skill.Info.Id, targetEntityId);
			aAction.Set(AttackerOptions.Result | AttackerOptions.KnockBackHit1);

			attacker.Stun = aAction.Stun = 2500;

			foreach (var target in enemies)
			{
				target.StopMove();
				var targetPosition = target.GetPosition();

				var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
				tAction.Set(TargetOptions.Result | TargetOptions.KnockDown);

				var cap = new CombatActionPack(attacker, skill.Info.Id, aAction, tAction);

				var damage = attacker.GetRndTotalDamage();
				damage *= skill.RankData.Var1 / 100;

				var critChance = attacker.GetCritChanceFor(target);

				// Critical Hit
				SkillHelper.HandleCritical(attacker, critChance, ref damage, tAction);

				// Subtract target def/prot
				SkillHelper.HandleDefenseProtection(target, ref damage);

				// Mana Shield
				SkillHelper.HandleManaShield(target, ref damage, tAction);

				target.TakeDamage(tAction.Damage = damage, attacker);

				if (target.IsDead)
					tAction.Options |= TargetOptions.FinishingKnockDown;

				target.Stun = tAction.Stun = 3000; // need the actual number.. ._.;
				target.KnockBack = 375;

				Position intersection;
				var knockbackPos = attacker.GetPosition().GetRelative(targetPosition, 375);
				if (target.Region.Collisions.Find(targetPosition, knockbackPos, out intersection))
					knockbackPos = targetPosition.GetRelative(intersection, -50);

				// Set knockbacked position
				target.SetPosition(knockbackPos.X, knockbackPos.Y);

				tAction.Delay = (int)rnd.Next(300, 351);

				cap.Handle();
			}

			Send.SkillUseStun(attacker, skill.Info.Id, 2500, 1);

			//return CombatSkillResult.Okay;
		}

		protected virtual int GetRange(Skill skill)
		{
			int range = 300;
			if (skill.Info.Rank >= SkillRank.R5)
				range += 100;
			if (skill.Info.Rank >= SkillRank.R1)
				range += 100;
			return range;
		}
	}

		

	/// <summary>
	/// Training, called when someone attacks something.
	/// </summary>
	/// <param name="action"></param>
	//	public void OnCreatureAttackedByPlayer(TargetAction action)
	//	{

	// Only train if used skill was windmill.
	//       if (action.SkillId != SkillId.Windmill)
	//           return;

	// Get skill
	//       var attackerSkill = action.Attacker.Skills.Get(SkillId.MeleeCounterattack);
	//      if (attackerSkill == null) return; // Should be impossible.

	//Training to be done later.
	//     return;
	//	}

	/// <summary>
	/// The GM skill has 2 vars, 1000 and 1500, we'll asume it never cost HP,
	/// var 1 is still the damage, and var 2 is here the range.
	/// </summary>
	[Skill(SkillId.SuperWindmillGMSkill)]
	public class SuperWindmillHandler : WindmillHandler
	{
		protected override int GetRange(Skill skill)
		{
			return (int)skill.RankData.Var2;

		}

		/// <summary>
		/// Training, called when someone attacks something.
		/// </summary>
		/// <param name="action"></param>
		//public void OnCreatureAttackedByPlayer(TargetAction action)
		//	{
		//		return;
		//	}
	}
}
