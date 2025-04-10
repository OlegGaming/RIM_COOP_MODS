﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using VSE.Passions;

namespace VSE;

public static class InsaneSkillsPatches
{
    private static bool enableSkillLoss;
    private static MethodInfo targetMethod;
    private static MethodInfo patchMethod;

    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_GetSkillDescription"), "Prefix"),
            transpiler: new HarmonyMethod(typeof(PassionPatches), nameof(PassionPatches.SkillDescription_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_GetSkillDescription"), "Prefix"),
            transpiler: new HarmonyMethod(typeof(InsaneSkillsPatches), nameof(DucksSkillDescription_Transpiler)) { priority = Priority.Low });
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_Learn"), "Prefix"),
            transpiler: new HarmonyMethod(typeof(PassionPatches), nameof(PassionPatches.Learn_Transpiler)));
        harm.Unpatch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor)),
            AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_LearnRateFactor"), "Prefix"));
        targetMethod = AccessTools.Method(typeof(SkillRecord), "Interval");
        patchMethod = AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_Interval"), "Prefix");
    }

    public static void UpdateSkillLoss(bool enable, Harmony harm)
    {
        if (enable == enableSkillLoss) return;
        if (enable)
            harm.Unpatch(targetMethod, patchMethod);
        else
            harm.Patch(targetMethod, new HarmonyMethod(patchMethod));

        enableSkillLoss = enable;
    }

    public static IEnumerable<CodeInstruction> DucksSkillDescription_Transpiler(IEnumerable<CodeInstruction> instructions) =>
        PassionPatches.SwitchReplacer(instructions, load => new[]
        {
            load,
            CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.PassionToDef)),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PassionDef), nameof(PassionDef.learnRateFactor))),
            new CodeInstruction(OpCodes.Stloc_3)
        });
}