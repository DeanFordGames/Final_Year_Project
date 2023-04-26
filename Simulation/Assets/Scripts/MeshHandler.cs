using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshHandler : MonoBehaviour
{
    public int mapsize = 255;
    public int scale = 20;
    public float elevation = 10;

    public RenderTexture terrainMap;
    public RenderTexture normalMap;
    public WaterHandler waterHandler;
    public ComputeShader calculateNormal;

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
        InitializeMesh();
        UpdateShaders();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitializeMesh();
        }
        UpdateShaders();
    }

    public void UpdateShaders()
    {
        //send textures to normal map shader and start shader
        calculateNormal.SetTexture(0, "normalMap", normalMap);
        calculateNormal.SetTexture(0, "heightMap", terrainMap);
        calculateNormal.Dispatch(0, normalMap.width / 32, normalMap.height / 32, 1);
        //send updated maps to shader for vertex displacement and lighting
        terrainMat.SetTexture("_Displacement", terrainMap);
        terrainMat.SetTexture("_NormalMap", normalMap);
        terrainMat.SetFloat("_Amount", elevation);
    }

    public void InitializeMesh()
    {
        //create new normal map
        normalMap = new RenderTexture(mapsize, mapsize, 24);
        normalMap.enableRandomWrite = true;
        normalMap.Create();
        //generate height map
        terrainMap = GetComponent<GenerateMap>().Generate(mapsize);
        waterHandler.InitializeTextures(mapsize, terrainMap);
        //generate mesh
        GenerateMesh(terrainMeshFilter, terrainMesh);
        GenerateMesh(waterHandler.waterMeshFilter, waterHandler.waterMesh);
    }

    void GenerateMesh(MeshFilter meshFilter, Mesh mesh)
    { 
        //initialize vertex and triangle arrays for mesh
        Vector3[] vertices = new Vector3[mapsize * mapsize];
        int[] triangles = new int[(mapsize - 1) * (mapsize - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];


        for (int t = 0, i = 0, z = 0; z < mapsize; z++)
        {
            for(int x = 0; x < mapsize; x++)
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
        if(mesh == null)
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
}
