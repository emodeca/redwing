using System;
using System.Collections;
using UnityEngine;
using Logger = Modding.Logger;
using Object = UnityEngine.Object;

namespace redwing
{
    internal class redwing_fireball_spawner_behavior : MonoBehaviour
    {
        public readonly float lifespan = 4.0f;

        public GameObject fbSpawn;

        public void Start()
        {
            StartCoroutine(despawn());
        }

        private IEnumerator despawn()
        {
            yield return new WaitForSeconds(lifespan);
            Destroy(fbSpawn);
        }
    }

    internal class redwing_fireball_behavior : MonoBehaviour
    {
        public const float G_FORCE = -1.5f;
        public const float TERMINAL_VELOCITY_Y = 15f;
        public const float CARTOON_FLOAT_TIME = 0.3f;
        private const float MAGMA_FRAMERATE = 15f;

        private const int FIREBALL_WIDTH = 150;
        private const int FIREBALL_HEIGHT = 150;

        public static AudioClip fireballSizzle;
        public static AudioClip fireballImpact;
        public static int fbDamageBase;
        public static int fbDamageScale;
        public static int fbmDamageBase;
        public static int fbmDamageScale;

        public static int fireballMana;
        public readonly float lifespan = 3.25f;

        private readonly GameObject[] ballObjs = new GameObject[4];

        private bool botLeftCollide, botRightCollide, topLeftCollide, topRightCollide;

        public AudioSource cachedAudioPlayer;
        public bool despawnBall;
        public bool doPhysics;


        public GameObject fireball;

        private int fireballDmg;
        public int fireballMagmaFireballHeight;
        public Texture2D[] fireballMagmaFireballs;

        public int fireballMagmaFireballWidth;
        public Texture2D[] fireballMagmas;
        public Rigidbody2D fireballPhysics;
        public SpriteRenderer fireballSprite;

        public BoxCollider2D hitboxForPivot;
        private bool isDoingHitboxStuff;

        public float maxHeight = 8f;
        public float maxySpeed = 30f;

        public bool realisticPhysics;
        public float rotationalVelocity;
        public Vector3 selfPosition;

        public Transform selfTranform;
        private bool stopAppear;

        public float xVelocity;
        public float yVelocity;

        public void Start()
        {
            StartCoroutine(despawn());

            StartCoroutine(realisticPhysics ? semiRealPhysics() : cartoonPhysics());

            StartCoroutine(ballAppear());

            fireballDmg = fbDamageBase + fbDamageScale * PlayerData.instance.GetInt("nailSmithUpgrades");
        }


        private IEnumerator ballAppear()
        {
            var ballSize = transform.localScale;
            var currentTime = 0f;
            while (currentTime < 0.2f && !stopAppear)
            {
                var alpha = currentTime * (float) (1.0 / 0.2);
                currentTime += Time.deltaTime;

                transform.localScale = ballSize * alpha;

                var fireballSpriteColor = fireballSprite.color;
                fireballSpriteColor.a = alpha;
                fireballSprite.color = fireballSpriteColor;

                yield return null;
            }

            if (stopAppear) yield break;
            var spriteColor = fireballSprite.color;
            spriteColor.a = 1.0f;
            fireballSprite.color = spriteColor;
            transform.localScale = ballSize;
        }

        private IEnumerator semiRealPhysics()
        {
            while (!doPhysics) yield return null;

            while (doPhysics)
            {
                var timePassed = Time.deltaTime;

                var actualYForce = 2 * G_FORCE + 8 * G_FORCE *
                                   ((TERMINAL_VELOCITY_Y + yVelocity) / TERMINAL_VELOCITY_Y);
                yVelocity += actualYForce * timePassed;

                selfPosition.x += xVelocity * timePassed;
                selfPosition.y += yVelocity * timePassed;

                selfTranform.position = selfPosition;
                fireball.transform.Rotate(0f, 0f, rotationalVelocity);

                yield return null;
            }
        }

        private IEnumerator cartoonPhysics()
        {
            while (!doPhysics) yield return null;

            var gFall = false;
            var currentTime = 0f;
            var currentHeight = 0f;

            while (doPhysics && currentTime < lifespan)
            {
                var timePassed = Time.deltaTime;

                //float actualYForce = ((G_FORCE) * (TERMINAL_VELOCITY_Y + yVelocity) / TERMINAL_VELOCITY_Y);
                if (gFall)
                {
                    var actualYForce = G_FORCE * (TERMINAL_VELOCITY_Y + yVelocity) / TERMINAL_VELOCITY_Y;
                    yVelocity += actualYForce;
                }
                else
                {
                    yVelocity = (float) (maxySpeed * (1.0 - currentHeight / (1.2 * maxHeight)));
                }


                currentTime += timePassed;

                if (yVelocity > 0f && currentHeight >= maxHeight)
                {
                    yVelocity = 0f;
                    var cartoonStartTime = currentTime;
                    gFall = true;
                    while (currentTime - cartoonStartTime < CARTOON_FLOAT_TIME)
                    {
                        timePassed = Time.deltaTime;
                        selfPosition.x += xVelocity * timePassed;
                        selfTranform.position = selfPosition;
                        fireball.transform.Rotate(0f, 0f, rotationalVelocity);
                        currentTime += timePassed;
                        yield return null;
                    }
                }


                selfPosition.x += xVelocity * timePassed;
                selfPosition.y += yVelocity * timePassed;
                currentHeight += yVelocity * timePassed;

                selfTranform.position = selfPosition;
                fireball.transform.Rotate(0f, 0f, rotationalVelocity);

                yield return null;
            }
        }

