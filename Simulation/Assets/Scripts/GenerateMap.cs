using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMap : MonoBehaviour
{
    //number of snoise iterations
    public int octaves = 10;
    //how much the noise will be scaled for each iteration
    public float lacunarity = 1.2f;
    public float scale = 3.0f;
    //scale down the impact of each iteration
    public float weightScaler = 0.4f;
    public int seed = 0;

    public ComputeShader mapGeneratorShader;

    public RenderTexture Generate(int _mapsize)
    {
        //generate a random seed
        seed = Random.Range(-10000, 10000);
        System.Random randSeed = new System.Random(seed);
        //create offsets for noise variation 
        Vector2[] offsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            offsets[i] = new Vector2(randSeed.Next(-1000, 1000), randSeed.Next(-1000, 1000));
        }
        //create a buffer and send to GPU shader
        ComputeBuffer offsetBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 2);
        offsetBuffer.SetData(offsets);
        mapGeneratorShader.SetBuffer(0, "offset", offsetBuffer);

        //create new render texture and send to GPU shader
        RenderTexture terrainMap = new RenderTexture(_mapsize, _mapsize, 24);
        terrainMap.enableRandomWrite = true;
        terrainMap.Create();
        mapGeneratorShader.SetTexture(0, "map", terrainMap);

        //send remaining values required to GPU shader
        mapGeneratorShader.SetInt("mapSize", _mapsize);
        mapGeneratorShader.SetInt("octaves", octaves);
        mapGeneratorShader.SetFloat("lacunarity", lacunarity);
        mapGeneratorShader.SetFloat("scale", scale);
        mapGeneratorShader.SetFloat("weightScaler", weightScaler);
        //start GPU shader
        mapGeneratorShader.Dispatch(0, terrainMap.width/32, terrainMap.height/32, 1);
        //clear used buffer
        offsetBuffer.Release();

        return terrainMap;

    }
}
