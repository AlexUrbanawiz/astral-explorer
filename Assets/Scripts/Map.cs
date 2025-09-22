using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode] 
public class Map : MonoBehaviour
{
    public RawImage debugImage;

    [Header("Dimensions")]
    public int width;
    public int height;
    public float scale;
    public Vector2 offset;

    [Header("Height Map")]
    public float[,] heightMap;

    private float lastGenerateTime;

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Time.time - lastGenerateTime > 0.1f)
        {
            lastGenerateTime = Time.time;
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        heightMap = NoiseGenerator.Generate(width, height, scale, offset);

        Color[] pixels = new Color[width * height];
        int i = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pixels[i] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
                i++;
            }
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        debugImage.texture = tex;
    }
}
