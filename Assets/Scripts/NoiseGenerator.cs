using UnityEditor.PackageManager.UI;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
    public static float[,] Generate(int width, int height, float scale, Vector2 offset)
    {
        //Create Noise Map 2D array
        float[,] noiseMap = new float[width, height];

        //Iterate through elements in noise map
        for (int x = 0; x < width; x++)
        {

            for (int y = 0; y < height; y++)
            {
                //calculate sample positions
                float samplePosX = (float)x * scale + offset.x;
                float samplePosY = (float)y * scale + offset.y;

                noiseMap[x, y] = Mathf.PerlinNoise(samplePosX, samplePosY);
            }
        }

        return noiseMap;
    }
}
