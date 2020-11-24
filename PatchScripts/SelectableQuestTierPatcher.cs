using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Text;
using UnityEngine;
using SDX.Core;
using SDX.Compiler;
using SDX.Payload;
using Mono.Cecil;
// using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using System.Linq;
//using System.Reflection;


// Pathcer
public class Patcher_SelectableQuestTier : IPatcherMod
{
    public bool Patch(ModuleDefinition module)
    {
        FieldDefinition field;
        TypeDefinition gameClass;
        MethodDefinition gameMethod;

        TypeReference voidTypeRef = module.TypeSystem.Void;
        TypeReference intTypeRef = module.TypeSystem.Int32;
        TypeReference byteTypeRef = module.TypeSystem.Byte;
        TypeReference stringTypeRef = module.TypeSystem.String;
        TypeReference objectTypeRef = module.TypeSystem.Object;
        TypeReference ulongTypeRef = module.TypeSystem.UInt64;
        MethodDefinition objectCtor = module.ImportReference(typeof(object)).Resolve().Methods.First(d => d.Name == ".ctor");
        TypeReference tDifficultyTier = module.Types.First(d => d.Name == "QuestClass").Properties.First(d => d.Name == "DifficultyTier").PropertyType;

        string _namespace = "Harmony_SelectableQuestTier";
        // string _namespace = "";

        Console.WriteLine("==QuestList Patcher==");


        // Making required things public
        var cQuestClass = module.Types.First(d => d.Name == "QuestClass");
        var mGetQuest = cQuestClass.Methods.First(d => d.Name == "GetQuest");
        SetMethodToPublic(mGetQuest);

        var cEntityNPC = module.Types.First(d => d.Name == "EntityNPC");
        var fUsedPOILocations = cEntityNPC.Fields.First(d => d.Name == "usedPOILocations");
        SetFieldToPublic(fUsedPOILocations);

        var cBaseStatement = module.Types.First(d => d.Name == "BaseStatement");
        var mNextStatementID_setter = cBaseStatement.Methods.First(d => d.Name == "set_NextStatementID");
        SetMethodToPublic(mNextStatementID_setter);
        var mAddAction = cBaseStatement.Methods.First(d => d.Name == "AddAction");
        SetMethodToPublic(mAddAction);

        var cDialogResponse = module.Types.First(d => d.Name == "DialogResponse");
        var mNextStatementEntry = cDialogResponse.Methods.First(d => d.Name == "NextStatementEntry");
        SetMethodToPublic(mNextStatementEntry);

        var cDialog = module.Types.First(d => d.Name == "Dialog");
        var mGetResponse = cDialog.Methods.First(d => d.Name == "GetResponse");
        SetMethodToPublic(mGetResponse);

        var cXUiC_QuestOfferWindow = module.Types.First(d => d.Name == "XUiC_QuestOfferWindow");
        var fQuestAccepted = cXUiC_QuestOfferWindow.Fields.First(d => d.Name == "questAccepted");
        SetFieldToPublic(fQuestAccepted);

        var gTieredQuestList = new GenericInstanceType(module.ImportReference(typeof(Dictionary<,>)));
        gTieredQuestList.GenericArguments.Add(tDifficultyTier);
        gTieredQuestList.GenericArguments.Add(module.ImportReference(typeof(List<Quest>)));

        // Add class
        /*
        namespace ${_namespace}
        {
            public class NPCTieredQuestData
            {
                public class PlayerTieredQuestData
                {
                    private Dictionary<byte, List<Quest>> tieredQuestList;
                    public ulong LastUpdate;

                    public Dictionary<byte, List<Quest>> TieredQuestList
                    {
                        get
                        {
                            return tieredQuestList;
                        }
                        set
                        {
                            tieredQuestList = value;
                            LastUpdate = GameManager.Instance.World.GetWorldTime() / 24000uL * 24000;
                        }
                    }

                    public PlayerTieredQuestData(Dictionary<byte, List<Quest>> tieredQuestList)
                    {
                        TieredQuestList = tieredQuestList;
                    }
                }

                public Dictionary<int, PlayerTieredQuestData> PlayerTieredQuestList = new Dictionary<int, PlayerTieredQuestData>();
            }
        }
        */
        var cNPCTieredQuestData = new TypeDefinition(_namespace, "NPCTieredQuestData", TypeAttributes.Class | TypeAttributes.Public, objectTypeRef);
        var cnPlayerTieredQuestData = new TypeDefinition("", "PlayerTieredQuestData", TypeAttributes.Class | TypeAttributes.NestedPublic, objectTypeRef);
        var fTieredQuestList = new FieldDefinition("tieredQuestList", FieldAttributes.Private, gTieredQuestList);
        cnPlayerTieredQuestData.Fields.Add(fTieredQuestList);
        var fLastUpdateTime = new FieldDefinition("LastUpdate", FieldAttributes.Public, ulongTypeRef);
        cnPlayerTieredQuestData.Fields.Add(fLastUpdateTime);
        var mGet_TieredQuestList = new MethodDefinition("get_TieredQuestList", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.CompilerControlled | MethodAttributes.HideBySig, gTieredQuestList);
        var mGet_TieredQuestListProc = mGet_TieredQuestList.Body.GetILProcessor();
        mGet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mGet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldfld, fTieredQuestList));
        mGet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ret));
        cnPlayerTieredQuestData.Methods.Add(mGet_TieredQuestList);
        var mSet_TieredQuestList = new MethodDefinition("set_TieredQuestList", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.CompilerControlled | MethodAttributes.HideBySig, voidTypeRef);
        mSet_TieredQuestList.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, gTieredQuestList));
        var mSet_TieredQuestListProc = mSet_TieredQuestList.Body.GetILProcessor();
        var tGameManager = module.Types.First(d => d.Name == "GameManager");
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldarg_1));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Stfld, fTieredQuestList));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldsfld, tGameManager.Fields.First(d => d.Name == "Instance")));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Callvirt, tGameManager.Methods.First(d => d.Name == "get_World")));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Callvirt, module.Types.First(d => d.Name == "WorldBase").Methods.First(d => d.Name == "GetWorldTime")));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldc_I4, 24000));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Conv_I8));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Div_Un));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ldc_I4, 24000));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Conv_I8));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Mul));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Stfld, fLastUpdateTime));
        mSet_TieredQuestListProc.Append(Instruction.Create(OpCodes.Ret));
        cnPlayerTieredQuestData.Methods.Add(mSet_TieredQuestList);
        var pTieredQuestList = new PropertyDefinition("TieredQuestList", PropertyAttributes.None, gTieredQuestList);
        pTieredQuestList.GetMethod = mGet_TieredQuestList;
        pTieredQuestList.SetMethod = mSet_TieredQuestList;
        cnPlayerTieredQuestData.Properties.Add(pTieredQuestList);
        var mPlayerTieredQuestDataCtor = new MethodDefinition(".ctor", MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, voidTypeRef);
        mPlayerTieredQuestDataCtor.Parameters.Add(new ParameterDefinition("tieredQuestList", ParameterAttributes.None, gTieredQuestList));
        var mPlayerTieredQuestDataCtorProc = mPlayerTieredQuestDataCtor.Body.GetILProcessor();
        mPlayerTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mPlayerTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Call, module.ImportReference(objectCtor)));
        mPlayerTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mPlayerTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ldarg_1));
        mPlayerTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Call, mSet_TieredQuestList));
        mPlayerTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ret));
        cnPlayerTieredQuestData.Methods.Add(mPlayerTieredQuestDataCtor);
        cNPCTieredQuestData.NestedTypes.Add(cnPlayerTieredQuestData);
        var gDictIntCnPlayerTieredQuestData = new GenericInstanceType(module.ImportReference(typeof(Dictionary<,>)));
        gDictIntCnPlayerTieredQuestData.GenericArguments.Add(intTypeRef);
        gDictIntCnPlayerTieredQuestData.GenericArguments.Add(cnPlayerTieredQuestData);
        var fPlayerTieredQuestList = new FieldDefinition("PlayerTieredQuestList", FieldAttributes.Public, gDictIntCnPlayerTieredQuestData);
        cNPCTieredQuestData.Fields.Add(fPlayerTieredQuestList);
        // FIXME: Why throws "MissingMethodException"?
        var mNPCTieredQuestDataCtor = new MethodDefinition(".ctor", MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, voidTypeRef);
        var mNPCTieredQuestDataCtorProc = mNPCTieredQuestDataCtor.Body.GetILProcessor();
        // mNPCTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        // // HERE: "MissingMethodException: void System.Collections.Generic.Dictionary`2..ctor()"
        // mNPCTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Newobj, module.ImportReference(new MethodReference(".ctor", voidTypeRef, module.ImportReference(gDictIntCnPlayerTieredQuestData)))));
        // mNPCTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Stfld, fPlayerTieredQuestList));
        mNPCTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mNPCTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Call, module.ImportReference(objectCtor)));
        mNPCTieredQuestDataCtorProc.Append(Instruction.Create(OpCodes.Ret));
        cNPCTieredQuestData.Methods.Add(mNPCTieredQuestDataCtor);
        module.Types.Add(cNPCTieredQuestData);


        // Grab classes
        var cDialogResponseQuest = module.Types.First(d => d.Name == "DialogResponseQuest");
        var cQuestEventManager = module.Types.First(d => d.Name == "QuestEventManager");

        // Add field
        var fActiveQuestsTiered = new FieldDefinition("activeTieredQuests", FieldAttributes.Public, gTieredQuestList);
        cEntityNPC.Fields.Add(fActiveQuestsTiered);

        var gDictIntCnNPCTieredQuestData = new GenericInstanceType(module.ImportReference(typeof(Dictionary<,>)));
        gDictIntCnNPCTieredQuestData.GenericArguments.Add(intTypeRef);
        gDictIntCnNPCTieredQuestData.GenericArguments.Add(cNPCTieredQuestData);
        var fNpcTieredQuestData = new FieldDefinition("npcTieredQuestData", FieldAttributes.Public, gDictIntCnNPCTieredQuestData);
        cQuestEventManager.Fields.Add(fNpcTieredQuestData);


        // Add Methods
        //public DialogResponseQuest::.ctor(string _questID) : base(_questID);
        var mDialogResponseQuestCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, voidTypeRef);
        mDialogResponseQuestCtor.Parameters.Add(new ParameterDefinition("_questID", ParameterAttributes.None, stringTypeRef));
        var mDialogResponseQuestCtorProc = mDialogResponseQuestCtor.Body.GetILProcessor();
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ldc_I4_1));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Stfld, cDialogResponseQuest.Fields.First(d => d.Name == "IsValid")));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ldc_I4_M1));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Stfld, cDialogResponseQuest.Fields.First(d => d.Name == "Variation")));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ldarg_0));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ldarg_1));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Call, cDialogResponse.Methods.First(d => d.Name == ".ctor")));
        mDialogResponseQuestCtorProc.Append(Instruction.Create(OpCodes.Ret));
        cDialogResponseQuest.Methods.Add(mDialogResponseQuestCtor);
        
        //public Dictionary<int, List<Quest>> QuestEventManager::GetTieredQuestList(World world, int npcEntityID, int playerEntityID);
        var mGetTieredQuestList = new MethodDefinition("GetTieredQuestList", MethodAttributes.Public, gTieredQuestList);
        mGetTieredQuestList.Parameters.Add(new ParameterDefinition("world", ParameterAttributes.None, module.Types.First(d => d.Name == "World")));
        mGetTieredQuestList.Parameters.Add(new ParameterDefinition("npcEntityID", ParameterAttributes.None, intTypeRef));
        mGetTieredQuestList.Parameters.Add(new ParameterDefinition("playerEntityID", ParameterAttributes.None, intTypeRef));
        var mGetTieredQuestListProc = mGetTieredQuestList.Body.GetILProcessor();
        mGetTieredQuestListProc.Append(Instruction.Create(OpCodes.Ldnull));
        mGetTieredQuestListProc.Append(Instruction.Create(OpCodes.Ret));
        cQuestEventManager.Methods.Add(mGetTieredQuestList);

        // // FIXME: Why throws "MissingMethodException"?
        // var mQuestEventManagerCtor = cQuestEventManager.Methods.First(d => d.Name == ".ctor");
        // var mQuestEventManagerCtorRet = mQuestEventManagerCtor.Body.Instructions.Last(d => d.OpCode == OpCodes.Call).Previous;
        // var mQuestEventManagerCtorProc = mQuestEventManagerCtor.Body.GetILProcessor();
        // mQuestEventManagerCtorProc.InsertBefore(mQuestEventManagerCtorRet, Instruction.Create(OpCodes.Ldarg_0));
        // // HERE: "MissingMethodException: void System.Collections.Generic.Dictionary`2..ctor()"
        // mQuestEventManagerCtorProc.InsertBefore(mQuestEventManagerCtorRet, Instruction.Create(OpCodes.Newobj, module.ImportReference(new MethodReference(".ctor", voidTypeRef, gDictIntCnNPCTieredQuestData))));
        // mQuestEventManagerCtorProc.InsertBefore(mQuestEventManagerCtorRet, Instruction.Create(OpCodes.Stfld, fNpcTieredQuestData));

        return true;
    }
	
    // Helper functions to allow us to access and change variables that are otherwise unavailable.
    private void SetMethodToVirtual(MethodDefinition meth)
    {
        meth.IsVirtual = true;
    }

    private void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    private void SetMethodToPublic(MethodDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;
    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        return true;
    }

}

