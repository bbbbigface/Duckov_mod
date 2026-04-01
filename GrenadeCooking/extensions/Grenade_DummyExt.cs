using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine;

namespace GrenadeCooking.extensions
{
    // extension to original Grenade class, adding option to be a dummy grenade for cooking implementation
    public static class Grenade_DummyExt
    {
        // Internal class holding all extra data
        private class Grenade_ext
        {
            public bool isDummy = false;
        }

        // Weak table ensures automatic cleanup
        private static readonly ConditionalWeakTable<Grenade, Grenade_ext> _data = new ConditionalWeakTable<Grenade, Grenade_ext>();

        private static Grenade_ext Get(Grenade g)
            => _data.GetOrCreateValue(g);

        // ====== Simulated Fields ======
        public static bool Get_isDummy(this Grenade g)
            => Get(g).isDummy;
        public static void Set_isDummy(this Grenade g, bool isDummy)
            => Get(g).isDummy = isDummy;

        // Update function for a dummy grenade, manually called each frame
        public static void dummyUpdate(this Grenade grenade)
        {
            if (grenade.Get_isDummy() == false)
                return;
            // stick the transform to character hand, this is just for creating explosion if exploded in hand
            grenade.transform.position = grenade.damageInfo.fromCharacter.CurrentUsingAimSocket.transform.position;
            // call regular Update(), to update timer
            typeof(Grenade).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grenade, null);

            // update grenadeCookingHUD
            float delayTimer = (float)typeof(Grenade).GetField("delayTimer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(grenade);

            HUDHelper.UpdateGrenadeCookingHUD(delayTimer/grenade.delayTime);

            // Update dummyRangeHUD
            /*SkillHud3D skillHud3D = UnityEngine.Object.FindObjectOfType<SkillHud3D>();
            if (skillHud3D == null)
            {
                return;
            }

            float delayTimer = (float)typeof(Grenade).GetField("delayTimer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(grenade);
            float timeLeft = grenade.delayTime - delayTimer;
            if (timeLeft > 0)
            {
                // Calculate the predicted position of the cooked grenade
                Vector3 start = grenade.damageInfo.fromCharacter.CurrentUsingAimSocket.transform.position;

                SkillBase currentSkill = (SkillBase)typeof(SkillHud3D).GetField("currentSkill", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skillHud3D);
                float verticalSpeed = currentSkill.SkillContext.grenageVerticleSpeed;

                CharacterMainControl mainCharacter = (CharacterMainControl)typeof(SkillHud3D).GetField("character", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skillHud3D);
                Vector3 currentSkillAimPoint = mainCharacter.GetCurrentSkillAimPoint();

                float g = Physics.gravity.magnitude;
                float t1 = verticalSpeed / g; // time to go up
                float t2 = Mathf.Sqrt(2f * (t1 * verticalSpeed * 0.5f + start.y - currentSkillAimPoint.y) / g);
                float totalTime = t1 + t2; // num3

                Vector3 vector = start;
                vector.y = 0f;
                Vector3 vector2 = currentSkillAimPoint;
                vector2.y = 0f;
                float horizontalSpeed = Vector3.Distance(vector, vector2) / totalTime;

                Vector3 dir = (vector2 - vector).normalized;

                Vector3 position = start + dir * (horizontalSpeed * timeLeft) + Vector3.up * (verticalSpeed * timeLeft - 0.5f * g * timeLeft * timeLeft);

                //Debug.Log("valid position: " + position);
                grenade.transform.position = position;
            }*/

        }

    }

    // Inject dummy grenade logic to original Grenade class
    [HarmonyPatch(typeof(Grenade))]
    public static class Patch_Grenade_DummyLogic
    {
        // Prefix inject before explode(), to let the character release the dummy grenade
        // This is the case the grenade exploded in hand....
        [HarmonyPatch("Explode")]
        [HarmonyPrefix]
        public static void Prefix_Explode(Grenade __instance)
        {
            if (__instance.Get_isDummy() == false)
                return;
            if (__instance.damageInfo.fromCharacter == null)
                return;
            // Later, harmony will intercept Skill_Grenade.OnRelease() and detect there is a dummy grenade, and will launch the dummy grenade instead
            __instance.damageInfo.fromCharacter.ReleaseSkill(SkillTypes.itemSkill);

        }
    }


}