        private IEnumerator despawn()
        {
            despawnBall = true;


            yield return new WaitForSeconds(lifespan);

            if (!despawnBall) yield break;
            Destroy(fireball);
            Destroy(gameObject);
        }

        public void OnTriggerEnter2D(Collider2D hitbox)
        {
            var targetLayer = hitbox.gameObject.layer;

            // why? These are hardcoded layers in the Damages Enemy class so they must not be important.
            // Or rather they must be specifically avoided for some reason.
            if (targetLayer == 20 || targetLayer == 9 || targetLayer == 26 || targetLayer == 31) return;

            if (targetLayer == 11)
            {
                redwing_game_objects.applyHitInstance(hitbox.gameObject,
                    fireballDmg, gameObject, 0.1f);

                if (doPhysics)
                {
                    fireballDmg = 0;
                    doPhysics = false;
                    cachedAudioPlayer.clip = fireballImpact;

                    var vol2 = GameManager.instance.gameSettings.masterVolume *
                               GameManager.instance.gameSettings.soundVolume * 0.01f;
                    if (GameManager.instance.gameSettings.masterVolume <= 5) vol2 /= 2f;

                    if (GameManager.instance.gameSettings.soundVolume <= 5) vol2 /= 2f;
                    cachedAudioPlayer.volume = vol2 * 0.3f;
                    cachedAudioPlayer.loop = false;
                    cachedAudioPlayer.Play();
                    ballExplode();
                    HeroController.instance.AddMPChargeSpa(fireballMana);
                }
            }


            if (targetLayer != 8) return;


            cachedAudioPlayer.clip = fireballSizzle;
            var vol = GameManager.instance.gameSettings.masterVolume *
                      GameManager.instance.gameSettings.soundVolume * 0.01f;
            if (GameManager.instance.gameSettings.masterVolume <= 5) vol /= 2f;

            if (GameManager.instance.gameSettings.soundVolume <= 5) vol /= 2f;
            cachedAudioPlayer.volume = vol * 0.12f;
            cachedAudioPlayer.loop = false;
            cachedAudioPlayer.Play();

            doPhysics = false;

            //center of the object. if we above it and within the bounds we hit it from the top
            //Vector2 centerMeme = hitbox.bounds.center;


            const float epsilon = 0.4f;

            Vector2 ourTopRight = hitboxForPivot.bounds.max;
            Vector2 ourBottomLeft = hitboxForPivot.bounds.min;
            //ourTopRight = new Vector2(ourTopRight.x + epsilon, ourTopRight.y + epsilon);
            //ourBottomLeft = new Vector2(ourBottomLeft.x - epsilon, ourBottomLeft.y - epsilon);

            var ourTopLeft = new Vector2(ourBottomLeft.x, ourTopRight.y);
            var ourBottomRight = new Vector2(ourTopRight.x, ourBottomLeft.y);

            Vector2 otherTopRight = hitbox.bounds.max;
            Vector2 otherBotLeft = hitbox.bounds.min;


            var br = getDistanceBetweenVectors(ourBottomRight, hitbox.closestPoint(ourBottomRight));
            var tr = getDistanceBetweenVectors(ourTopRight, hitbox.closestPoint(ourTopRight));
            var tl = getDistanceBetweenVectors(ourTopLeft, hitbox.closestPoint(ourTopLeft));
            var bl = getDistanceBetweenVectors(ourBottomLeft, hitbox.closestPoint(ourBottomLeft));

            if (!botRightCollide)
                if (br < epsilon)
                    botRightCollide = true;

            if (!topRightCollide)
                if (tr < epsilon)
                    topRightCollide = true;

            if (!topLeftCollide)
                if (tl < epsilon)
                    topLeftCollide = true;

            if (!botLeftCollide)
                if (bl < epsilon)
                    botLeftCollide = true;


            if (!isDoingHitboxStuff)
                StartCoroutine(doHitboxStuff());
        }

        private float getDistanceBetweenVectors(Vector2 a, Vector2 b)
        {
            return (float) Math.Sqrt(Math.Pow(a.x - b.x, 2.0) + Math.Pow(a.y - b.y, 2.0));
        }

