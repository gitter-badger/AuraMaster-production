// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

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
using Aura.Channel.Skills;

namespace Aura.Channel.Skills.Actions
{
	[Skill(SkillId.SpreadWings)]
	public class SpreadWingsHandler : StartStopSkillHandler
	{
		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Conditions.Activate(ConditionsD.SpreadWings);
			var item = creature.Inventory.GetItemAt(Pocket.Robe, 0, 0);
			var item2 = creature.Inventory.GetItemAt(Pocket.RobeStyle, 0, 0);

			Send.SpreadWings(creature, true);

			// Methods to update spreadwings to make wings flap for selected devil wings sets.
			// Needs more research for a cleaner, better method.
			// Perhaps has to do with wingtype string and item entity id?
			if ((item != null && item.Info.Id == 19157) || (item2 != null && item2.Info.Id == 19157))
			{
				creature.Region.Broadcast(new Packet(Op.ConditionUpdate, creature.EntityId).PutLong(0).PutLong(0).PutLong(0).PutLong(1099511627776).PutLong(0).PutInt(1).PutInt(232).PutString("WING_TYPE:1:1").PutLong(0));
				creature.Region.Broadcast(new Packet(0x702E, creature.EntityId).PutInt(19157));

				return StartStopResult.Okay;
			}

			if ((item != null && item.Info.Id == 19158) || (item2 != null && item2.Info.Id == 19158))
			{
				creature.Region.Broadcast(new Packet(Op.ConditionUpdate, creature.EntityId).PutLong(0).PutLong(0).PutLong(0).PutLong(1099511627776).PutLong(0).PutInt(1).PutInt(232).PutString("WING_TYPE:1:1").PutLong(0));
				creature.Region.Broadcast(new Packet(0x702E, creature.EntityId).PutInt(19158));

				return StartStopResult.Okay;
			}

			if ((item != null && item.Info.Id == 19159) || (item2 != null && item2.Info.Id == 19159))
			{
				creature.Region.Broadcast(new Packet(Op.ConditionUpdate, creature.EntityId).PutLong(0).PutLong(0).PutLong(0).PutLong(1099511627776).PutLong(0).PutInt(1).PutInt(232).PutString("WING_TYPE:1:1").PutLong(0));
				creature.Region.Broadcast(new Packet(0x702E, creature.EntityId).PutInt(19159));
				return StartStopResult.Okay;
			}

			return StartStopResult.Okay;
		}

		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Conditions.Deactivate(ConditionsD.SpreadWings);
			Send.SpreadWings(creature, false);

			return StartStopResult.Okay;
		}
	}
}
