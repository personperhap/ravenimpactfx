using System;
using Enum;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "VolumetricFogOptions", menuName = "Volumetric Fog/Fog Options")]
public class VolumetricFogOptions : ScriptableObject
{
    [Header("Position and size(in m³)")]
    public bool limitFogInSize;
    public Vector3 fogWorldPosition;
    public float fogSize = 50.0f;

    [Header("Performance")] 
    [Range(0, 8)] public int renderTextureResDivision;
    [Range(1, 1024)] public int rayMarchSteps = 350;

    public bool optimizeSettingsFps; // optimize raymarch steps according to fps
    public FPSTarget fpsTarget = FPSTarget.Max60;

    [Header("Physical coefficients")] 
    public bool useRayleighScattering = true;
    [Range(-1, 2)] public float rayleighScatteringCoef = 0.003f;

    [Range(-1, 2)] public float mieScatteringCoef = 0.007f;
    public MieScatteringApproximation mieScatteringApproximation = MieScatteringApproximation.HenyeyGreenstein;

    [Range(0, 100f)] public float fogDensityCoef = 12f;
    [Range(0, 1f)] public float extinctionCoef = 0.01f;
    [Range(-1f, 1f)] public float anisotropy = -.3f;
    [Range(0, 1f)] public float heightDensityCoef = 0.2f;
    [Range(0, 10000)] public float baseHeightDensity = 6000f;

    [Header("Blur")]
    [Range(1, 8)] public int blurIterations = 4;
    [Range(0, 2000f)] public float blurDepthFalloff = 100f;
    public Vector3 blurOffsets = new Vector3(1, 2, 3);
    public Vector3 blurWeights = new Vector3(0.213f, 0.17f, 0.036f);

    [Header("Color")] 
    public bool useLightColorForFog = true;
    public Color fogInShadowColor = new Color32(10, 70, 200, 255);
    public Color fogInLightColor = new Color32(255, 65, 0, 255);
    [Range(0, 1)] public float ambientFog = .4f;
    
    [Header("Sun")]
    [Range(0, 10)] public float lightIntensity = 1;
    public bool sunShouldMove;
    public Vector3 sunAngle = new Vector3(13, 0, 0);
    [Range(-10,10)] public float moveSpeed = 2;

    [Header("Animation")]
    public Vector3 windDirection = Vector3.right;
    public float speed = 1f;

    [Header("Debug")] 
    public NoiseSource noiseSource = NoiseSource.Texture3D;
    public bool addSceneColor = true;
    public bool blurEnabled = true;
    public bool shadowsEnabled = true;
    public bool heightFogEnabled;

    [Range(-100, 100)] public float noiseScale = 0.9f;
    // [Range(1, 16)] public float _NoiseOctaves = 1f; TODO

    public Vector3Int noiseTexture3DDimensions = new Vector3Int(64, 64, 86);
}