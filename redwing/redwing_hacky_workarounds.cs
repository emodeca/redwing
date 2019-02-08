using System;
using System.IO;
using UnityEngine;
using Logger = Modding.Logger;


// Radiance help and forgive me if I ever need to use this class for anything.
namespace redwing
{
    public static class redwing_static_hacks
    {
        public static Texture2D textureReadHack(Texture2D in_tex)
        {
            var temporary = RenderTexture.GetTemporary(in_tex.width, in_tex.height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(in_tex, temporary);
            var active = RenderTexture.active;
            RenderTexture.active = temporary;
            var texture2D = new Texture2D(in_tex.width, in_tex.height);
            texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            return texture2D;
        }

        public static Texture2D subTexture(Texture2D in_tex, int x, int y, int w, int h)
        {
            var temporary = RenderTexture.GetTemporary(in_tex.width, in_tex.height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(in_tex, temporary);
            var active = RenderTexture.active;
            RenderTexture.active = temporary;
            var texture2D = new Texture2D(w, h);
            texture2D.ReadPixels(new Rect(x, y, w, h), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            return texture2D;
        }

        public static void saveTextureToFile(Texture2D tex, string fileName)
        {
            var buffer = tex.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(fileName) ?? throw new NullReferenceException());
            var output = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            var binaryWriter = new BinaryWriter(output);
            binaryWriter.Write(buffer);
            binaryWriter.Close();
        }

        public static bool validEnemy(GameObject enemy)
        {
            if (enemy.name.StartsWith("Health Scuttler")) return false;

            return true;
        }
    }


    public class redwing_hacky_workarounds : MonoBehaviour
    {
        private void OnDestroy()
        {
        }


        private void Start()
        {
        }


        private static void log(string str)
        {
            Logger.Log("[Redwing] " + str);
        }
    }
}