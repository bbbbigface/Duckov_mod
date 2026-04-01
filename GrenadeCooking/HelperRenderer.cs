using System.Collections.Generic;
using UnityEngine;

namespace GrenadeCooking
{
    public static class RendererHelper
    {
        // Stores original materials per renderer
        public static Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

        // Call this before you overwrite materials
        public static void RecordOriginal(Renderer[] renderers)
        {
            foreach (Renderer r in renderers)
            {
                if (!originalMaterials.ContainsKey(r))
                {
                    // Clone the array so we don’t get reference issues
                    originalMaterials[r] = r.sharedMaterials;
                }
            }
        }

        // Call this to restore original materials
        public static void RestoreOriginal(Renderer[] renderers)
        {
            foreach (Renderer r in renderers)
            {
                if (originalMaterials.TryGetValue(r, out Material[] mats))
                {
                    r.materials = mats;
                    originalMaterials.Remove(r);
                }
            }
        }
    }
}
