namespace WhatAreThose;

using HarmonyLib;

[HarmonyPatch(typeof(ObjectDB))]
static class ObjectDBPatch {
  [HarmonyPostfix]
  [HarmonyPatch(nameof(ObjectDB.Awake))]
  static void AwakePostfix(ObjectDB __instance) {
    if (__instance) {
      IconManager.ReplaceIcons(__instance);
    }
  }
}
