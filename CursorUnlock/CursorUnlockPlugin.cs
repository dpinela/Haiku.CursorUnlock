global using System;
using Reflection = System.Reflection;
using Bep = BepInEx;
using MMDetour = MonoMod.RuntimeDetour;
using Cil = MonoMod.Cil;
using MonoCil = Mono.Cecil.Cil;
using static MonoMod.Cil.ILPatternMatchingExt;
using static MonoMod.Utils.Extensions;
using UE = UnityEngine;

namespace Haiku.CursorUnlock
{
    [Bep.BepInPlugin("haiku.cursorunlock", "Haiku Cursor Unlock", "1.0.0.0")]
    [Bep.BepInDependency("haiku.mapi", "1.0")]
    public class CursorUnlockPlugin : Bep.BaseUnityPlugin
    {
        public void Start()
        {
            Logger.LogInfo("GameManager.OnApplicationFocus");
            IL.GameManager.OnApplicationFocus += NoCursorManipulation;
            Logger.LogInfo("MainMenuManager.DisableMouseAndSelectButton");
            var m = typeof(MainMenuManager)
                        .GetMethod("DisableMouseAndSelectButton", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic)
                        .GetStateMachineTarget();
            new MMDetour.ILHook(m, NoCursorManipulation);
            Logger.LogInfo("SkipToScene.OnEnable");
            IL.SkipToScene.OnEnable += NoCursorManipulation;
            Logger.LogInfo("SkipToScene.OnDisable");
            IL.SkipToScene.OnDisable += NoCursorManipulation;
            Logger.LogInfo("SkipToScene.OnDestroy");
            IL.SkipToScene.OnDestroy += NoCursorManipulation;
            // debug mode also needs patching
            
            UE.Cursor.visible = true;
            UE.Cursor.lockState = UE.CursorLockMode.None;
        }

        private void NoCursorManipulation(Cil.ILContext il)
        {
            var c = new Cil.ILCursor(il);

            while (c.TryGotoNext(
                Cil.MoveType.AfterLabel,
                i => i.MatchCall(typeof(UE.Cursor), "set_visible")))
            {
                Logger.LogInfo("Paching call to set_visible");
                c.Emit(MonoCil.OpCodes.Pop);
                c.Emit(MonoCil.OpCodes.Ldc_I4, 1);
                c.GotoNext(
                    Cil.MoveType.AfterLabel,
                    i => i.MatchCall(typeof(UE.Cursor), "set_visible"));
            }

            c.Goto(0, Cil.MoveType.Before);

            while (c.TryGotoNext(
                Cil.MoveType.AfterLabel,
                i => i.MatchCall(typeof(UE.Cursor), "set_lockState")))
            {
                Logger.LogInfo("Paching call to set_lockState");
                c.Emit(MonoCil.OpCodes.Pop);
                c.Emit(MonoCil.OpCodes.Ldc_I4, (int)UE.CursorLockMode.None);
                c.GotoNext(
                    Cil.MoveType.AfterLabel,
                    i => i.MatchCall(typeof(UE.Cursor), "set_lockState"));
            }
        }

        private void NoCursorManipulationB(Cil.ILContext il)
        {
            var c = new Cil.ILCursor(il);

            while (c.TryGotoNext(
                Cil.MoveType.AfterLabel,
                i => i.MatchCall(typeof(UE.Cursor), "set_lockState")))
            {
                Logger.LogInfo("Paching call to set_lockState");
                c.Emit(MonoCil.OpCodes.Pop);
                c.Emit(MonoCil.OpCodes.Ldc_I4, (int)UE.CursorLockMode.None);
                c.GotoNext(
                    Cil.MoveType.AfterLabel,
                    i => i.MatchCall(typeof(UE.Cursor), "set_lockState"));
            }
        }

        private void NoRosrucManipulation(Cil.ILContext il)
        {
            var c = new Cil.ILCursor(il);
            while (c.TryGotoNext(
                Cil.MoveType.AfterLabel,
                i => i.MatchLdcI4(out var _),
                i => i.MatchCall(typeof(UE.Cursor), "set_lockState"),
                i => i.MatchLdcI4(out var _),
                i => i.MatchCall(typeof(UE.Cursor), "set_visible")))
            {
                SkipNInstructions(c, 4);
            }
        }

        private void SkipNInstructions(Cil.ILCursor c, int n)
        {
            Logger.LogInfo("Skipping cursor manip");
            var skip = c.DefineLabel();
            c.EmitDelegate((Action)(() => Logger.LogInfo("cursor manip skipped")));
            c.Emit(MonoCil.OpCodes.Br, skip);
            c.Index += n;
            c.MarkLabel(skip);
        }
    }
}