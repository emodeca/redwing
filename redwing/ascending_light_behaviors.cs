using System.Collections;
using UnityEngine;

namespace redwing
{
    public class ascending_light : MonoBehaviour
    {
        public SpriteRenderer ballSprite;
        public redwing_pillar_detect_behavior flamePillarDetect;
        public bool ghostBalls;
        public float timeBeforeDeath = 2f;

        private void Start()
        {
            StartCoroutine(despawnBallOverTime());
        }

        private IEnumerator despawnBallOverTime()
        {
            var c = ballSprite.color;

            for (var time = 0f; time < timeBeforeDeath; time += Time.deltaTime)
            {
                c.a = (timeBeforeDeath * 2f - time) / timeBeforeDeath;
                ballSprite.color = c;
                yield return null;
            }
        }


        // stolen from: Token: 0x0600006E RID: 110 RVA: 0x00005168 File Offset: 0x00003368
        public GameObject getTarget()
        {
            var enemyList = flamePillarDetect.enemyList;
            GameObject result = null;
            var num = 99999f;
            if (enemyList.Count > 0)
            {
                for (var i = enemyList.Count - 1; i > -1; i--)
                    if (enemyList[i] == null || !enemyList[i].activeSelf)
                        enemyList.RemoveAt(i);
                foreach (var gameObject in enemyList)
                    // just pick enemy in range
                    if (ghostBalls && gameObject != null)
                    {
                        var sqrMagnitude = (transform.position - gameObject.transform.position).sqrMagnitude;
                        if (sqrMagnitude < num)
                        {
                            result = gameObject;
                            num = sqrMagnitude;
                        }

                        // Otherwise also check if you can raycast to them.
                    }
                    else if (!Physics2D.Linecast(transform.position, gameObject.transform.position, 256))
                    {
                        var sqrMagnitude = (transform.position - gameObject.transform.position).sqrMagnitude;
                        if (sqrMagnitude < num)
                        {
                            result = gameObject;
                            num = sqrMagnitude;
                        }
                    }
            }

            return result;
        }
    }
}