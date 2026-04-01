using System.Reflection;
using Duckov.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;
using System.Linq;
using UnityEngine.Pool;
using UnityEngine.ProBuilder;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace TestMod
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // keep a dictionary from projectile instance to [CharacterMainControl] targetCharacter
        // bc it seems hard to implement in a polymorphism way
        private Dictionary<Projectile, CharacterMainControl> projectileTargetDic;

        public float _searchRadius = 3f;

        private List<RaycastHit> _searchHits;

        private DamageReceiver _tmpDmgReceiver;

        private CharacterMainControl _tmpTargetCharacter;

        void Awake()
        {
            projectileTargetDic = new Dictionary<Projectile, CharacterMainControl>();
        }

        void OnDestroy()
        {
            projectileTargetDic.Clear();
            projectileTargetDic = null;
        }

        // main idea: in update()
        // go through every projectile, and if a projectile is from RPG, modify it's direction
        private void Update()
        {
            // cleanup previous projectile that is no longer in bulletPool, or target that is dead or released
            cleanupInvalidPair();

            if (LevelManager.Instance != null)
            {
                // Get bulletPool
                BulletPool bulletPool = LevelManager.Instance.BulletPool;

                // Note: the Keys are actually prefabs instead of real instances, and the prefab's context is not initialized...?

                // We need to access all the actual projectile instances in the object pool
                foreach (var kvp in bulletPool.pools)
                {
                    Projectile prefab = kvp.Key;
                    ObjectPool<Projectile> pool = kvp.Value;

                    // Debug.Log($"--- Pool for prefab: {prefab.name} ---");

                    // ObjectPool<T> has a private List<T> called 'm_List'
                    FieldInfo listField = typeof(ObjectPool<Projectile>)
                                           .GetField("m_List", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (listField != null)
                    {
                        List<Projectile> list = listField.GetValue(pool) as List<Projectile>;
                        if (list != null)
                        {
                            foreach (Projectile projectile in list)
                            {
                                // TODO: dont search if projectile is not active or dead
                                /*bool isDead = (bool)typeof(Projectile).GetField("dead", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(projectile);
                                if (!projectile.gameObject.activeInHierarchy)
                                    continue;*/
                                // now projectile is actual bullet instance
                                Debug.Log("projectile ID: " + projectile.context.fromWeaponItemID);
                                // RPG ID == 327
                                if (projectile.context.fromWeaponItemID != 327)
                                    continue;
                                // search target if not having one
                                if (!projectileTargetDic.ContainsKey(projectile))
                                {
                                    Debug.Log("Searching new target...");
                                    Vector3 currentPosition = projectile.transform.position;
                                    LayerMask hitLayers = (LayerMask)typeof(Projectile).GetField("hitLayers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(projectile);
                                    this._searchHits = Physics.SphereCastAll(
                                        currentPosition, this._searchRadius, new Vector3(1, 0, 0), 0f,
                                        hitLayers,
                                        QueryTriggerInteraction.Ignore).ToList<RaycastHit>();
                                    //Debug.Log("Search hit...");
                                    int count = this._searchHits.Count;
                                    Debug.Log("Hits count: " + count);
                                    if (count > 0)
                                    {
                                        this._searchHits.Sort(delegate (RaycastHit a, RaycastHit b)
                                        {
                                            if (a.distance > b.distance)
                                            {
                                                return 1;
                                            }
                                            return 0;
                                        });
                                        Debug.Log("Search sort...");
                                        for (int i = 0; i < count; i++)
                                        {
                                            _tmpDmgReceiver = this._searchHits[i].collider.GetComponent<DamageReceiver>();
                                            // skip if the search hit is not a character
                                            if (_tmpDmgReceiver == null)
                                                continue;
                                            if (_tmpDmgReceiver.health == null)
                                                continue;
                                            if (_tmpDmgReceiver.health.TryGetCharacter() == null)
                                                continue;
                                            // skip if the search target is from the same team
                                            if (_tmpDmgReceiver.Team == projectile.context.team)
                                                continue;
                                            Debug.Log("4");
                                            _tmpTargetCharacter = _tmpDmgReceiver.health.TryGetCharacter();
                                            projectileTargetDic.Add(projectile, _tmpTargetCharacter);
                                            Debug.Log("6");
                                            break;
                                        }
                                        Debug.Log("Search sort end...");
                                    }
                                    //Debug.Log("Search end...");
                                }
                            }
                        }
                    }
                }


                // modify projectile velocity (direction will be updated within projectile update function)
                foreach (var kv in projectileTargetDic)
                {
                    Projectile bullet = kv.Key;
                    CharacterMainControl target = kv.Value;
                    Vector3 aimTarget = target.transform.position + target.Velocity * 0.1f;
                    Vector3 bulletVelocity = (Vector3)typeof(Projectile).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bullet);
                    Vector3 targetBulletVelocity = (aimTarget - bullet.transform.position).normalized * bulletVelocity.magnitude;

                    // interpolate
                    float theta = 2f;
                    Vector3 newBulletVelocity = Vector3.RotateTowards(bulletVelocity, targetBulletVelocity, Mathf.Deg2Rad * theta, 0f);

                    typeof(Projectile).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bullet, newBulletVelocity);

                }

            }

        }

        private void cleanupInvalidPair()
        {
            if (projectileTargetDic == null)
                return;

            if (projectileTargetDic.Count == 0)
                return;

            var toRemove = new List<Projectile>();

            foreach (var kv in projectileTargetDic)
            {
                if (
                    kv.Key == null || !kv.Key.gameObject.activeInHierarchy || !kv.Key.isActiveAndEnabled || 
                    kv.Value == null || !kv.Value.gameObject.activeInHierarchy || kv.Value.Health.CurrentHealth <= 0 || !kv.Value.isActiveAndEnabled)
                    toRemove.Add(kv.Key);
            }

            foreach (var proj in toRemove)
                projectileTargetDic.Remove(proj);
        }

        // keep a dictionary from projectile instance to [CharacterMainControl] targetCharacter
        // bc it seems hard to implement in a polymorphism way
        public class HomingProjectile : Projectile
        {
            public CharacterMainControl targetCharacter;

            public float searchRadius = 3f;

            private List<RaycastHit> _searchHits;

            private DamageReceiver _tmpDmgReceiver;

            private void searchNewTarget()
            {
                // If there's already a target, dont find a new one
                if (this.targetCharacter)
                    return;
                Vector3 currentPosition = this.transform.position;
                this._searchHits = Physics.SphereCastAll(
                    currentPosition, this.searchRadius, new Vector3(0,0,0), 0f,
                    (int) typeof(Projectile).GetField("hitLayers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this), 
                    QueryTriggerInteraction.Ignore).ToList<RaycastHit>();
                int count = this._searchHits.Count;
                if (count > 0)
                {
                    this._searchHits.Sort(delegate (RaycastHit a, RaycastHit b)
                    {
                        if (a.distance > b.distance)
                        {
                            return 1;
                        }
                        return 0;
                    });

                    for (int i = 0; i < count; i++)
                    {
                        _tmpDmgReceiver = this._searchHits[i].collider.GetComponent<DamageReceiver>();
                        // skip if the search hit is not a character
                        if (_tmpDmgReceiver.health.TryGetCharacter() == null)
                            continue;
                        // skip if the search target is from the same team
                        if (_tmpDmgReceiver.Team == this.context.team)
                            continue;
                        targetCharacter = _tmpDmgReceiver.health.TryGetCharacter();
                        break;
                    }
                }
                targetCharacter = null;
            }

        }
    }
}
