using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace GrenadeCooking.extensions
{
    // Intercept OnRelease() if there's already a dummyGrenade (means a pin has been pulled)
    [HarmonyPatch(typeof(Skill_Grenade))]
    public static class Patch_Skill_Grenade_OnRelease
    {
        // Prefix inject before OnRelease(), to let the character release the dummy grenade
        // This is the case the grenade exploded in hand....
        [HarmonyPatch("OnRelease")]
        [HarmonyPrefix]
        public static bool Prefix_OnRelease(Skill_Grenade __instance)
        {
            CharacterMainControl fromCharacter = (CharacterMainControl)typeof(Skill_Grenade).GetField("fromCharacter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (fromCharacter == null)
                return true;
            Grenade dummyGrenade = fromCharacter.Get_dummyGrenade();
            // run original OnRelease if no dummy grenade
            if (dummyGrenade == null)
                return true;
            // should not happen, but in case
            if (dummyGrenade.Get_isDummy() == false)
            {
                dummyGrenade.Set_isDummy(false);
                dummyGrenade.gameObject.SetActive(true);
                fromCharacter.Set_dummyGrenade(null);
                return true;
            }
            // now the character has a dummyGrenade to be released
            // activate the dummy grenade and unhook it from original character, now it's a normal grenade
            dummyGrenade.Set_isDummy(false);
            dummyGrenade.gameObject.SetActive(true);
            /*dummyGrenade.enabled = true;
            FieldInfo rbField = typeof(Grenade).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
            Rigidbody rb = rbField.GetValue(dummyGrenade) as Rigidbody;
            rb.isKinematic = false;
            rb.detectCollisions = true;*/
            fromCharacter.Set_dummyGrenade(null);

            // set dummy grenade material/shader back to normal
            /*Renderer[] renderers = dummyGrenade.GetComponentsInChildren<Renderer>();
            RendererHelper.RestoreOriginal(renderers);*/

            FieldInfo field = typeof(Skill_Grenade).GetField("skillContext", BindingFlags.NonPublic | BindingFlags.Instance);
            SkillContext skillContext = (SkillContext)field.GetValue(__instance);

            field = typeof(Skill_Grenade).GetField("skillReleaseContext", BindingFlags.NonPublic | BindingFlags.Instance);
            SkillReleaseContext skillReleaseContext = (SkillReleaseContext)field.GetValue(__instance);

            Vector3 releasePoint = skillReleaseContext.releasePoint;
            float y = releasePoint.y;
            Vector3 point = releasePoint - fromCharacter.transform.position;
            point.y = 0f;
            float num = point.magnitude;
            if (!__instance.canControlCastDistance)
            {
                num = skillContext.castRange;
            }
            point.Normalize();

            // Launch the grenade
            Vector3 position = fromCharacter.CurrentUsingAimSocket.position;
            Vector3 target = position + (Quaternion.Euler(0f, -__instance.blastAngle * 0.5f, 0f) * point) * num;
            target.y = releasePoint.y;
            Vector3 velocity = __instance.CalculateVelocity(position, target, skillContext.grenageVerticleSpeed);
            dummyGrenade.Launch(position, velocity, fromCharacter, __instance.canHurtSelf);

            // hide progress bar
            HUDHelper.grenadeCookingHUD.gameObject.SetActive(false);

            return false;
        }
    }

}
