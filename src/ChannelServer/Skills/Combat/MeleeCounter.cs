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

namespace Aura.Channel.Skills.Combat
{
    [Skill(SkillId.MeleeCounterattack)]
    public class MeleeCounterHandler : CombatSkillHandler, IInitiableSkillHandler
	{
		private const int StunTime = 3000;
        private const int AfterUseStun = 1000;
        private const float Knockback = 120;
        private const int KnockbackDistance = 450;

           public void Init()
            {
                ChannelServer.Instance.Events.CreatureAttack += this.OnCreatureAttack;
            }

        public override void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{
            Send.SystemMessage(creature, "Prepare");
            Send.SkillFlashEffect(creature);
            Send.SkillPrepare(creature, skill.Info.Id, castTime);

            creature.Skills.ActiveSkill = skill;
		}

        public override void Ready(Creature creature, Skill skill, Packet packet)
        {
            Send.SystemMessage(creature, "Ready");
            Send.SkillReady(creature, skill.Info.Id);

            // Training
            if (skill.Info.Rank == SkillRank.RF)
                skill.Train(1); // Use the Defense skill.
        }

		public override void Complete(Creature creature, Skill skill, Packet packet)
		{
            Send.SystemMessage(creature, "Complete");
			Send.SkillComplete(creature, skill.Info.Id);
		}

        public override CombatSkillResult Use(Creature attacker, Skill skill, long targetEntityId)
		{
            var target = attacker.Region.GetCreature(targetEntityId);
			if (target == null)
				return CombatSkillResult.InvalidTarget;

             // Check range
			var targetPosition = target.GetPosition();
			if (!attacker.GetPosition().InRange(targetPosition, attacker.AttackRangeFor(target)))
				return CombatSkillResult.OutOfRange;

			// Stop movement
			attacker.StopMove();
			target.StopMove();

            // Prepare combat actions
            var aAction = new AttackerAction(CombatActionType.HardHit, attacker, skill.Info.Id, targetEntityId);
            aAction.Set(AttackerOptions.Result);

            var tAction = new TargetAction(CombatActionType.CounteredHit, target, attacker, skill.Info.Id);
            tAction.Set(TargetOptions.Result | TargetOptions.KnockDown);

            var cap = new CombatActionPack(attacker, skill.Info.Id, aAction, tAction);

            // Calculate damage
            var damage = this.GetDamage(attacker, skill);
            var critChance = this.GetCritChance(attacker, target, skill);

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
            attacker.Stun = aAction.Stun = StunTime;
            target.Stun = tAction.Stun = StunTime;
            target.KnockBack = Knockback;

            // Check collisions
            Position intersection;
            var knockbackPos = attacker.GetPosition().GetRelative(targetPosition, KnockbackDistance);
            if (target.Region.Collisions.Find(targetPosition, knockbackPos, out intersection))
                knockbackPos = targetPosition.GetRelative(intersection, -50);

            // Set knockbacked position
            target.SetPosition(knockbackPos.X, knockbackPos.Y);

            // Response
            Send.SkillUseStun(attacker, skill.Info.Id, AfterUseStun, 1);

            // Action!
            cap.Handle();

            return CombatSkillResult.Okay;
		}

        protected float GetDamage(Creature attacker, Skill skill)
        {
            var result = attacker.GetRndTotalDamage();
            result *= skill.RankData.Var1 / 100f;

            return result;
        }

        protected float GetCritChance(Creature attacker, Creature target, Skill skill)
        {
            var result = attacker.GetCritChanceFor(target);

            return result;
        }

        public void OnCreatureAttack(TargetAction action)
        {
            // Only train if used skill was Counter.
            if (action.SkillId != SkillId.MeleeCounterattack)
                return;

            // Get skill
            var attackerSkill = action.Attacker.Skills.Get(SkillId.MeleeCounterattack);
            if (attackerSkill == null) return; // Should be impossible.

            //Training to be done later.
            return;
            }
        }
	}
