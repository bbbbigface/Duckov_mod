using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine;
using ItemStatsSystem;
using System.Reflection;
using Duckov;
using FMOD.Studio;
using System.IO;
using ECM2.Examples.SlopeSpeedModifier;

namespace GrenadeCooking.extensions
{
    // Extension to original InputManager class
    // Add takePin field
    public static class InputManager_GrenadePin
    {
        // Generate a dummy grenade and start count down on itself
        public static void takeOutGrenadePin(this InputManager inputManager)
        {
            if (!InputManager.InputActived)
                return;
            if (!inputManager.characterMainControl)
                return;
            if (inputManager.characterMainControl.skillAction == null)
                return;
            // is there is already an existing dummy grenade, skip
            if (inputManager.characterMainControl.Get_dummyGrenade() != null)
                return;
            // skip if the aiming progress is done
            if (inputManager.characterMainControl.skillAction.GetProgress().inProgress)
                return;

            SkillBase currentSkill = inputManager.characterMainControl.skillAction.CurrentRunningSkill;
            if (currentSkill == null)
                return;

            if(currentSkill is Skill_Grenade grenadeSkill)
            {
                // skip landmine
                if (grenadeSkill.isLandmine)
                    return;

                CharacterMainControl fromCharacter = inputManager.characterMainControl;     // this grenade is from player

                FieldInfo field = typeof(Skill_Grenade).GetField("skillContext", BindingFlags.NonPublic | BindingFlags.Instance);
                SkillContext skillContext = (SkillContext)field.GetValue(grenadeSkill);

                Vector3 position = fromCharacter.CurrentUsingAimSocket.position;
                // Instantiate a grenade in hand socket, and set it to dummy
                Grenade grenade = UnityEngine.Object.Instantiate<Grenade>(grenadeSkill.grenadePfb, position, fromCharacter.CurrentUsingAimSocket.rotation);
                grenadeSkill.damageInfo.fromCharacter = fromCharacter;
                grenade.damageInfo = grenadeSkill.damageInfo;
                grenade.createExplosion = grenadeSkill.createExplosion;
                grenade.explosionShakeStrength = grenadeSkill.explosionShakeStrength;
                grenade.damageRange = skillContext.effectRange;
                grenade.delayFromCollide = false;                               // disable delayFromCollide for cooking a grenade
                grenade.delayTime = grenadeSkill.delay;
                grenade.isLandmine = false;
                if (grenadeSkill.fromItem != null)
                {
                    grenade.SetWeaponIdInfo(grenadeSkill.fromItem.TypeID);
                }

                // make fuse line longer for normal grenade
                if (grenadeSkill.fromItem.TypeID == 67)
                    grenade.delayTime = grenade.delayTime + 1.0f;

                // set grenade self hurting field
                typeof(Grenade).GetField("canHurtSelf", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grenade, grenadeSkill.canHurtSelf);
                grenade.Set_isDummy(true);
                grenade.gameObject.SetActive(false);
                // Disable the monobehaviour and physics instead, we need the visual for detonate indicator
                /*grenade.enabled = false;
                FieldInfo rbField = typeof(Grenade).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
                Rigidbody rb = rbField.GetValue(grenade) as Rigidbody;
                rb.isKinematic = true;
                rb.detectCollisions = false;*/

                // Store the dummy grenade reference in main character's extened field
                fromCharacter.Set_dummyGrenade(grenade);

                // play sound
                string soundPath = Path.Combine(AssetHelper.assetFolderPath, "GrenadeTakePin.mp3");
                if (grenadeSkill.fromItem.TypeID == 23 || grenadeSkill.fromItem.TypeID == 24)
                    soundPath = Path.Combine(AssetHelper.assetFolderPath, "SetFuse.mp3");
                EventInstance? ev = (EventInstance?)typeof(AudioManager).GetMethod("MPostCustomSFX", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(AudioManager.Instance, new object[] { soundPath, fromCharacter.gameObject, false });

                // change the dummy grenade material
                /*Shader holoShader = Shader.Find("OverlayTransparent");
                Material holoMat = new Material(holoShader);
                // Set tint color (this shader uses "_Tint")
                holoMat.SetColor("_Tint", new Color(1.0f, 0.3f, 0.3f, 0.6f)); // semi-transparent blue
                // Optional: if the shader supports transparency via alpha blending
                holoMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                holoMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                holoMat.SetInt("_ZWrite", 0);
                holoMat.EnableKeyword("_ALPHABLEND_ON");
                holoMat.renderQueue = 3000;

                Color emission = new Color(1.0f, 0.3f, 0.3f) * 10.0f; // brighter multiplier makes it "pop"
                holoMat.SetColor("_EmissionColor", emission);

                Renderer[] renderers = grenade.GetComponentsInChildren<Renderer>();
                // record original materials
                RendererHelper.RecordOriginal(renderers);
                foreach (Renderer r in renderers)
                {
                    r.material = holoMat;

                }*/

                /*for (int i = 0; i < holoShader.GetPropertyCount(); i++)
                {
                    Debug.Log($"Property {i}: {holoShader.GetPropertyName(i)} ({holoShader.GetPropertyType(i)})");
                }*/
                /*Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();
                foreach (Shader s in shaders)
                {
                    UnityEngine.Debug.Log($"Shader found: {s.name}");
                }*/
                /*UnityEngine.Debug.Log(Shader.Find("OverlayTransparent") != null);
                UnityEngine.Debug.Log(Shader.Find("OverlayTransparent") != null);
                UnityEngine.Debug.Log(Shader.Find("OverlayTransparent") != null);*/

                // instantiate grenadeCookingHUD and activate, initialize
                if(HUDHelper.grenadeCookingHUD == null)
                {
                    // Find the original ActionProgressHUD in the scene (even if inactive)
                    ActionProgressHUD originalHUD = GameObject.FindObjectOfType<ActionProgressHUD>(true);
                    if (originalHUD == null)
                    {
                        Debug.LogError("No ActionProgressHUD found in the scene!");
                        return;
                    }

                    // Instantiate a copy and parent it to the *same parent* as the original
                    HUDHelper.grenadeCookingHUDGO = GameObject.Instantiate(
                        originalHUD.gameObject,
                        originalHUD.transform.parent
                    );

                    HUDHelper.grenadeCookingHUD = HUDHelper.grenadeCookingHUDGO.GetComponent<ActionProgressHUD>();
                    HUDHelper.grenadeCookingHUDGO.name = "GrenadeCookingHUD";

                    // Reset local transform so it appears in exactly the same position/scale
                    HUDHelper.grenadeCookingHUDGO.transform.localPosition = originalHUD.transform.localPosition;
                    HUDHelper.grenadeCookingHUDGO.transform.localRotation = originalHUD.transform.localRotation;
                    HUDHelper.grenadeCookingHUDGO.transform.localScale = originalHUD.transform.localScale;

                    // Hide cancel indicator, reset fill
                    if (HUDHelper.grenadeCookingHUD.stopIndicator != null)
                        HUDHelper.grenadeCookingHUD.stopIndicator.SetActive(false);

                    if (HUDHelper.grenadeCookingHUD.fillImage != null)
                        HUDHelper.grenadeCookingHUD.fillImage.fillAmount = 0f;

                    // Make sure it’s visible
                    HUDHelper.grenadeCookingHUD.SliderCanvasGroup.alpha = 1f;
                }
                HUDHelper.grenadeCookingHUD.gameObject.SetActive(true);


            }

        }

    }
}
