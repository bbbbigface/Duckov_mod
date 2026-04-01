using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

namespace GrenadeCooking
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private void OnEnable()
        {
            HarmonyLoad.Load0Harmony();
        }

        private void Start()
        {
            new Harmony("GrenadeCooking").PatchAll();
            // Load the audios
            AssetHelper.setUpAssetFolderPath();
        }

    }

    public static class AssetHelper
    {
        public static string assetFolderPath = null;

        public static void setUpAssetFolderPath()
        {
            string dllFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            assetFolderPath = Path.Combine(dllFolder, "AssetBundles");
        }
    }

    public static class HUDHelper
    {
        public static ActionProgressHUD grenadeCookingHUD = null;
        public static GameObject grenadeCookingHUDGO = null;

        public static void UpdateGrenadeCookingHUD(float fraction)
        {
            if (grenadeCookingHUD == null)
                return;

            // Clamp between 0–1 just to be safe
            fraction = Mathf.Clamp01(fraction);

            // Update the fill image
            if (grenadeCookingHUD.fillImage != null)
                grenadeCookingHUD.fillImage.fillAmount = fraction;

            // Make sure HUD is visible and alpha is correct
            grenadeCookingHUD.SliderCanvasGroup.alpha = 1f;
        }
    }

}
