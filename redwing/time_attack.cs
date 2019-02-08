using Modding;
using UnityEngine;
using UnityEngine.UI;

namespace redwing
{
    public class time_attack : MonoBehaviour
    {
        private GameObject canvas;
        private bool didDestroy;
        private Text textObj;
        public float timeRemaining;

        private void Start()
        {
            if (canvas != null) return;

            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            var go =
                CanvasUtil.CreateTextPanel(canvas, "", 70, TextAnchor.UpperRight,
                    new CanvasUtil.RectData(
                        new Vector2(0, 0),
                        new Vector2(0, 0),
                        new Vector2(0, 0),
                        new Vector2(0.95f, 0.95f),
                        new Vector2(0.5f, 0.5f)));


            textObj = go.GetComponent<Text>();
            textObj.color = Color.black;
            textObj.font = CanvasUtil.TrajanBold;
            textObj.text = "Lightbringer\nT-" + getTimeInCleanFormat(timeRemaining);
            textObj.fontSize = 90;
            textObj.CrossFadeAlpha(1f, 0f, false);
        }

        private void Update()
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining > 0.0f)
            {
                textObj.text = "Lightbringer\nT-" + getTimeInCleanFormat(timeRemaining);
            }
            else
            {
                textObj.color = Color.red;
                textObj.text = "Lightbringer\nT-00:00";
            }
        }


        public static string getTimeInCleanFormat(float time)
        {
            var seconds = ((int) time % 60).ToString();
            if (seconds.Length == 1) seconds = "0" + seconds;
            var minutes = ((int) time / 60).ToString();
            return minutes + ":" + seconds;
        }
    }
}