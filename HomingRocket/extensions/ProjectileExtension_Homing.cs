using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.ProBuilder;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HomingRocket.extensions
{
    // Extension to original Projectile class
    // Add homing function
    public static class ProjectileExtension_Homing
    {
        // Internal class holding all extra data
        private class HomingData
        {
            public CharacterMainControl targetCharacter = null;             // the homing target
            public bool isHomeable = false;                                 // if the bullet can adjust direction to the target (可制导)
            public bool canSearchTarget = false;                            // if the bullet can search target itself (可自主寻敌)
            public float searchRadius = 10.0f;                               // search radius (索敌半径)

            public List<RaycastHit> _searchHits_ext;
        }

        // Weak table ensures automatic cleanup when Projectile is destroyed
        private static readonly ConditionalWeakTable<Projectile, HomingData> _data = new ConditionalWeakTable<Projectile, HomingData>();

        // ====== Simulated Fields ======
        public static CharacterMainControl Get_targetCharacter(this Projectile proj)
            => _data.GetOrCreateValue(proj).targetCharacter;

        public static void Set_targetCharacter(this Projectile proj, CharacterMainControl targetCharacter)
            => _data.GetOrCreateValue(proj).targetCharacter = targetCharacter;

        public static bool Get_isHomeable(this Projectile proj)
            => _data.GetOrCreateValue(proj).isHomeable;

        public static void Set_isHomeable(this Projectile proj, bool isHomeable)
            => _data.GetOrCreateValue(proj).isHomeable = isHomeable;

        public static bool Get_canSearchTarget(this Projectile proj)
            => _data.GetOrCreateValue(proj).canSearchTarget;

        public static void Set_canSearchTarget(this Projectile proj, bool canSearchTarget)
            => _data.GetOrCreateValue(proj).canSearchTarget = canSearchTarget;

        public static List<RaycastHit> Get__searchHits_ext(this Projectile proj)
            => _data.GetOrCreateValue(proj)._searchHits_ext;

        public static void Set__searchHits_ext(this Projectile proj, List<RaycastHit> _searchHits_ext)
            => _data.GetOrCreateValue(proj)._searchHits_ext = _searchHits_ext;

        public static float Get_searchRadius(this Projectile proj)
            => _data.GetOrCreateValue(proj).searchRadius;

        public static void Set_searchRadius(this Projectile proj, float searchRadius)
            => _data.GetOrCreateValue(proj).searchRadius = searchRadius;

        // Search a new target
        public static void searchNewTarget(this Projectile proj)
        {
            // skip if the bullet is either not homable or cannot search target
            if (!proj.Get_isHomeable() || !proj.Get_canSearchTarget())
                return;
            // skip if already have a target
            if (proj.Get_targetCharacter() != null)
                return;
            Vector3 currentPosition = proj.transform.position;
            Vector3 previousVelocity = (Vector3)typeof(Projectile).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(proj);
            Vector3 searchOrigin = currentPosition + previousVelocity * 0.05f;       // target searching prioritize somewhere in the front
            LayerMask hitLayers = (LayerMask)typeof(Projectile).GetField("hitLayers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(proj);
            proj.Set__searchHits_ext(
                Physics.SphereCastAll(
                currentPosition, proj.Get_searchRadius(), new Vector3(1, 0, 0), 0f,
                hitLayers,
                QueryTriggerInteraction.Ignore).ToList<RaycastHit>()
            );

            int count = proj.Get__searchHits_ext().Count;
            //Debug.Log("Search Hits Count: " + count);
            if (count > 0)
            {
                proj.Get__searchHits_ext().Sort(delegate (RaycastHit a, RaycastHit b)
                {
                    if (a.distance > b.distance)
                    {
                        return 1;
                    }
                    return 0;
                });
                //Debug.Log("Search sort...");
                for (int i = 0; i < count; i++)
                {
                    DamageReceiver _tmpDmgReceiver = proj.Get__searchHits_ext()[i].collider.GetComponent<DamageReceiver>();
                    // skip if the search hit is not a character
                    if (_tmpDmgReceiver == null)
                        continue;
                    if (_tmpDmgReceiver.health == null)
                        continue;
                    if (_tmpDmgReceiver.health.TryGetCharacter() == null)
                        continue;
                    // skip if the search target is from the same team
                    if (_tmpDmgReceiver.Team == proj.context.team)
                        continue;

                    Collider _targetCollider = proj.Get__searchHits_ext()[i].collider;
                    // Temporarily disable target collider for LOS check
                    _targetCollider.enabled = false;
                    // Do a line of sight check to avoid locking targets behind wall
                    if (Physics.Linecast(currentPosition, _targetCollider.transform.position, hitLayers))
                    {
                        _targetCollider.enabled = true;
                        continue;
                    }
                    _targetCollider.enabled = true;

                    proj.Set_targetCharacter(_tmpDmgReceiver.health.TryGetCharacter());
                    // Debug.Log("6");
                    break;
                }
            }
        }

        // Modify projectile speed slightly towards aiming target
        public static void modifySpeed(this Projectile proj)
        {
            // skip if the bullet is either not homable
            if (!proj.Get_isHomeable())
                return;
            // skip if no homing target locked yet
            if (proj.Get_targetCharacter() == null)
                return;
            Debug.Log("[HomingRocket] Modding rocket velocity");
            // modify the speed, interpolate towards target
            CharacterMainControl target = proj.Get_targetCharacter();
            Vector3 aimTarget = target.transform.position + target.Velocity * 0.1f;         // with a little moving prediction
            Vector3 bulletVelocity = (Vector3)typeof(Projectile).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(proj);
            Vector3 targetBulletVelocity = (aimTarget - proj.transform.position).normalized * bulletVelocity.magnitude;

            // rotate towards target direction
            float theta = 3f;
            Vector3 newBulletVelocity = Vector3.RotateTowards(bulletVelocity, targetBulletVelocity, Mathf.Deg2Rad * theta, 0f);

            typeof(Projectile).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(proj, newBulletVelocity);
        }
    }
}
