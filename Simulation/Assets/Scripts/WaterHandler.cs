using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterHandler : MonoBehaviour
{
    //r = terrain map | g = water map | b = sediment
    public RenderTexture terrain;
    //r = left pipe | g = right pipe | b = top pipe | a = bottom pipe
    private RenderTexture flux;
    //r or g = x | b or a = y
    public RenderTexture velocityField;
    public RenderTexture normalMap;

    private RenderTexture terrainMap;
    public RenderTexture waterMap;

    public ComputeShader addWater;
    public ComputeShader calculateFlux;
    public ComputeShader simulateWater;
    public ComputeShader calculateNormal;

    public Mesh waterMesh;
    public MeshFilter waterMeshFilter;
    public Material waterMat;

    public float amount = 1.0f;
    public float waterIncrease = 0.03f;
    private float length = 1f;
    private float crossSection = 1f;
    private float gravity = 9.87f;
    private float dTime;

    public void InitializeTextures(int _mapsize, RenderTexture _terrain)
    {
        waterMeshFilter = GetComponent<MeshFilter>();
        waterMat = GetComponent<MeshRenderer>().sharedMaterial;

        waterMap = new RenderTexture(_mapsize, _mapsize, 24);
        velocityField = new RenderTexture(_mapsize, _mapsize, 24);
        flux = new RenderTexture(_mapsize, _mapsize, 24);
        normalMap = new RenderTexture(_mapsize, _mapsize, 24);
        terrainMap = _terrain;

        waterMap.enableRandomWrite = true;
        velocityField.enableRandomWrite = true;
        flux.enableRandomWrite = true;
        normalMap.enableRandomWrite = true;

        waterMap.Create();
        velocityField.Create();
        flux.Create();
        normalMap.Create();

        updateMesh();
    }

    private void Awake()
    {
        waterMeshFilter = GetComponent<MeshFilter>();
        waterMat = GetComponent<MeshRenderer>().material;
    }

    private void Update()
    {
        if (terrainMap == null || flux == null || velocityField == null || waterMap == null)
            return;
        dTime = Time.deltaTime;
        crossSection = length * length;

        addingWater();

        updateFlux();

        updateWater();

        updateMesh();
    }

    private void addingWater()
    {
        addWater.SetTexture(0, "waterMap", waterMap);
        addWater.SetFloat("waterIncrease", waterIncrease);
        addWater.SetFloat("dTime", dTime);

        addWater.Dispatch(0, waterMap.width / 32, waterMap.height / 32, 1);
    }

    private void updateFlux()
    {
        calculateFlux.SetTexture(0, "flux", flux);
        calculateFlux.SetTexture(0, "terrainMap", terrainMap);
        calculateFlux.SetTexture(0, "waterMap", waterMap);
        calculateFlux.SetFloat("dTime", dTime);
        calculateFlux.SetFloat("a", crossSection);
        calculateFlux.SetFloat("g", gravity);
        calculateFlux.SetFloat("l", length);

        calculateFlux.Dispatch(0, waterMap.width / 32, waterMap.height / 32, 1);
    }

    private void updateWater()
    {
        simulateWater.SetTexture(0, "flux", flux);
        simulateWater.SetTexture(0, "velocityField", velocityField);
        simulateWater.SetTexture(0, "terrainMap", terrainMap);
        simulateWater.SetTexture(0, "waterMap", waterMap);
        simulateWater.SetFloat("l", length);

        simulateWater.Dispatch(0, waterMap.width / 32, waterMap.height / 32, 1);
    }

    private void updateMesh()
    {
        calculateNormal.SetTexture(0, "heightMap", waterMap);
        calculateNormal.SetTexture(0, "normalMap", normalMap);

        calculateNormal.Dispatch(0, waterMap.width / 32,  waterMap.height / 32, 1);

        waterMat.SetTexture("_Displacement", waterMap);
        waterMat.SetTexture("_Terrain", terrainMap);
        waterMat.SetTexture("_NormalMap", normalMap);
        waterMat.SetFloat("_Amount", amount);
    }
}
