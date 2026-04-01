using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrenadeCooking.extensions
{
    // Extension to original CharacterInputControl class
    // Add keyboard mapping to take out grenade pin
    public static class CharacterInputControl_GrenadePin
    {
        // Internal class holding all extra data
        private class KeyAction_GrenadePin
        {
            public InputAction takePinAction;
        }

        // Weak table ensures automatic cleanup when CharacterInputControl is destroyed
        private static readonly ConditionalWeakTable<CharacterInputControl, KeyAction_GrenadePin> _data = new ConditionalWeakTable<CharacterInputControl, KeyAction_GrenadePin>();

        private static KeyAction_GrenadePin Get(CharacterInputControl c)
            => _data.GetOrCreateValue(c);

        // ====== Simulated Fields ======
        public static InputAction GetTakePinAction(this CharacterInputControl c)
            => Get(c).takePinAction;
        public static void SetTakePinAction(this CharacterInputControl c, InputAction a)
            => Get(c).takePinAction = a;

        // handler function when taking the pin
        public static void onTakePinAction(this CharacterInputControl characterInputControl, InputAction.CallbackContext context)
        {
            if (GameManager.Paused)
            {
                return;
            }
            if (context.started)
            {
                Debug.Log("Started takePin");
                //characterInputControl.inputManager.Set_takePin(true);
                characterInputControl.inputManager.takeOutGrenadePin();
            }
            // TODO: maybe change this to "Toggle"? put the pin back...
        }

    }

    [HarmonyPatch(typeof(CharacterInputControl), "RegisterEvents")]
    public static class Patch_CharacterInputControl_AddPinBind
    {
        [HarmonyPostfix]
        public static void Postfix(CharacterInputControl __instance)
        {
            Type type = __instance.GetType();

            InputAction takePinAction = new InputAction(
                "TakeGrenadePin",
                InputActionType.Button,
                "<Keyboard>/g"
            );

            // Store the action in your simulated field
            __instance.SetTakePinAction(takePinAction);

            MethodInfo bindMethod = AccessTools.Method(type, "Bind");
            
            // Invoke Bind() via reflection
            bindMethod.Invoke(__instance, new object[] { takePinAction, new Action<InputAction.CallbackContext>(__instance.onTakePinAction) });
            takePinAction.Enable();

            // Get private unbindCommands queue
            FieldInfo unbindQueueField = AccessTools.Field(type, "unbindCommands");
            if (unbindQueueField != null)
            {
                var queue = unbindQueueField.GetValue(__instance) as Queue;
                if (queue != null)
                {
                    queue.Enqueue((Action)(() =>
                    {
                        takePinAction.Disable();
                        takePinAction.Dispose();
                    }));
                }
            }
            else
            {
                Debug.LogWarning("[GrenadeCooking] Could not find private unbindCommands field!");
            }
            
        }
    }



}
