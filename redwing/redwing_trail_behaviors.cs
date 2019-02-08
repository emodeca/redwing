using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Modding.Logger;

namespace redwing
{
    public class redwing_trail_behavior : MonoBehaviour
    {
        private const float LIFESPAN = 0.6f;
        private const float DAMAGEPERIOD = 0.3f;
        private const int SECONDARYDAMAGETIMES = 0;

        // woah... that's strong. especially when combined with the blackmoth dash.
        public static int damagePriBase;
        public static int damagePriNail;
        public static int damageSecBase;
        public static int damageSecNail;

        public AudioSource cachedAudio;
        private int cachedPrimaryDmg;
        private int cachedSecondaryDmg;

        private float currentTime;
        public List<Collider2D> enteredColliders;

        private Vector2 initPosition;

        public MeshFilter memeFilter;
        public MeshRenderer memeRenderer;

        public Texture2D memeTextureUsed;


        private bool stopAnimation;
        public BoxCollider2D voidKnightCollider;

        public void Start()
        {
            enteredColliders = new List<Collider2D>();

            cachedPrimaryDmg = damagePriBase + damagePriNail * PlayerData.instance.GetInt("nailSmithUpgrades");
            cachedSecondaryDmg = damageSecBase + damageSecNail * PlayerData.instance.GetInt("nailSmithUpgrades");
            cachedAudio.loop = false;
            var vol = GameManager.instance.gameSettings.masterVolume *
                      GameManager.instance.gameSettings.soundVolume * 0.01f;
            if (GameManager.instance.gameSettings.masterVolume <= 5) vol /= 2f;

            if (GameManager.instance.gameSettings.soundVolume <= 5) vol /= 2f;
            cachedAudio.volume = vol * 0.4f;

            cachedAudio.Play();
            StartCoroutine(dashAnimation());
            StartCoroutine(secondaryAttacks());
        }

        private IEnumerator dashAnimation()
        {
            initPosition = voidKnightCollider.gameObject.transform.position;
            const int pixelsPerUnit = 75;
            buildTextureInital(75);

            yield return null;
            var maxDashLength = memeTextureUsed.width;
            var estimatedDistanceMoved = 0f;
            var cachedCollider = gameObject.GetComponent<BoxCollider2D>();
            while ((HeroController.instance.cState.dashing || HeroController.instance.cState.shadowDashing ||
                    HeroController.instance.cState.backDashing) && !stopAnimation)
            {
                Vector2 currentPosition = voidKnightCollider.gameObject.transform.position;
                var deltaPosition = currentPosition - initPosition;
                //log($@"currentPos is {currentPosition} and init position is {initPosition}");
                estimatedDistanceMoved += deltaPosition.magnitude;

                //log("void knight moved " + estimatedDistanceMoved + " units total");

                updateTextureInProgress(pixelsPerUnit, estimatedDistanceMoved);

                var cachedColliderSize = cachedCollider.size;
                cachedColliderSize.x = estimatedDistanceMoved;
                cachedColliderSize.y = voidKnightCollider.size.y;

                cachedCollider.size = cachedColliderSize;
                cachedCollider.offset = new Vector2(cachedColliderSize.x / 2, 0);

                initPosition = currentPosition;
                yield return null;
            }

            if (stopAnimation) log("stopped animation because stopanimation true");

            capTexture(75, estimatedDistanceMoved);
            StartCoroutine(playAnimation());
        }

        private void capTexture(int pixelsPerUnit, float distanceMoved)
        {
            var capWidth = redwing_flame_gen.FTCAPTEXTURE_WIDTH / (float) pixelsPerUnit;
            var capHeight = redwing_flame_gen.FTTEXTURE_HEIGHT / (float) pixelsPerUnit;

            memeFilter.mesh.vertices = new[]
            {
                memeFilter.mesh.vertices[0],
                memeFilter.mesh.vertices[1],
                memeFilter.mesh.vertices[2],
                memeFilter.mesh.vertices[3],
                new Vector3(distanceMoved, -capHeight / 2, 0.01f),
                new Vector3(distanceMoved, capHeight / 2, 0.01f),
                new Vector3(capWidth + distanceMoved, -capHeight / 2, 0.01f),
                new Vector3(capWidth + distanceMoved, capHeight / 2, 0.01f)
            };
            memeFilter.mesh.RecalculateBounds();
        }

