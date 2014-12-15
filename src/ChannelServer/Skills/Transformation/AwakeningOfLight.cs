// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Aura.Channel.Skills;

namespace Aura.World.Skills.Handlers.Transformations
{
	/// <summary>
	/// Var1: Duration
	/// Var2: Cooldown
	/// Var3/4/5: HP/MP/STM recovery
	/// Var6: ? (1500-1900)
	/// Var7: Arat Berry Radius (5k-8k)
	/// Var8: Stun Negated (%) (20-40) 
	/// Var9: Skill exp used? (600 (/1000))
	/// </summary>

	// <To-Do List>
	// Timer/Duration. (var 1)
	// Arat berry radius function... whenever we have basic shadow mission function?
	// Stun negation <- prolly whenever we actually have a proper stun helper in place.. maybe..? Nothing mentioned in current future scope regarding the feature.
	// Do training/skill exp drain.

	[Skill(SkillId.AwakeningofLight)]
	public class AwakeningOfLightHandler : StartStopSkillHandler
	{
		private const uint EruptionRadius = 750;
		private const uint EruptionDamage = 120;
		private const short EruptionStun = 5000;
		private const int EruptionKnockBack = 375;

		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Conditions.Activate(ConditionsB.Demigod);

			creature.StopMove();

			// Spawn eruption
			{
				var pos = creature.GetPosition();
				var targets =  creature.Region.GetAttackableCreaturesInRange(creature, (int)EruptionRadius); 

				// Prepare combat actions
				
				var aAction = new AttackerAction(CombatActionType.SpecialHit, creature, skill.Info.Id, (long)SkillHelper.GetAreaTargetID(creature.Region, (uint)pos.X, (uint)pos.Y));
				aAction.Options |= AttackerOptions.KnockBackHit1 | AttackerOptions.UseEffect;

				var t = new Thread(() =>
				{
					Thread.Sleep(1000);
				foreach (var target in targets)
				{
					target.StopMove();
					var targetPosition = target.GetPosition();

					// Officials use CM skill id.
					var tAction = new TargetAction(CombatActionType.TakeHit, target, creature, skill.Info.Id);
					tAction.Options |= TargetOptions.KnockDown | TargetOptions.Result;
					target.Stun = tAction.Stun = EruptionStun;
					// tAction.Delay = 1000; <- pretty sure this just delays the knockback, not the actual attack.
					target.KnockBack = EruptionKnockBack;

					// Apply damage
					target.TakeDamage(tAction.Damage = creature.GetMagicDamage(null, EruptionDamage), creature);

					// Check collisions
					Position intersection;
					var knockbackPos = creature.GetPosition().GetRelative(targetPosition, EruptionKnockBack);
					if (target.Region.Collisions.Find(targetPosition, knockbackPos, out intersection))
						knockbackPos = targetPosition.GetRelative(intersection, -50);

					// Set knockbacked position
					target.SetPosition(knockbackPos.X, knockbackPos.Y);

					var cap = new CombatActionPack(creature, skill.Info.Id, aAction, tAction);

					cap.Handle(); 	
				}

				});

				t.Start();
			}
			Send.EffectDelayed(Effect.AwakeningOfLight1, 800, creature);
			Send.EffectDelayed(Effect.AwakeningOfLight2, 800, creature);
			Send.UseMotion(creature, 67, 3, false, false);


			creature.Regens.Add("DemiRegen", Stat.Life, (0.12f * ((skill.RankData.Var3 - 100) / 100)), creature.LifeMax);
			creature.Regens.Add("DemiRegen", Stat.Mana, (0.4f * ((skill.RankData.Var4 - 100) / 100)), creature.ManaMax);
			creature.Regens.Add("DemiRegen", Stat.Stamina, skill.RankData.Var5, creature.StaminaMax);

			Send.StatUpdateDefault(creature);

			return StartStopResult.Okay;
		}


		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Conditions.Deactivate(ConditionsB.Demigod);

			creature.Regens.Remove("DemiRegen");

			//WorldManager.Instance.Broadcast(PacketCreator.StatRegenStop(creature, StatUpdateType.Public, creature.Temp.DemiHpRegen, creature.Temp.DemiMpRegen, creature.Temp.DemiStmRegen), SendTargets.Range, creature);
			//WorldManager.Instance.Broadcast(PacketCreator.StatRegenStop(creature, StatUpdateType.Private, creature.Temp.DemiHpRegen, creature.Temp.DemiMpRegen, creature.Temp.DemiStmRegen), SendTargets.Range, creature);
			//WorldManager.Instance.CreatureStatsUpdate(creature);

			return StartStopResult.Okay;
		}
	}
}
