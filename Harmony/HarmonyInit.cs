using DMT;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;
using System.Reflection.Emit;


// Harmony Init
public class SelectableQuestTier_Init : IHarmony
{
    public void Start()
    {
        Debug.Log(" Loading Patch: " + this.GetType().ToString());

        // Reduce extra logging stuff
        Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(UnityEngine.LogType.Warning, StackTraceLogType.None);
        
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
