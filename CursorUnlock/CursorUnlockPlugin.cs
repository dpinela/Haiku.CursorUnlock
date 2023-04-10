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
            IL.GameManager.OnApplicationFocus += NoCursorManipulation;
            var m = typeof(MainMenuManager)
                        .GetMethod("DisableMouseAndSelectButton", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic)
                        .GetStateMachineTarget();
            new MMDetour.ILHook(m, NoCursorManipulation);
            IL.SkipToScene.OnEnable += NoCursorManipulation;
            IL.SkipToScene.OnDisable += NoCursorManipulation;
            IL.SkipToScene.OnDestroy += NoCursorManipulation;
            
            // By the time we have the chance to hook it, the main menu code has already
            // locked the cursor. Rectify that.
            UE.Cursor.visible = true;
            UE.Cursor.lockState = UE.CursorLockMode.None;
        }

        private void NoCursorManipulation(Cil.ILContext il)
        {
            // The cursor lock is accomplished by setting Cursor.visible to `false`
            // and Cursor.lockState to CursorLockMode.Locked.
            // To prevent this, we override the arguments to the setters of those properties
            // to be always `true` and CursorLockMode.None, respectively.
            var c = new Cil.ILCursor(il);

            while (c.TryGotoNext(
                Cil.MoveType.AfterLabel,
                i => i.MatchCall(typeof(UE.Cursor), "set_visible")))
            {
                c.Emit(MonoCil.OpCodes.Pop);
                c.Emit(MonoCil.OpCodes.Ldc_I4, 1);
                c.GotoNext(
                    Cil.MoveType.After,
                    i => i.MatchCall(typeof(UE.Cursor), "set_visible"));
            }

            c.Goto(0, Cil.MoveType.Before);

            while (c.TryGotoNext(
                Cil.MoveType.AfterLabel,
                i => i.MatchCall(typeof(UE.Cursor), "set_lockState")))
            {
                c.Emit(MonoCil.OpCodes.Pop);
                c.Emit(MonoCil.OpCodes.Ldc_I4, (int)UE.CursorLockMode.None);
                c.GotoNext(
                    Cil.MoveType.After,
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
    }
}