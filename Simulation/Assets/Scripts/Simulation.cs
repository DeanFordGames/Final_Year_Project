using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Simulation : MonoBehaviour
{
    [Header("Map Generation Variables")]
    [SerializeField]
    private int mapsize = 255;
    [SerializeField]//size in width and length of terrain
    private int scale = 20;
    [SerializeField]//size in height of terrain
    private float elevation = 10;
    [Header("Water Variables")]
    [SerializeField]
    private bool animateWater = true;
    [SerializeField]//how much water gets added through rain or source
    private float waterIncrease = 0.03f;
    //length of pipes connecting cells
    private float length = 1f;
    //area of pipes
    private float crossSection = 1f;
    //no idea what this imaginary thing is
    private float gravity = 9.81f;
    [SerializeField]//water evaporation scale
    private float evaporationScale = 0.1f;
    //delta time-time step
    private float dTime = 0.022f;
    [Header("Erosion Variables")]
    [SerializeField]//sediment maximum capacity
    private float sedimentCap = 0.1f;
    [SerializeField]
    private float dissolvingConst = 0.5f;
    [SerializeField]
    private float depositionConst = 0.5f;
    [SerializeField]
    private float erosionScalar = 0.005f;

    [Header("Render Texture")]
    [SerializeField]//r = terrain map | g = water map | b = sediment
    private RenderTexture terrain;
    [SerializeField]//r = left pipe | g = right pipe | b = top pipe | a = bottom pipe
    private RenderTexture flux;
    [SerializeField]//r or g = x | b or a = y
    private RenderTexture velocityField;
    [SerializeField]
    private RenderTexture normalMap;

    [Header("Compute Shader")]
    [SerializeField]
    private ComputeShader calculateNormal;
    [SerializeField]
    private ComputeShader addWater;
    [SerializeField]
    private ComputeShader calculateFlux;
    [SerializeField]
    private ComputeShader simulateWater;
    [SerializeField]
    private ComputeShader erode;
    [SerializeField]
    private ComputeShader transport;
    [SerializeField]
    private ComputeShader evaporate;

    Mesh terrainMesh;
    MeshFilter terrainMeshFilter;
    Material terrainMat;

    private int averageCount = 0;
    private float[] times;

    private void Awake()
    {
        //assign meshfilter meshfilter
        terrainMeshFilter = GetComponent<MeshFilter>();
        terrainMat = GetComponent<MeshRenderer>().material;
    }

    private void Start()
    {
        InitializeTextures();
        InitializeMesh();
        UpdateShaders();
        times = new float[5000];
    }

    private void Update()
    {
        Simulate();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitializeTextures();
            InitializeMesh();
        }
        if (animateWater == true)
            terrainMat.SetInt("_AnimateWater", 1);
        else
            terrainMat.SetInt("_AnimateWater", 0);
        UpdateShaders();

        times[averageCount] = Time.deltaTime;
        averageCount++;
        if (averageCount >= times.Length)
        {
            float average = 0;
            for (int i = 0; i < times.Length; i++)
                average += times[i];
            average /= averageCount;
            Debug.Log("Average time over 5000 cycles = " + average);
            averageCount = 0;
            times = new float[5000];
        }
    }

    public void UpdateShaders()
    {
        //send textures to normal map shader and start shader
        calculateNormal.SetTexture(0, "normalMap", normalMap);
        calculateNormal.SetTexture(0, "heightMap", terrain);
        calculateNormal.SetBool("includeWater", animateWater);
        calculateNormal.Dispatch(0, normalMap.width / 32, normalMap.height / 32, 1);
        //send updated maps to shader for vertex displacement and lighting
        terrainMat.SetTexture("_Displacement", terrain);
        terrainMat.SetTexture("_NormalMap", normalMap);
        terrainMat.SetFloat("_Amount", elevation);
        terrainMat.SetFloat("_MapSize", mapsize);
    }

    public void InitializeMesh()
    {
        //generate height map
        terrain = GetComponent<GenerateMap>().Generate(mapsize);
        //generate mesh
        GenerateMesh(terrainMeshFilter, terrainMesh);
    }

    public void InitializeTextures()
    {
        terrain = new RenderTexture(mapsize, mapsize, 24, RenderTextureFormat.ARGBFloat);
        velocityField = new RenderTexture(mapsize, mapsize, 24, RenderTextureFormat.RGFloat);
        flux = new RenderTexture(mapsize, mapsize, 24, RenderTextureFormat.ARGBFloat);
        normalMap = new RenderTexture(mapsize, mapsize, 24);

        //required to edit textures
        terrain.enableRandomWrite = true;
        velocityField.enableRandomWrite = true;
        flux.enableRandomWrite = true;
        normalMap.enableRandomWrite = true;

        terrain.Create();
        velocityField.Create();
        flux.Create();
        normalMap.Create();
    }

    void GenerateMesh(MeshFilter meshFilter, Mesh mesh)
    {
        //initialize vertex and triangle arrays for mesh
        Vector3[] vertices = new Vector3[mapsize * mapsize];
        int[] triangles = new int[(mapsize - 1) * (mapsize - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];


        for (int t = 0, i = 0, z = 0; z < mapsize; z++)
        {
            for (int x = 0; x < mapsize; x++)
            {
                i = z * mapsize + x;

                //scale and assign vertices
                Vector2 percent = new Vector2(x / (mapsize - 1f), z / (mapsize - 1f));
                Vector3 pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

                vertices[i] = pos;
                uvs[i] = percent;

                //Create Triangles
                if (x != mapsize - 1 && z != mapsize - 1)
                {
                    triangles[t + 0] = i + mapsize;
                    triangles[t + 1] = i + mapsize + 1;
                    triangles[t + 2] = i;

                    triangles[t + 3] = i + mapsize + 1;
                    triangles[t + 4] = i + 1;
                    triangles[t + 5] = i;

                    t += 6;
                }

            }
        }

        //create new mesh/clear current for next frame
        if (mesh == null)
        {
            mesh = new Mesh();
            meshFilter.mesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        //apply vertices, triangles and uv to new mesh 
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;


    }

    private void addingWater()
    {
        addWater.SetTexture(0, "terrain", terrain);
        addWater.SetFloat("waterIncrease", waterIncrease);
        addWater.SetFloat("dTime", dTime);
        addWater.SetFloat("scale", elevation);
        addWater.SetFloat("size", mapsize);

        addWater.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void updateFlux()
    {
        calculateFlux.SetTexture(0, "flux", flux);
        calculateFlux.SetTexture(0, "terrain", terrain);
        calculateFlux.SetFloat("dTime", dTime);
        calculateFlux.SetFloat("a", crossSection);
        calculateFlux.SetFloat("g", gravity);
        calculateFlux.SetFloat("l", length);
        calculateFlux.SetFloat("scale", elevation);
        calculateFlux.SetFloat("size", mapsize);

        calculateFlux.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void updateWater()
    {
        simulateWater.SetTexture(0, "flux", flux);
        simulateWater.SetTexture(0, "velocityField", velocityField);
        simulateWater.SetTexture(0, "terrain", terrain);
        simulateWater.SetFloat("l", length);
        simulateWater.SetFloat("dTime", dTime);
        simulateWater.SetFloat("scale", elevation);
        simulateWater.SetFloat("size", mapsize);

        simulateWater.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void computeErosion()
    {
        erode.SetTexture(0, "velocity", velocityField);
        erode.SetTexture(0, "terrain", terrain);
        erode.SetFloat("sedimentCapConst", sedimentCap);
        erode.SetFloat("dissolvingConst", dissolvingConst);
        erode.SetFloat("depositionConst", depositionConst);
        erode.SetFloat("scalar", erosionScalar);
        erode.SetFloat("l", length);
        erode.SetFloat("size", mapsize);


        erode.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void sedimentTransport()
    {
        transport.SetTexture(0, "terrain", terrain);
        transport.SetTexture(0, "velocity", velocityField);
        transport.SetFloat("dTime", dTime);
        transport.SetFloat("size", mapsize);

        transport.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void computeEvaporation()
    {
        evaporate.SetTexture(0, "terrain", terrain);
        evaporate.SetFloat("evaporationConst", evaporationScale);
        evaporate.SetFloat("dTime", dTime);

        evaporate.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void Simulate()
    {
        if (terrain == null || flux == null || velocityField == null || normalMap == null)
            return;

        crossSection = length * length;

        addingWater();

        updateFlux();

        updateWater();

        computeErosion();

        sedimentTransport();

        computeEvaporation();
    }
}
