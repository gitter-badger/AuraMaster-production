// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aura.Channel.World;
using System.Reflection;

namespace Aura.Channel.Skills
{
	public static class SkillHelper
	{
		private const int DefenseAttackerStun = 2500;
		private const int DefenseTargetStun = 1000;
        private const int CounterAttackerStun = 3000;
        private const int CounterTargetStun = 1000;

        /// <summary>
		/// Checks if target's Mana Shield is active, calculates mana
		/// damage, and sets target action's Mana Damage property if applicable.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="damage"></param>
		/// <param name="tAction"></param>
		public static void HandleManaShield(Creature target, ref float damage, TargetAction tAction)
		{
			// Mana Shield active?
			if (!target.Conditions.Has(ConditionsA.ManaShield))
				return;

			// Get Mana Shield skill to get the rank's vars
			var manaShield = target.Skills.Get(SkillId.ManaShield);
			if (manaShield == null) // Checks for things that should never ever happen, yay.
				return;

			// Var 1 = Efficiency
			var manaDamage = damage / manaShield.RankData.Var1;
			if (target.Mana >= manaDamage)
			{
				// Damage is 0 if target's mana is enough to cover it
				damage = 0;
			}
			else
			{
				// Set mana damage to target's mana and reduce the remaining
				// damage from life if the mana is not enough.
				damage -= (manaDamage - target.Mana) * manaShield.RankData.Var1;
				manaDamage = target.Mana;
			}

			// Reduce mana
			target.Mana -= manaDamage;

			if (target.Mana <= 0)
				ChannelServer.Instance.SkillManager.GetHandler<StartStopSkillHandler>(SkillId.ManaShield).Stop(target, manaShield);

			tAction.ManaDamage = manaDamage;
		}

		/// <summary>
		/// Checks if attacker has Critical Hit and applies crit bonus
		/// by chance. Also sets the target action's critical option if a
		/// crit happens.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="critChance"></param>
		/// <param name="damage"></param>
		/// <param name="tAction"></param>
		public static void HandleCritical(Creature attacker, float critChance, ref float damage, TargetAction tAction)
		{
			// Check if attacker actually has critical hit
			var critSkill = attacker.Skills.Get(SkillId.CriticalHit);
			if (critSkill == null)
				return;

			// Does the crit happen?
			if (RandomProvider.Get().NextDouble() > critChance)
				return;

			// Add crit bonus
			var bonus = critSkill.RankData.Var1 / 100f;
			damage = damage + (damage * bonus);

			// Set target option
			tAction.Set(TargetOptions.Critical);
		}

		/// <summary>
		/// Checks if target has Defense skill activated and makes the necessary
		/// changes to the actions, stun times, and damage.
		/// </summary>
		/// <param name="aAction"></param>
		/// <param name="tAction"></param>
		/// <param name="damage"></param>
		/// <returns></returns>
		public static bool HandleDefense(AttackerAction aAction, TargetAction tAction, ref float damage)
		{
			// Defense
			if (!tAction.Creature.Skills.IsActive(SkillId.Defense))
				return false;

			// Update actions
			tAction.Type = CombatActionType.Defended;
			tAction.SkillId = SkillId.Defense;
			tAction.Creature.Stun = tAction.Stun = DefenseTargetStun;
			aAction.Creature.Stun = aAction.Stun = DefenseAttackerStun;

			// Reduce damage
			var defenseSkill = tAction.Creature.Skills.Get(SkillId.Defense);
			if (defenseSkill != null)
				damage -= defenseSkill.RankData.Var3;

			Send.SkillUseStun(tAction.Creature, SkillId.Defense, 1000, 0);

			return true;
		}

      //  public static vo HandleCounter(AttackerAction aAction, TargetAction tAction, ref float damage)
       // {
            // Counter
         //   if (!tAction.Creature.Skills.IsActive(SkillId.MeleeCounterattack))
           //     return false;

            // Update actions
       //     tAction.Type = CombatActionType.CounteredHit;
         //   tAction.SkillId = SkillId.MeleeCounterattack;
          //  tAction.Creature.Stun = tAction.Stun = CounterTargetStun;
           // aAction.Creature.Stun = aAction.Stun = CounterAttackerStun;

            // Reduce damage and attack them back
           // var counterSkill = tAction.Creature.Skills.Get(SkillId.MeleeCounterattack);
           // if (counterSkill != null)
             //   damage = 0;

           // Send.SkillUseStun(tAction.Creature, SkillId.MeleeCounterattack, 1000, 0);
           // return true;
       // }

		public static CombatSkillResult TryCounter(Creature target, Creature attacker)
		{
			
			if (target.Skills.IsActive(SkillId.MeleeCounterattack))
			{
				CombatSkillHandler handler; Skill counterSkill;

				handler = ChannelServer.Instance.SkillManager.GetHandler<CombatSkillHandler>(SkillId.MeleeCounterattack);
				counterSkill = target.Skills.Get(SkillId.MeleeCounterattack);

					return handler.Use(target, counterSkill, attacker.EntityId);
			}

			return CombatSkillResult.None;
		}

		/// <summary>
		/// Reduces damage by target's defense and protection.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="damage"></param>
		/// <param name="defense"></param>
		/// <param name="protection"></param>
		public static void HandleDefenseProtection(Creature target, ref float damage, bool defense = true, bool protection = true)
		{
			if (defense)
				damage = Math.Max(1, damage - target.Defense);
			if (protection && damage > 1)
				damage = Math.Max(1, damage - (damage * target.Protection));
		}

		/// <summary>
		/// Calculates the target ID for a particular area. Used in Fireball, Icespear, WM, others?
		/// </summary>
		public static ulong GetAreaTargetID(Region region, uint x, uint y)
		{
			return 0x3000000000000000 + ((ulong)region.Id << 32) + ((x / 20) << 16) + (y / 20);
		}

		/// <summary>
		/// Fills stack and sends update.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		public static void FillStack(Creature creature, Skill skill)
		{
			IncStack(creature, skill, skill.RankData.StackMax);
		}

		/// <summary>
		/// Increases stack and sends update.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="amount"></param>
		public static void IncStack(Creature creature, Skill skill, byte amount = 1)
		{
			if (creature.ActiveSkillStacks + amount > skill.RankData.StackMax)
				creature.ActiveSkillStacks = skill.RankData.StackMax;
			else
				creature.ActiveSkillStacks += amount;
			Send.SkillStackSet(creature, skill, creature.ActiveSkillStacks);
		}

		/// <summary>
		/// Removes stack and sends update.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		public static void ClearStack(Creature creature, Skill skill)
		{
			DecStack(creature, skill, byte.MaxValue);
		}

		/// <summary>
		/// Decreases stack and sends update.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="amount"></param>
		public static void DecStack(Creature creature, Skill skill, byte amount = 1)
		{
			if (creature.ActiveSkillStacks > amount)
				creature.ActiveSkillStacks -= amount;
			else
				creature.ActiveSkillStacks = 0;
			Send.SkillStackUpdate(creature, skill, creature.ActiveSkillStacks);
		}
	}
}
