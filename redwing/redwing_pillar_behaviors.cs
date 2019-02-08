using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Modding.Logger;

namespace redwing
{
    public class redwing_pillar_behavior : MonoBehaviour
    {
        public static int damagePriBase;
        public static int damagePriNail;
        public static int damageSecBase;
        public static int damageSecNail;
        public static int damageSecondaryTimes;


        private int cachedPrimaryDmg;
        private int cachedSecondaryDmg;

        public List<Collider2D> enteredColliders;
        public float lifespan = 1.0f;


        private void Start()
        {
            enteredColliders = new List<Collider2D>();

            cachedPrimaryDmg = damagePriBase + damagePriNail * PlayerData.instance.GetInt("nailSmithUpgrades");
            cachedSecondaryDmg = damageSecBase + damageSecNail * PlayerData.instance.GetInt("nailSmithUpgrades");

            StartCoroutine(fadeOut());
            StartCoroutine(destroyPillar());
        }

        private IEnumerator fadeOut()
        {
            var life = 0f;
            var cachedSprite = gameObject.GetComponent<SpriteRenderer>();
            var cachedColor = cachedSprite.color;

            while (life < lifespan)
            {
                life += Time.deltaTime;
                cachedColor.a = (0.1f + lifespan - life) / lifespan;
                cachedSprite.color = cachedColor;
                yield return null;
            }

            cachedColor.a = 0f;
            cachedSprite.color = cachedColor;
        }

        private IEnumerator destroyPillar()
        {
            var life = 0f;
            var secondaryAttacks = 0;
            yield return null;
            primaryDamage();

            while (secondaryAttacks < damageSecondaryTimes)
            {
                yield return new WaitForSeconds(lifespan / damageSecondaryTimes);
                secondaryDamage();
                secondaryAttacks++;
            }

            Destroy(gameObject);
        }


        private void primaryDamage()
        {
            foreach (var collider in enteredColliders.ToList())
                redwing_game_objects.applyHitInstance(collider.gameObject, cachedPrimaryDmg, gameObject, 0.7f);
        }

        private void secondaryDamage()
        {
            for (var index = enteredColliders.Count - 1; index >= 0; index--)
            {
                var enteredCollider = enteredColliders[index];
                if (enteredCollider == null || !enteredCollider.isActiveAndEnabled)
                    enteredColliders.RemoveAt(index);
            }

            foreach (var collider in enteredColliders.ToList())
                redwing_game_objects.applyHitInstance(collider.gameObject, cachedSecondaryDmg, gameObject, 0f);
        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            var layer = collision.gameObject.layer;
            if (layer != 11) return;

            if (!enteredColliders.Contains(collision)) enteredColliders.Add(collision);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var layer = other.gameObject.layer;
            if (layer != 11) return;

            try
            {
                enteredColliders.Remove(other);
            }
            catch (Exception e)
            {
                log("failed to remove collider. error " + e);
            }
        }

        private static void log(string str)
        {
            Logger.Log("[Redwing] " + str);
        }
    }


    public class redwing_pillar_detect_behavior : MonoBehaviour
    {
        public static Texture2D[] pillarTextures;

        public List<GameObject> enemyList;
        public GameObject firePillar;

        private void Start()
        {
            enemyList = new List<GameObject>();
        }


        // ReSharper disable once UnusedMember.Global - Used implicitly
        public void spawnFirePillar()
        {
            firePillar = new GameObject("redwingFlamePillar", typeof(redwing_pillar_behavior),
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D));
            firePillar.transform.localScale = new Vector3(1f, 1f, 1f);
            GameObject fireAtJerk = null;

            if (isEnemyInRange())
                try
                {
                    fireAtJerk = firePillarTarget();
                }
                catch (Exception e)
                {
                    log("spawn fire pillar failed with error " + e);
                }

            if (fireAtJerk != null)
            {
                firePillar.transform.parent = null;
                firePillar.transform.localPosition = Vector3.zero;
                var pillarRelativePosition = new Vector3(
                    fireAtJerk.gameObject.transform.position.x,
                    gameObject.transform.position.y,
                    fireAtJerk.gameObject.transform.position.z);
                firePillar.transform.position = pillarRelativePosition;
            }
            else
            {
                firePillar.transform.parent = gameObject.transform;
                firePillar.transform.localPosition = Vector3.zero;
            }


            var randomTextureToUse = redwing_flame_gen.rng.Next(0, pillarTextures.Length);
            var img = firePillar.GetComponent<SpriteRenderer>();
            var pillarSpriteRect = new Rect(0, 0,
                redwing_flame_gen.FPTEXTURE_WIDTH, redwing_flame_gen.FPTEXTURE_HEIGHT);
            img.sprite = Sprite.Create(pillarTextures[randomTextureToUse], pillarSpriteRect,
                new Vector2(0.5f, 0.5f), 30f);
            img.color = Color.white;

            var fakePhysics = firePillar.GetComponent<Rigidbody2D>();
            fakePhysics.isKinematic = true;
            var hitEnemies = firePillar.GetComponent<BoxCollider2D>();
            hitEnemies.isTrigger = true;
            hitEnemies.size = img.size;
            hitEnemies.offset = new Vector2(0, 0);

            firePillar.SetActive(true);
        }

        // From grimmchild upgrades and: Token: 0x0600006E RID: 110 RVA: 0x00005168 File Offset: 0x00003368
        private GameObject firePillarTarget()
        {
            GameObject result = null;
            var num = 99999f;
            if (enemyList.Count <= 0) return null;

            for (var i = enemyList.Count - 1; i > -1; i--)
                if (enemyList[i] == null || !enemyList[i].activeSelf)
                    enemyList.RemoveAt(i);
            foreach (var enemyGameObject in enemyList)
            {
                // just pick enemy in range
                if (enemyGameObject == null) continue;
                var sqrMagnitude = (gameObject.transform.position - enemyGameObject.transform.position).sqrMagnitude;
                if (!(sqrMagnitude < num)) continue;
                result = enemyGameObject;
                num = sqrMagnitude;
            }

            return result;
        }

        private void OnTriggerEnter2D(Collider2D otherCollider)
        {
            if (otherCollider.gameObject.layer != 11) return;
            enemyList.Add(otherCollider.gameObject);
        }

        private void OnTriggerExit2D(Collider2D otherCollider)
        {
            if (otherCollider.gameObject.layer != 11) return;
            enemyList.Remove(otherCollider.gameObject);
        }

        private bool isEnemyInRange()
        {
            return enemyList.Count != 0;
        }

        private static void log(string str)
        {
            Logger.Log("[Redwing] " + str);
        }
    }
}