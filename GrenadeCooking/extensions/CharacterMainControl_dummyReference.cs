using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

namespace GrenadeCooking.extensions
{
    // Extension to CharacterMainControl to hold a Grenade reference to the dummy grenade
    public static class CharacterMainControl_dummyReference
    {
        private class CharacterMainControl_ext
        {
            public Grenade dummyGrenade = null;
        }

        // Weak table ensures automatic cleanup
        private static readonly ConditionalWeakTable<CharacterMainControl, CharacterMainControl_ext> _data = new ConditionalWeakTable<CharacterMainControl, CharacterMainControl_ext>();

        private static CharacterMainControl_ext Get(CharacterMainControl c)
            => _data.GetOrCreateValue(c);

        // ====== Simulated Fields ======
        public static Grenade Get_dummyGrenade(this CharacterMainControl c)
            => Get(c).dummyGrenade;
        public static void Set_dummyGrenade(this CharacterMainControl c, Grenade dummyGrenade)
            => Get(c).dummyGrenade = dummyGrenade;

    }

    // Postfix Update(), to add dummyGrenade update if dummyGrenade is not null
    [HarmonyPatch(typeof(CharacterMainControl), "Update")]
    public static class Patch_CharacterMainControl_DummyGrenadeUpdate
    {
        [HarmonyPostfix]
        public static void Postfix(CharacterMainControl __instance)
        {
            if (__instance.Get_dummyGrenade() == null)
                return;

            __instance.Get_dummyGrenade().dummyUpdate();

        }
    }


}