        private IEnumerator doHitboxStuff()
        {
            isDoingHitboxStuff = true;

            yield return null;


            var direction = 0;
            var collides = 0;

            if (topLeftCollide)
                collides++;
            if (topRightCollide)
                collides++;
            if (botLeftCollide)
                collides++;
            if (botRightCollide)
                collides++;

            if (!topRightCollide && topLeftCollide)
            {
                direction = 3;
                // left and right collides should just disappear into balls because the animation looks stupid otherwise
                // Not much I can do about it.
                collides = 0;
            }
            else if (topRightCollide && !topLeftCollide)
            {
                direction = 1;
                collides = 0;
            }
            else if (!botLeftCollide && !botRightCollide)
            {
                direction = 2;
            }
            else if (topLeftCollide && topRightCollide && botLeftCollide && botRightCollide)
            {
                direction = 0;
                collides = 0;
            }

            fireball.transform.rotation = Quaternion.identity;
            fireball.transform.Rotate(new Vector3(0f, 0f, (float) (direction * 90.0)));
            var balls = redwing_flame_gen.rng.Next(0, 5);


            if (collides > 1)
            {
                StartCoroutine(magmaFadeAnimation(direction));
                fireballDmg = fbmDamageBase + fbmDamageScale * PlayerData.instance.GetInt("nailSmithUpgrades");
            }
            else
            {
                ballExplode();
                fireballDmg = 0;
            }

            generateFireballMagmaBalls(balls);
        }

        private void ballExplode()
        {
            var fireballSpriteColor = fireballSprite.color;
            fireballSpriteColor.a = 0f;
            fireballSprite.color = fireballSpriteColor;
            generateFireballMagmaBalls(4);
            generateFireballMagmaBalls(4);
            stopAppear = true;
        }

        private void generateFireballMagmaBalls(int balls)
        {
            if (balls == 0) return;

            for (var i = 0; i < balls; i++)
            {
                ballObjs[i] = new GameObject("FireballMagmaFireball" + i, typeof(Rigidbody2D),
                    typeof(redwing_fireball_magma_fireball_behavior), typeof(SpriteRenderer));
                ballObjs[i].transform.localPosition = selfTranform.position;
                ballObjs[i].transform.parent = fireball.transform;
                ballObjs[i].layer = 31;

                var a = ballObjs[i].GetComponent<redwing_fireball_magma_fireball_behavior>();
                a.self = ballObjs[i];
                a.selfSprite = ballObjs[i].GetComponent<SpriteRenderer>();
                var b = ballObjs[i].GetComponent<Rigidbody2D>();
                b.velocity = new Vector2((float) (redwing_flame_gen.rng.NextDouble() - 0.5) * 5f,
                    (float) (redwing_flame_gen.rng.NextDouble() * 2.5 + 2f));
                b.mass = 0.005f;
                b.isKinematic = true;

                var r = new Rect(0, 0, fireballMagmaFireballWidth, fireballMagmaFireballHeight);
                var d = ballObjs[i].GetComponent<SpriteRenderer>();
                d.sprite = Sprite.Create(fireballMagmaFireballs[i], r, new Vector2(0.5f, 0.5f));
                d.enabled = true;
                d.color = Color.white;

                ballObjs[i].SetActive(true);
            }
        }

        private IEnumerator magmaFadeAnimation(int directionToFade)
        {
            despawnBall = false;
            var animTime = 0f;
            var frame = 0;
            var oldFrame = -1;

            while (frame < fireballMagmas.Length)
            {
                if (frame > oldFrame)
                {
                    var r = new Rect(0, 0, FIREBALL_WIDTH, FIREBALL_HEIGHT);
                    fireballSprite.sprite = Sprite.Create(fireballMagmas[frame], r, new Vector2(0.5f, 0.5f));
                    oldFrame = frame;
                }

                frame++;
                yield return new WaitForSeconds((float) (1.0 / MAGMA_FRAMERATE));
            }

            Destroy(fireball);
            Destroy(gameObject);
            yield return null;
        }

        private static void log(string str)
        {
            Logger.Log("[Redwing] " + str);
        }
    }

    internal class redwing_fireball_magma_fireball_behavior : MonoBehaviour
    {
        private const float LIFETIME = 0.5f;
        public GameObject self;
        public SpriteRenderer selfSprite;

        public void Start()
        {
            StartCoroutine(fade());
        }

        private IEnumerator fade()
        {
            var life = 0f;
            while (life < LIFETIME)
            {
                life += Time.deltaTime;
                var selfSpriteColor = selfSprite.color;
                selfSpriteColor.a = 1.0f - life / LIFETIME;
                selfSprite.color = selfSpriteColor;
                yield return null;
            }

            Destroy(this);
        }
    }


    public static class collider_two_dimensional_extension
    {
        /// <summary>
        ///     Return the closest point on a Collider2D relative to point
        /// </summary>
        public static Vector2 closestPoint(this Collider2D col, Vector2 point)
        {
            var go = new GameObject("tempCollider");
            go.transform.position = point;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.1f;
            var dist = col.Distance(c);
            Object.Destroy(go);
            return dist.pointA;
        }

        public static bool didCollide(this Collider2D col, Vector2 point)
        {
            var go = new GameObject("tempCollider");
            go.transform.position = point;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.1f;
            var collideWithCol = c.IsTouching(col);
            Object.Destroy(go);
            return collideWithCol;
        }
    }
}