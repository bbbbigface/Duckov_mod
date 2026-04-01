using HarmonyLib;
using HomingRocket.extensions;
using UnityEngine;

namespace HomingRocket
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private void OnEnable()
        {
            HarmonyLoad.Load0Harmony();
        }

        private void Start()
        {
            new Harmony("HomingRocket").PatchAll();
        }


        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Init), new System.Type[] { typeof(ProjectileContext) })]
        public class Patch_Projectile_Init_Add_Homing_Fields
        {
            // Patch the projectile with extra fields when shooting a bullet, AFTER it was originally initiated WITH CONTEXT
            [HarmonyPostfix]
            static void Postfix(Projectile __instance, ProjectileContext _context)
            {
                Debug.Log("[HomingRocket] Postfix patching Projectile.Init(ProjectileContext)");

                // check if the instance is from rocket, skip if not
                // TODO: maybe add specific homing rocket launcher type later
                if (_context.fromWeaponItemID != 327)
                    return;

                __instance.Set_canSearchTarget(true);
                __instance.Set_isHomeable(true);
                __instance.Set_targetCharacter(null);
                __instance.Set_searchRadius(10.0f);
                // TODO: set initial target, for Homing RPG later
                // TODO: set search radius, if needed later

            }
        }

        [HarmonyPatch(typeof(Projectile))]
        public class Patch_Projectile_Homing_Change_Velocity_before_moving
        {
            // Patch the projectile with a modified velocity towards target and search for target, BEFORE projectile moves this frame
            [HarmonyPrefix]
            [HarmonyPatch("UpdateMoveAndCheck", MethodType.Normal)]
            static void Prefix(Projectile __instance)
            {
                __instance.searchNewTarget();
                // Debug.Log("Prefix patching Projectile.UpdateMoveAndCheck()");
                __instance.modifySpeed();
            }
        }


    }
}
