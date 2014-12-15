// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Channel.Util;
using Aura.Shared.Network;
using Aura.Channel.Network.Sending;
using Aura.Shared.Util;
using Aura.Shared.Mabi.Const;
using Aura.Channel.Skills.Base;

namespace Aura.Channel.Network.Handlers
{
    public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
    {

        /// <summary>
        /// Starting/Activating a skill.
        /// </summary>
        /// <remarks>
        /// The second paramter seems to always be a string if the
        /// main player uses the skill. When pets use it you get a byte.
        /// The string is a MabiDictionary with optional extra information,
        /// like chair ids for Rest.
        /// </remarks>
        /// <example>
        /// 0001 [............2714] Short  : 10004
        /// 0002 [................] String : 
        /// 
        /// 0001 [............2714] Short  : 10004
        /// 0002 [..............01] Byte   : 1
        /// </example>
        [PacketHandler(Op.HandicraftUse)]
        public void HandiStart(ChannelClient client, Packet packet)
        {
            var skillId = (SkillId)packet.GetUShort();

            var creature = client.GetCreatureSafe(packet.Id);

            var skill = creature.Skills.GetSafe(skillId);

            var handler = ChannelServer.Instance.SkillManager.GetHandler<IStartable>(skillId);
            if (handler == null)
            {
                Log.Unimplemented("SkillStart: Skill handler or interface for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented yet."));
                Send.SkillStartSilentCancel(creature, skillId);
                return;
            }

            try
            {
                handler.Start(creature, skill, packet);
            }
            catch (NotImplementedException)
            {
                Log.Unimplemented("SkillStart: Skill start method for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented completely yet."));
                Send.SkillStartSilentCancel(creature, skillId);
            }
        }


        /// <summary>
        /// Preparing a skill before it can be used.
        /// </summary>
        /// <remarks>
        /// Preparing a skill is the first step of getting a skill ready
        /// to be used. The next packet usually is "Ready", once the skill
        /// is loaded. Some skills skip that step though.
        /// </remarks>
        /// <example>
        /// 0001 [............2714] Short  : 50057
        /// 0002 [................] String : 
        /// </example>
        [PacketHandler(Op.SkillPrepare)]
        public void HandiPrepare(ChannelClient client, Packet packet)
        {
            var skillId = (SkillId)packet.GetUShort();

            var creature = client.GetCreatureSafe(packet.Id);

            var skill = creature.Skills.GetSafe(skillId);

            var handler = ChannelServer.Instance.SkillManager.GetHandler<IPreparable>(skillId);
            if (handler == null)
            {
                Log.Unimplemented("SkillPrepare: Skill handler or interface for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented yet."));
                Send.SkillPrepareSilentCancel(creature, skillId);
                return;
            }

            try
            {
                var loadtime = ChannelServer.Instance.Conf.World.CombatSystem == Util.Configuration.Files.CombatSystem.Dynamic
                    ? skill.RankData.NewLoadTime
                    : skill.RankData.LoadTime;

                handler.Prepare(creature, skill, loadtime, packet);

                creature.Skills.SkillInProgress = true;
            }
            catch (NotImplementedException)
            {
                Log.Unimplemented("SkillPrepare: Skill prepare method for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented completely yet."));
                Send.SkillPrepareSilentCancel(creature, skillId);
            }
        }

        /// <summary>
        /// Using a skill.
        /// </summary>
        /// <remarks>
        /// Sent after Prepare and maybe Ready.
        /// </remarks>
        /// <example>
        /// 0001 [............2714] Short  : 50057
        /// 0002 [................] String : 
        /// </example>
        [PacketHandler(Op.HandicraftUse)]
        public void HandiUse(ChannelClient client, Packet packet)
        {
            var skillId = (SkillId)packet.GetUShort();

            var creature = client.GetCreatureSafe(packet.Id);

            var skill = creature.Skills.GetSafe(skillId);

            var handler = ChannelServer.Instance.SkillManager.GetHandler<IUseable>(skillId);
            if (handler == null)
            {
                Log.Unimplemented("SkillUse: Skill handler or interface for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented yet."));
                Send.SkillUseSilentCancel(creature);
                return;
            }

            try
            {
                handler.Use(creature, skill, packet);
            }
            catch (NotImplementedException)
            {
                Log.Unimplemented("SkillUse: Skill use method for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented completely yet."));
                Send.SkillUseSilentCancel(creature);
            }
        }

        /// <summary>
        /// Completing skill usage.
        /// </summary>
        /// <remarks>
        /// Sent after skill was used successfully.
        /// </remarks>
        /// <example>
        /// 0001 [............2714] Short  : 50057
        /// 0002 [................] String : 
        /// </example>
        [PacketHandler(Op.SkillComplete)]
        public void HandiComplete(ChannelClient client, Packet packet)
        {
            var skillId = (SkillId)packet.GetUShort();

            var creature = client.GetCreatureSafe(packet.Id);

            var skill = creature.Skills.GetSafe(skillId);

            var handler = ChannelServer.Instance.SkillManager.GetHandler<ICompletable>(skillId);
            if (handler == null)
            {
                Log.Unimplemented("SkillComplete: Skill handler or interface for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented yet."));
                // Cancel?
                goto L_End;
            }

            try
            {
                handler.Complete(creature, skill, packet);
            }
            catch (NotImplementedException)
            {
                Log.Unimplemented("SkillComplete: Skill complete method for '{0}'.", skillId);
                Send.ServerMessage(creature, Localization.Get("This skill isn't implemented completely yet."));
                // Cancel?
            }

        L_End:
            // Always set active skill to null after complete.
            if (creature.ActiveSkillStacks == 0)
            {
                creature.Skills.ActiveSkill = null;
                creature.Skills.SkillInProgress = false;
            }
        }
    }
}
