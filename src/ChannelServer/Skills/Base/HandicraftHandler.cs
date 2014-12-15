// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.World.Entities;
using Aura.Shared.Network;
using System;

namespace Aura.Channel.Skills.Base
{
    public class HandicraftHandler : ICancelable
    {
        public virtual void HandiPrepare(Creature creature, Skill skill, int castTime, Packet packet)
        {
            throw new NotImplementedException();
        }

        public virtual void HandiReady(Creature creature, Skill skill, Packet packet)
        {
            throw new NotImplementedException();
        }

        public virtual void HandiComplete(Creature creature, Skill skill, Packet packet)
        {
            throw new NotImplementedException();
        }

        public virtual void Cancel(Creature creature, Skill skill)
        {
        }
    }
}
