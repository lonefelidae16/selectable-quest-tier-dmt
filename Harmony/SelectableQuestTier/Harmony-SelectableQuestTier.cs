using DMT;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;
using System.Reflection.Emit;
using Harmony_SelectableQuestTier;


public class SelectableQuestTier
{
    public class SelectableQuestTier_Logger
    {
        public static void Log(params object[] args)
        {
            if (GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled))
            {
                Debug.Log(NullSafeJoin(", ", args));
            }
        }

        public static void LogWarning(params object[] args)
        {
            Debug.LogWarning(NullSafeJoin(", ", args));
        }

        public static void LogError(params object[] args)
        {
            Debug.LogError(NullSafeJoin(", ", args));
        }

        static string NullSafeJoin(string separator, params object[] args)
        {
            return string.Join(separator, args.Select(a => (a == null) ? "Null" : a));
        }
    }

    // Unfortunately I'm inserting patch method to avoid Exception: "MissingMethodException: void System.Collections.Generic.Dictionary`2..ctor()"
    [HarmonyPatch(typeof(NPCTieredQuestData), MethodType.Constructor)]
    [HarmonyPatch("NPCTieredQuestData")]
    public class SelectableQuestTier_NPCTieredQuestData_Ctor
    {
        static void Postfix(NPCTieredQuestData __instance)
        {
            SelectableQuestTier_Logger.Log("SelectableQuestTier_NPCTieredQuestData_Ctor instantiate");
            __instance.PlayerTieredQuestList = new Dictionary<int, NPCTieredQuestData.PlayerTieredQuestData>();
        }
    }

    [HarmonyPatch(typeof(QuestEventManager))]
    [HarmonyPatch("GetTieredQuestList")]
    public class SelectableQuestTier_QuestEventManager_GetTieredQuestList
    {
        static Dictionary<byte, List<Quest>> Postfix(Dictionary<byte, List<Quest>> __result, QuestEventManager __instance, World world, int npcEntityID, int playerEntityID)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_QuestEventManager_GetTieredQuestList custom method injection 'Postfix'");
            if (__instance.npcTieredQuestData.ContainsKey(npcEntityID))
            {
                SelectableQuestTier_Logger.Log("found NPCQuestData by NPC ID: " + npcEntityID);
                NPCTieredQuestData nPCQuestData = __instance.npcTieredQuestData[npcEntityID];
                if (nPCQuestData.PlayerTieredQuestList.ContainsKey(playerEntityID))
                {
                    SelectableQuestTier_Logger.Log("found PlayerQuestList by Player ID: " + playerEntityID);
                    var questData = nPCQuestData.PlayerTieredQuestList[playerEntityID];
                    if ((int)(world.GetWorldTime() - questData.LastUpdate) > 24000)
                    {
                        SelectableQuestTier_Logger.Log("Clearing TieredQuestList (world.GetWorldTime() - questData.LastUpdate > 24000 : questData.LastUpdate == " + questData.LastUpdate + ")");
                        questData.TieredQuestList = null;
                    }
                    __result = questData.TieredQuestList;
                }
            }
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_QuestEventManager_GetTieredQuestList custom method injection 'Postfix'");
            return __result;
        }
    }

    [HarmonyPatch(typeof(QuestEventManager))]
    [HarmonyPatch("ClearQuestList")]
    public class SelectableQuestTier_QuestEventManager_ClearQuestList
    {
        static void Postfix(QuestEventManager __instance, int npcEntityID)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_QuestEventManager_ClearQuestList patcher method 'Postfix'");
            if (__instance.npcTieredQuestData.ContainsKey(npcEntityID))
            {
                SelectableQuestTier_Logger.Log("Clearing quest list by NPC: ID = " + npcEntityID);
                __instance.npcTieredQuestData[npcEntityID].PlayerTieredQuestList.Clear();
            }
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_QuestEventManager_ClearQuestList patcher method 'Postfix'");
        }
    }

    [HarmonyPatch(typeof(QuestEventManager))]
    [HarmonyPatch("Cleanup")]
    public class SelectableQuestTier_QuestEventManager_Cleanup
    {
        static void Postfix(QuestEventManager __instance)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_QuestEventManager_Cleanup patcher method 'Postfix'");
            SelectableQuestTier_Logger.Log("Cleaning up");
            __instance.npcTieredQuestData.Clear();
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_QuestEventManager_Cleanup patcher method 'Postfix'");
        }
    }

    // Unfortunately I'm inserting patch method to avoid Exception: "MissingMethodException: void System.Collections.Generic.Dictionary`2..ctor()"
    [HarmonyPatch(typeof(QuestEventManager))]
    [HarmonyPatch("get_Current")]
    public class SelectableQuestTier_QuestEventManager_get_Current
    {
        static void Postfix(QuestEventManager __result)
        {
            if (__result.npcTieredQuestData == null)
            {
                SelectableQuestTier_Logger.Log("QuestEventManager.npcTieredQuestData instantiate");
                __result.npcTieredQuestData = new Dictionary<int, NPCTieredQuestData>();
            }
        }
    }

    [HarmonyPatch(typeof(EntityNPC))]
    [HarmonyPatch("OnEntityActivated")]
    public class SelectableQuestTier_EntityNPC_OnEntityActivated
    {
        static void Postfix(EntityNPC __instance, int _indexInBlockActivationCommands, Vector3i _tePos, EntityAlive _entityFocusing)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_EntityNPC_OnEntityActivated patcher method 'Postfix'");
            if (__instance.activeTieredQuests == null)
            {
                SelectableQuestTier_Logger.Log("restoring 'activeTieredQuests'");
                __instance.activeTieredQuests = QuestEventManager.Current.GetTieredQuestList(GameManager.Instance.World, __instance.entityId, _entityFocusing.entityId);
                if (__instance.activeTieredQuests == null)
                {
                    throw new Exception("ObjectNotFoundException: QuestEventManager.Current.GetTieredQuestList() returns Null; restoring FAILED");
                }
            }
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_EntityNPC_OnEntityActivated patcher method 'Postfix'");
        }
    }

    [HarmonyPatch(typeof(EntityNPC))]
    [HarmonyPatch("PopulateActiveQuests")]
    public class SelectableQuestTier_EntityNPC_PopulateActiveQuests
    {
        static void Postfix(EntityNPC __instance, EntityPlayer player, int currentTier)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_EntityNPC_PopulateActiveQuests patcher method 'Postfix'");

            // Init
            __instance.activeTieredQuests = new Dictionary<byte, List<Quest>>();
            for (byte i=1; i<=Quest.MaxQuestTier; ++i)
            {
                __instance.activeTieredQuests[i] = new List<Quest>();
            }

            var tmpQuestEntryList = __instance.questList;
            SelectableQuestTier_Logger.Log("List of QuestEntryList.Count == " + tmpQuestEntryList.Count);

            for (byte t=1; t<=5; ++t)
            {
                // Fill QuestList Range 5 (DifficultyTier <= 2), or RandomRange 3 to 5 (DifficultyTier == 3), or RandomRange 2 to 4 (DifficultyTier >= 4)
                int maxCount = (t <= 2) ? 5 : ((t == 3) ? 3 + __instance.rand.RandomRange(2) : 2 + __instance.rand.RandomRange(2));
                for (int i=0; i<200; ++i)
                {
                    int idx = __instance.rand.RandomRange(tmpQuestEntryList.Count);
                    QuestEntry questEntry = tmpQuestEntryList[idx];
                    QuestClass questClass = questEntry.QuestClass;
                    var tier = questClass.DifficultyTier;
                    if (t != tier || questClass.QuestType != "" || !(__instance.rand.RandomFloat < questEntry.Prob))
                    {
                        continue;
                    }
                    Quest quest = GenerateQuest(__instance, player, questClass);
                    if (quest == null)
                    {
                        continue;
                    }
                    __instance.activeTieredQuests[tier].Add(quest);
                    SelectableQuestTier_Logger.Log("append activeTieredQuests; Tier == " + tier);
                    if (__instance.activeTieredQuests[t].Count >= maxCount)
                    {
                        break;
                    }
                }
            }
            SelectableQuestTier_Logger.Log("EntityNPC.activeTieredQuests Generation Complete");
            SetupTieredQuestList(QuestEventManager.Current, __instance.entityId, player.entityId, __instance.activeTieredQuests);
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_EntityNPC_PopulateActiveQuests patcher method 'Postfix'");
        }

        // Helper Methods
        static Quest GenerateQuest(EntityNPC targetNPC, EntityPlayer player, QuestClass questClass)
        {
            bool @bool = GameStats.GetBool(EnumGameStats.EnemySpawnMode);
            Quest quest = questClass.CreateQuest();
            quest.QuestGiverID = targetNPC.entityId;
            quest.SetPositionData(Quest.PositionDataTypes.QuestGiver, targetNPC.position);
            quest.SetupTags();
            if (@bool || (quest.QuestTags & QuestTags.clear) == 0)
            {
                if ((quest.QuestTags & QuestTags.treasure) == 0 && GameSparksCollector.CollectGamePlayData)
                {
                    GameSparksCollector.IncrementCounter(GameSparksCollector.GSDataKey.QuestOfferedDistance, ((int)Vector3.Distance(quest.Position, targetNPC.position) / 50 * 50).ToString(), 1);
                }
                if (!quest.NeedsNPCSetPosition || quest.SetupPosition(targetNPC, player, targetNPC.usedPOILocations, player.entityId))
                {
                    return quest;
                }
            }
            return null;
        }

        static void SetupTieredQuestList(QuestEventManager manager, int nPCId, int playerId, Dictionary<byte, List<Quest>> tieredQuestList)
        {
            if (tieredQuestList == null)
            {
                throw new ArgumentException("tieredQuestList is Null");
            }
            if (!manager.npcTieredQuestData.ContainsKey(nPCId))
            {
                SelectableQuestTier_Logger.Log("New quest list accepted by NPC: ID = " + nPCId);
                manager.npcTieredQuestData.Add(nPCId, new NPCTieredQuestData());
            }
            if (!manager.npcTieredQuestData[nPCId].PlayerTieredQuestList.ContainsKey(playerId))
            {
                SelectableQuestTier_Logger.Log("New player accepted: ID = " + playerId);
                manager.npcTieredQuestData[nPCId].PlayerTieredQuestList.Add(playerId, new NPCTieredQuestData.PlayerTieredQuestData(tieredQuestList));
            }
            else
            {
                SelectableQuestTier_Logger.Log("Player ID existing: ID = " + playerId);
                manager.npcTieredQuestData[nPCId].PlayerTieredQuestList[playerId].TieredQuestList = tieredQuestList;
            }
        }
    }

    [HarmonyPatch(typeof(EntityNPC))]
    [HarmonyPatch("ClearActiveQuests")]
    public class SelectableQuestTier_EntityNPC_ClearActiveQuests
    {
        static void Postfix(EntityNPC __instance, int playerID)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_EntityNPC_ClearActiveQuests patcher method 'Postfix'");
            try
            {
                __instance.activeTieredQuests = null;
            }
            catch
            {
            }
            // TODO: Multi Player Support
            // if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            // {
            //     SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageNPCQuestList>().Setup(entityId, playerID));
            // }
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_EntityNPC_ClearActiveQuests patcher method 'Postfix'");
        }
    }

    [HarmonyPatch(typeof(DialogStatement))]
    [HarmonyPatch("GetResponses")]
    public class SelectableQuestTier_DialogStatement_GetResponses
    {
        static List<DialogResponse> Postfix(List<DialogResponse> __result, DialogStatement __instance)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_DialogStatement_GetResponses patcher method 'Postfix'");
            __result.Clear();
            if (__instance.ResponseEntries.Count > 0)
            {
                for (int i = 0; i < __instance.ResponseEntries.Count; i++)
                {
                    switch (__instance.ResponseEntries[i].ResponseType)
                    {
                    case BaseResponseEntry.ResponseTypes.Response:
                        __result.Add(__instance.OwnerDialog.GetResponse(__instance.ResponseEntries[i].ID));
                        break;
                    case BaseResponseEntry.ResponseTypes.QuestAdd:
                    {
                        DialogQuestResponseEntry dialogQuestResponseEntry = __instance.ResponseEntries[i] as DialogQuestResponseEntry;
                        DialogResponseQuest dialogResponseQuest = new DialogResponseQuest(dialogQuestResponseEntry.ID);
                        byte _tier = 0;
                        switch (__instance.ID)
                        {
                            case "currentjobsT1":
                                SelectableQuestTier_Logger.Log("T1 selected");
                                _tier = 1;
                                break;
                            case "currentjobsT2":
                                SelectableQuestTier_Logger.Log("T2 selected");
                                _tier = 2;
                                break;
                            case "currentjobsT3":
                                SelectableQuestTier_Logger.Log("T3 selected");
                                _tier = 3;
                                break;
                            case "currentjobsT4":
                                SelectableQuestTier_Logger.Log("T4 selected");
                                _tier = 4;
                                break;
                            case "currentjobsT5":
                                SelectableQuestTier_Logger.Log("T5 selected");
                                _tier = 5;
                                break;
                        }
                        ReInit(dialogResponseQuest, dialogQuestResponseEntry.ReturnStatementID, dialogQuestResponseEntry.questType, __instance.OwnerDialog, dialogQuestResponseEntry.ListIndex, _tier);
                        if (dialogResponseQuest.IsValid)
                        {
                            SelectableQuestTier_Logger.Log("instantiate DialogQuestResponseEntry");
                            __result.Add(dialogResponseQuest);
                        }
                        break;
                    }
                    }
                }
            }
            else if (__instance.NextStatementID != "")
            {
                __result.Add(DialogResponse.NextStatementEntry(__instance.NextStatementID));
            }

            SelectableQuestTier_Logger.Log("List<DialogResponse> __result .Count == " + __result.Count);
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_DialogStatement_GetResponses patcher method 'Postfix'");
            return __result;
        }

        // Helper Method
        static void ReInit(DialogResponseQuest instance, string _nextStatementID, string _type, Dialog _ownerDialog, int listIndex = -1, byte tier = 0)
        {
            instance.OwnerDialog = _ownerDialog;

            Quest quest = null;
            LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(GameManager.Instance.World.GetPrimaryPlayer());
            EntityNPC respondent = uIForPlayer.xui.Dialog.Respondent;
            if (tier > Quest.MaxQuestTier)
            {
                throw new ArgumentException("Tier value over Quest.MaxQuestTier ( == " + Quest.MaxQuestTier + ")");
            }
            if (instance.ID != "")
            {
                quest = QuestClass.GetQuest(instance.ID).CreateQuest();
                quest.SetupTags();
                quest.SetupPosition(respondent);
            }
            else
            {
                List<Quest> activeQuests;
                if (_type == "" && respondent.activeTieredQuests != null && respondent.activeTieredQuests.ContainsKey(tier))
                {
                    activeQuests = respondent.activeTieredQuests[tier];
                    if (activeQuests != null && listIndex < activeQuests.Count && activeQuests[listIndex].QuestClass.QuestType == "")
                    {
                        quest = activeQuests[listIndex];
                    }
                    else
                    {
                        instance.IsValid = false;
                    }
                }
                else
                {
                    activeQuests = respondent.activeQuests;
                    int num = 0;
                    for (int i = 0; i < activeQuests.Count; i++)
                    {
                        if (activeQuests[i].QuestClass.QuestType == _type && activeQuests[i].QuestClass.DifficultyTier <= uIForPlayer.entityPlayer.QuestJournal.GetCurrentFactionTier(0))
                        {
                            if (listIndex == num)
                            {
                                quest = activeQuests[i];
                                num = -1;
                                break;
                            }
                            num++;
                        }
                    }
                    if (num != -1)
                    {
                        instance.IsValid = false;
                    }
                }
            }
            if (instance.IsValid)
            {
                instance.Quest = quest;
                instance.AddAction(new DialogActionAddQuest
                {
                    Quest = quest,
                    Owner = instance.OwnerDialog,
                    ListIndex = listIndex
                });
                instance.ReturnStatementID = _nextStatementID;
                instance.NextStatementID = _nextStatementID;
                if (tier == 0)
                {
                    tier = quest.QuestClass.DifficultyTier;
                }
                string text = "I";
                switch (tier)
                {
                case 1:
                    text = "I";
                    break;
                case 2:
                    text = "II";
                    break;
                case 3:
                    text = "III";
                    break;
                case 4:
                    text = "IV";
                    break;
                case 5:
                    text = "V";
                    break;
                }
                instance.Text = "[[DECEA3]" + Localization.Get("xuiTier").ToUpper() + " " + text + "[-]] " + quest.GetParsedText(quest.QuestClass.ResponseText);
                SelectableQuestTier_Logger.Log("Generated Text = '" + instance.Text + "'");
            }
        }
    }

    [HarmonyPatch(typeof(XUiC_QuestOfferWindow))]
    [HarmonyPatch("OnClose")]
    public class SelectableQuestTier_XUiC_QuestOfferWindow_OnClose
    {
        static bool Prefix(XUiC_QuestOfferWindow __instance)
        {
            SelectableQuestTier_Logger.Log(">>> SelectableQuestTier_XUiC_QuestOfferWindow_OnClose patcher method 'Prefix'");
            var activeTieredQuests = __instance.xui.Dialog.Respondent.activeTieredQuests;
            if (__instance.OfferType == XUiC_QuestOfferWindow.OfferTypes.Dialog && __instance.questAccepted && activeTieredQuests != null)
            {
                Quest quest = __instance.Quest;
                var tier = quest.QuestClass.DifficultyTier;
                bool isFetch = (quest.QuestTags & QuestTags.fetch) == QuestTags.fetch;
                bool isClear = (quest.QuestTags & QuestTags.clear) == QuestTags.clear;
                if (activeTieredQuests.ContainsKey(tier) && (!GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled) || !isFetch || !isClear))
                {
                    if (activeTieredQuests[tier].Remove(quest))
                    {
                        SelectableQuestTier_Logger.Log("Deleting Quest from activeTieredQuests, Tier == " + tier);
                    }
                }
                // TODO: Multi Player Support
                // if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                // {
                //     SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageNPCQuestList>().Setup(QuestGiverID, base.xui.playerUI.entityPlayer.entityId, (byte)listIndex));
                // }
            }
            SelectableQuestTier_Logger.Log("<<< SelectableQuestTier_XUiC_QuestOfferWindow_OnClose patcher method 'Prefix'");
            return true;
        }
    }
}
