﻿using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.CharacterCreation;
using HarmonyLib;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.CharacterCreation.Patches
{
    [HarmonyPatch]
    public class MainMenuOptionPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Module), "GetInitialStateOptions")]
        public static void MainMenuSkipStoryMode(ref IEnumerable<InitialStateOption> __result)
        {
            List<InitialStateOption> newlist = new List<InitialStateOption>();
            newlist = __result.Where(x => x.Id != "StoryModeNewGame" && x.Id != "SandBoxNewGame").ToList();
            var torOption = new InitialStateOption("DADGNewGame", new TextObject("Start Dell'arte della Guerra"), 3, OnClick, IsDisabledAndReason);
            newlist.Add(torOption);
            newlist.Sort((x, y) => x.OrderIndex.CompareTo(y.OrderIndex));
            __result = newlist;
        }

        private static void OnClick()
        {
            MBGameManager.StartNewGame(new DadgGameManager());
        }

        private static (bool, TextObject) IsDisabledAndReason()
        {
            TextObject coreContentDisabledReason = new TextObject("{=V8BXjyYq}Disabled during installation.", null);
            return new ValueTuple<bool, TextObject>(Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason);
        }
    }
}