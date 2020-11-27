using System;
using System.Xml;
using UnityEngine;


// Codes Injection
public class DialogRequirementCurrentTierSDX : BaseDialogRequirement
{
    public override RequirementTypes RequirementType => RequirementTypes.QuestsAvailable;

    public override string GetRequiredDescription(EntityPlayer player)
    {
        return "";
    }

    public override bool CheckRequirement(EntityPlayer player, EntityNPC talkingTo)
    {
        int tier = int.MaxValue;
        LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(GameManager.Instance.World.GetPrimaryPlayer());
        try
        {
            tier = int.Parse(base.Value);
        }
        catch
        {
            UnityEngine.Debug.LogError("Failed to cast: XmlParseException: Tier value must be Int32!");
        }
        return tier <= uIForPlayer.entityPlayer.QuestJournal.GetCurrentFactionTier(0) || GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled);
    }
}
