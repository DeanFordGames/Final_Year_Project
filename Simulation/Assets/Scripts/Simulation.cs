using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public int mapsize = 255;
    //size in width and length of terrain
    public int scale = 20;
    //size in height of terrain
    public float elevation = 10;
    //how much water gets added through rain or source
    public float waterIncrease = 0.03f;
    //length of pipes connecting cells
    public float length = 1f;
    //area of pipes
    private float crossSection = 1f;
    //no idea what this imaginary thing is
    private float gravity = 9.81f;
    //delta time-time step
    public float dTime = 0.022f;
    //sediment maximum capacity
    public float sedimentCap = 0.1f;
    //water evaporation scale
    public float evaporationScale = 0.1f;

    //r = terrain map | g = water map | b = sediment
    public RenderTexture terrain;
    //r = left pipe | g = right pipe | b = top pipe | a = bottom pipe
    public RenderTexture flux;
    //r or g = x | b or a = y
    public RenderTexture velocityField;
    public RenderTexture normalMap;

    public ComputeShader calculateNormal;
    public ComputeShader addWater;
    public ComputeShader calculateFlux;
    public ComputeShader simulateWater;
    public ComputeShader erode;
    public ComputeShader transport;
    public ComputeShader evaporate;

    Mesh terrainMesh;
    MeshFilter terrainMeshFilter;
    Material terrainMat;

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
    }

    private void Update()
    {
        Simulate();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitializeTextures();
            InitializeMesh();
        }
        UpdateShaders();
    }

    public void UpdateShaders()
    {
        //send textures to normal map shader and start shader
        calculateNormal.SetTexture(0, "normalMap", normalMap);
        calculateNormal.SetTexture(0, "heightMap", terrain);
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
        terrain = new RenderTexture(mapsize, mapsize, 24);
        velocityField = new RenderTexture(mapsize, mapsize, 24);
        flux = new RenderTexture(mapsize, mapsize, 24);
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

        simulateWater.Dispatch(0, terrain.width / 32, terrain.height / 32, 1);
    }

    private void computeErosion()
    {
        erode.SetTexture(0, "velocity", velocityField);
        erode.SetTexture(0, "terrain", terrain);
        erode.SetFloat("sedimentCapConst", sedimentCap);
        erode.SetFloat("l", length);

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