        private void updateTextureInProgress(int pixelsPerUnit, float distanceMoved)
        {
            var capHeight = redwing_flame_gen.FTTEXTURE_HEIGHT / (float) pixelsPerUnit;

            memeFilter.mesh.vertices = new[]
            {
                memeFilter.mesh.vertices[0],
                memeFilter.mesh.vertices[1],
                memeFilter.mesh.vertices[2],
                memeFilter.mesh.vertices[3],
                new Vector3(distanceMoved, -capHeight / 2, 0.01f),
                new Vector3(distanceMoved, capHeight / 2, 0.01f),
                new Vector3(distanceMoved, -capHeight / 2, 0.01f),
                new Vector3(distanceMoved, capHeight / 2, 0.01f)
            };
            memeFilter.mesh.RecalculateBounds();
        }

        private void buildTextureInital(int pixelsPerUnit)
        {
            var capWidth = redwing_flame_gen.FTCAPTEXTURE_WIDTH / (float) pixelsPerUnit;
            var capHeight = redwing_flame_gen.FTTEXTURE_HEIGHT / (float) pixelsPerUnit;

            var leftCapWidthPercentage = redwing_flame_gen.FTCAPTEXTURE_WIDTH / (float) memeTextureUsed.width;
            var middleWidthPercentage = redwing_flame_gen.FTTEXTURE_WIDTH / (float) memeTextureUsed.width;
            var m = new Mesh
            {
                name = "redwingtrailmesh",
                vertices = new[]
                {
                    new Vector3(-capWidth, -capHeight / 2, 0.01f),
                    new Vector3(0, -capHeight / 2, 0.01f),
                    new Vector3(0, capHeight / 2, 0.01f),
                    new Vector3(-capWidth, capHeight / 2, 0.01f),
                    new Vector3(0, -capHeight / 2, 0.01f),
                    new Vector3(0, capHeight / 2, 0.01f),
                    new Vector3(0, -capHeight / 2, 0.01f),
                    new Vector3(0, capHeight / 2, 0.01f)
                },
                uv = new Vector2[8]
                {
                    new Vector2(0, 0),
                    new Vector2(leftCapWidthPercentage, 0),
                    new Vector2(leftCapWidthPercentage, 1),
                    new Vector2(0, 1),
                    new Vector2(leftCapWidthPercentage + middleWidthPercentage, 0),
                    new Vector2(leftCapWidthPercentage + middleWidthPercentage, 1),
                    new Vector2(1, 0),
                    new Vector2(1, 1)
                },
                triangles = new[]
                {
                    2, 1, 0,
                    3, 2, 0,
                    5, 4, 1,
                    2, 5, 1,
                    7, 6, 4,
                    5, 7, 4
                }
            };

            m.RecalculateNormals();

            memeFilter.mesh = m;
            Material memeMaterial;
            ( memeMaterial = memeRenderer.material).shader = Shader.Find("Particles/Additive");
             memeMaterial.mainTexture = memeTextureUsed;
             memeMaterial.color = Color.white;
        }


        private IEnumerator playAnimation()
        {
            currentTime = 0f;
            var cachedColor = memeRenderer.material.GetColor("_TintColor");

            while (currentTime < LIFESPAN)
            {
                currentTime += Time.unscaledDeltaTime;
                cachedColor.a = (LIFESPAN - currentTime) / (LIFESPAN * 2);
                memeRenderer.material.SetColor("_TintColor", cachedColor);
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator secondaryAttacks()
        {
            yield return null;
            var secDamages = 0;
            while (secDamages < SECONDARYDAMAGETIMES)
            {
                secondaryDamage();
                secDamages++;
                yield return new WaitForSeconds(DAMAGEPERIOD);
            }
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
                redwing_game_objects.applyHitInstance(collider.gameObject, cachedSecondaryDmg,
                    gameObject, 0f);
        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            var layer = collision.gameObject.layer;
            if (layer != 11) return;

            if (enteredColliders.Contains(collision)) return;

            enteredColliders.Add(collision);
            burnThatMotherTrucker(collision.gameObject);
        }

        /*
        private void OnTriggerExit2D(Collider2D other)
        {
            int layer = other.gameObject.layer;
            if (layer != 11) return;

            try
            {
                enteredColliders.Remove(other);
            }
            catch (Exception e)
            {
                log("failed to remove collider. error " + e);
            }
            
        }*/

        private void burnThatMotherTrucker(GameObject enemy)
        {
            redwing_game_objects.applyHitInstance(enemy, cachedPrimaryDmg, gameObject, 0.33f);
        }


        private static void log(string str)
        {
            Logger.Log("[Redwing] " + str);
        }
    }
}