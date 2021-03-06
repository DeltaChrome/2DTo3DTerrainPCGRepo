﻿
using System.Drawing;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;

/*  This Manager Manages all Managers :)
 *  Big Boss
 */

public class TerrainManager : MonoBehaviour
{

    //Resolution of Terrain Grid & Height Map
    public const float SIZE_FULL = 2049.0f; //Resolution of the height map texture

    /* ------------------------------------------------------------------ */
    /* INPUT MAP VARIABLES */
    /* ------------------------------------------------------------------ */

    // inputMapTextureDim * inputMapTextureDim pixels is the size of the user defined 2D map. We expect a square texture.
    private int inputMapTextureDim = 20;
    // Our user defined and provided 2D tiled map as a texture2D
    public Texture2D inputMapTexture;
    private Color[, ] inputMapGrid;

    /* ------------------------------------------------------------------ */
    /* TERRAIN TYPE VARIABLES */
    /* ------------------------------------------------------------------ */

    // terrainTypeGrid holds the values that determine the terrain type weighting for any point in the 3D landscape. This is a crucial variable for the functionality of the program.
    private Color[, ] terrainTypeGrid = new Color[(int)SIZE_FULL, (int)SIZE_FULL];

    // Modifies the degree of border warping between terrain type areas
    private const int terrainBorderShiftMod = 200;

    // Variables to adjust the Colour correction function's tolerance for minor differences in Color type property values in the input that would be unoticeable to the naked eye.
    // Determines how close to equal the RGB values need to be.
    private const float greyMountainTolerance = 0.02f;
    // Determines how much greater the value of blue in comparison to red and green to consider it water.
    private const float blueWaterTolerance = 0.1f;
    // Determines the value at which green must at least be to be considered Grassland.
    private const float greenGrasslandTolerance = 0.9f;

    /* ------------------------------------------------------------------ */
    /* BLUR VARIABLES */
    /* ------------------------------------------------------------------ */

    // Flag for whether or not pseudo-Gaussian blurring should execute. This is likely preferable over true Gaussian/Box blurring due to time complexity.
    private const bool doPseudoGaussBlur = true;

    // Pseudo Gaussian blur variables, runs in linear time, O(n) where n = number of pixels. Sigma determines the degree of blurring applied to terrainTypeGrid. There is a very small percentage margin or error on pixels.
    private const float pseudoGaussSigma = 30.0f;

    // Flag variables for whether true Gaussian, in index 0, and/or true Box, in index 1, should execute. By default, these are assigned values of false due to the computational load that true Gaussian/Box blurring requires.
    private static bool[] doGaussAndOrThenBoxBlur = new bool[] { false, false };

    // True Gaussian blur variables, runs in exponential time, O(n^2 + r^2) where n = number of pixels and r is the kernel size. Consider using pseudo-Gaussian blur or increasing the number of passes instead of increasing the size of the kernel.
    private float[, ] trueGaussBlurKernel;
    private const int trueGaussBlurKernelDimNxN = 3;
    private const float trueGaussSigma = 0.5f;
    private const int trueGaussNumPasses = 3;

    // True Box blur variables, runs in exponential time, O(n^2 + r^2) where n = number of pixels and r is the kernel size. Consider using pseudo-Gaussian blur or increasing the number of passes instead of increasing the size of the kernel.
    private const int boxBlurKernelDimNxN = 3;
    private const int boxBlurNumPasses = 3;

    /* ------------------------------------------------------------------ */
    /* MINIMAP GUI VARIABLES FOR TERRAIN TYPES & HEIGHT */
    /* ------------------------------------------------------------------ */

    // Terrain type grid texture for terrainTypeMiniMap
    private Texture2D terrainTypeBeforeFillTexture;
    private Texture2D terrainTypeFilledAndHorizWarpTexture;
    private Texture2D terrainTypeAfterVertWarpTexture;
    private Texture2D terrainTypeTexture;
    // Variables for rendering the mini maps for perlin noise and terrsain types.
    private Image terrainTypeMiniMap;
    private Material terrainTypeMaterial;
    private Image perlinNoiseMiniMap;
    private Material perlinNoiseMaterial;

    /* ------------------------------------------------------------------ */
    /* 3D LANDSCAPE APPEARANCE VARIABLES */
    /* ------------------------------------------------------------------ */

    private const float baseSnowHeight = 80.0f;
    //  1 / (fractionOfBlackSand+1) will control the sand speckling.
    private const int fractionOfBlackSand = 63;
    // Higher will mean higher variation in the starting height of snow but will also increase the likely snow height overall.
    private const int perlinSnowHeightMod = 20;
    // Higher will mean higher variation in the colour variation of the shoreline sand.
    private const int perlinSandVariationSpecklingMod = 30;


    /* ------------------------------------------------------------------ */

    // Perlin Noise Arrays.
    float[, ] perlinNoiseArray = new float[(int)SIZE_FULL, (int)SIZE_FULL];
    float[, ] perlinNoiseArrayMPass = new float[(int)SIZE_FULL, (int)SIZE_FULL];
    float[, ] perlinNoiseArrayMBasePass = new float[(int)SIZE_FULL, (int)SIZE_FULL];
    float[, ] perlinNoiseArrayMPoints = new float[(int)SIZE_FULL, (int)SIZE_FULL];

    //To be accessed by Water Manager
    float[, ] perlinNoiseArrayFinalized = new float[(int)SIZE_FULL, (int)SIZE_FULL];
    float[, ] perlinNoiseArrayFinalizedFlipped = new float[(int)SIZE_FULL, (int)SIZE_FULL];

    // For Individual Terrain Cell.
    float[, ] perlinNoiseArrayCell = new float[(int)SIZE_FULL, (int)SIZE_FULL];

    // Variables to hold the scene's terrain data
    public GameObject terrainObject;
    Terrain terrainComponent;

    //Water Manager Script - Eric's Code
    public GameObject waterManagerGO;
    public WaterManager waterManager;
    public AgentGenerator agentManager;

    public Material waterShader;
    public GameObject waterPlane;

    //Agent Manager Script - Nader's Code
    //*Mountain Manager Script - Jacob's Code*

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState((int)42);

        float timeStart = Time.realtimeSinceStartup;

        terrainTypeTexture = new Texture2D((int)SIZE_FULL, (int)SIZE_FULL);

        //Call Water Manager
        waterManager = new GameObject().AddComponent(typeof(WaterManager))as WaterManager;
        waterManager.name = "WaterManager";
        //agentManager = new GameObject().AddComponent(typeof(AgentGenerator))as AgentGenerator;
        //agentManager.name = "AgentManager";

        ClearNoise(perlinNoiseArray);
        ClearNoise(perlinNoiseArrayMPass);
        ClearNoise(perlinNoiseArrayFinalized);
        ClearNoise(perlinNoiseArrayCell);

        float timeStartTTG = Time.realtimeSinceStartup;
        //Create Terrain Type Grid
        InitTerrainTypeGrid();
        print("Seconds to create blurred terrain type grid: " + (Time.realtimeSinceStartup - timeStartTTG));

        float timeStartHeight = Time.realtimeSinceStartup;
        //Create Noise functions and merge them
        CreateHeightArray(); //Result is a filled perlinNoiseArrayFinalized array
        print("Seconds to set heights: " + (Time.realtimeSinceStartup - timeStartHeight));

        float timeStartShores = Time.realtimeSinceStartup;
        //Modify perlinNoiseArrayFinalized array for water manipulation
        //Call Water Manager
        waterManager.initialize((int)SIZE_FULL, terrainTypeGrid);
        perlinNoiseArrayFinalized = waterManager.getHeights(perlinNoiseArrayFinalized);
        print("Seconds to create shorelines: " + (Time.realtimeSinceStartup - timeStartShores));

        //Create Textures for Grids
        GenerateMapsGUI();

        //Call Terrain creation functions
        GenerateTerrain();

        // WaterManager AGAIN
        waterManager.waterShader = waterShader;
        waterManager.waterPlane = waterPlane;
        waterManager.createWaterPlane(terrainComponent);
        
        float timeStartPlace = Time.realtimeSinceStartup;
        //Call Agent Manager
        agentManager.IntiateAgentGenerator(terrainTypeGrid, waterManager.GetMaxShoreThreshold(), perlinNoiseArrayFinalizedFlipped);
        print("Seconds to place objects: " + (Time.realtimeSinceStartup - timeStartPlace));

        float timeStartModTer = Time.realtimeSinceStartup;
        ModifyTerrainTexture();
        print("Seconds to texture terrain: " + (Time.realtimeSinceStartup - timeStartModTer));

        print("Seconds to generate everything: " + (Time.realtimeSinceStartup - timeStart));
    }

    // Resets noise array to hold zeros.
    void ClearNoise(float[, ] noiseArray)
    {
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseArray[x, y] = 0;
            }
        }
    }


    void GenerateTTGOneImage(Texture2D mapTexture, string name)
    {
        mapTexture  = new Texture2D((int)SIZE_FULL, (int)SIZE_FULL);
        for (int x = 0; x < SIZE_FULL; x++)
        {
            for (int y = 0; y < SIZE_FULL; y++)
            {
                mapTexture.SetPixel(x, y, terrainTypeGrid[x, y]);
            }
        }
        //Apply texture
        mapTexture.Apply();

        Material mapMaterial = new Material(Shader.Find("Unlit/Texture"));
        Image miniMap = GameObject.Find(name).GetComponent<Image>();
        miniMap.material = mapMaterial;

        miniMap.material.mainTexture = mapTexture;
        saveTextureAsPNG(mapTexture, name);
    }


    //Generates the physical GUI
    void GenerateMapsGUI()
    {

        for (int x = 0; x < SIZE_FULL; x++)
        {
            for (int y = 0; y < SIZE_FULL; y++)
            {
                terrainTypeTexture.SetPixel(x, y, terrainTypeGrid[x, y]);
            }
        }
        //Apply texture
        terrainTypeTexture.Apply();

        Texture2D currentTexture;

        // Set texture of perlinNoiseMiniMap.
        perlinNoiseMaterial = new Material(Shader.Find("Unlit/Texture"));
        perlinNoiseMiniMap = GameObject.Find("perlinNoiseMiniMap").GetComponent<Image>();
        perlinNoiseMiniMap.material = perlinNoiseMaterial;

        currentTexture = GenerateTexture();
        perlinNoiseMiniMap.material.mainTexture = currentTexture;
        saveTextureAsPNG(currentTexture, "perlinNoise");

        // Set texture of perlinNoiseMiniMap.
        perlinNoiseMaterial = new Material(Shader.Find("Unlit/Texture"));
        perlinNoiseMiniMap = GameObject.Find("mountainBaseMiniMap").GetComponent<Image>();
        perlinNoiseMiniMap.material = perlinNoiseMaterial;

        currentTexture = GenerateTextureMountainProcess();
        perlinNoiseMiniMap.material.mainTexture = currentTexture;
        saveTextureAsPNG(currentTexture, "mountainConnections");
    
        // Set texture of perlinNoiseMiniMap.
        perlinNoiseMaterial = new Material(Shader.Find("Unlit/Texture"));
        perlinNoiseMiniMap = GameObject.Find("mountainPointsMiniMap").GetComponent<Image>();
        perlinNoiseMiniMap.material = perlinNoiseMaterial;

        currentTexture = GenerateTextureMountainPoints();
        perlinNoiseMiniMap.material.mainTexture = currentTexture;
        saveTextureAsPNG(currentTexture, "mountainPoints");

        // Set texture of terrainTypeMiniMap.
        terrainTypeMaterial = new Material(Shader.Find("Unlit/Texture"));
        terrainTypeMiniMap = GameObject.Find("terrainTypeMiniMap").GetComponent<Image>();
        terrainTypeMiniMap.material = terrainTypeMaterial;

        currentTexture = GetTerrainTypeTexture();
        terrainTypeMiniMap.material.mainTexture = currentTexture;
        saveTextureAsPNG(currentTexture, "terrainTypeGridFinal");


    }

    void CreateHeightArray()
    {
        CreateMountains();
        CreateMultiLayeredNoise();

        //Merge noise functions
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {

           
                perlinNoiseArrayFinalized[x, y] = perlinNoiseArray[x, y] + (((perlinNoiseArrayMBasePass[x, y] / 2.0f) * perlinNoiseArrayMPass[x, y]) * terrainTypeGrid[x, y].a);

                //}

            }
        }

    }

    //save texture to file
    public static void saveTextureAsPNG(Texture2D texture, string name)
    {
        byte[] bytes = texture.EncodeToPNG();

        var dirPath = Application.dataPath + "/../SaveImages/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        System.IO.File.WriteAllBytes(dirPath + name + ".png", bytes);
        //Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }

    Texture2D GenerateTextureMountainPoints()
    {
        Texture2D texture = new Texture2D((int)SIZE_FULL, (int)SIZE_FULL);

        float noiseValue = 0.0f;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseValue = perlinNoiseArrayMPoints[x, y];

                Color color = new Color(noiseValue, noiseValue, noiseValue);

                texture.SetPixel(x, y, color);

            }
        }

        //Apply texture
        texture.Apply();

        return texture;
    }

    Texture2D GenerateTextureMountainProcess()
    {
        Texture2D texture = new Texture2D((int)SIZE_FULL, (int)SIZE_FULL);

        float noiseValue = 0.0f;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseValue = perlinNoiseArrayMPass[x, y];

                Color color = new Color(noiseValue, noiseValue, noiseValue);

                texture.SetPixel(x, y, color);

            }
        }

        //Apply texture
        texture.Apply();

        return texture;
    }

    Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D((int)SIZE_FULL, (int)SIZE_FULL);

        float noiseValue = 0.0f;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseValue = perlinNoiseArrayFinalized[x, y];

                Color color = new Color(noiseValue, noiseValue, noiseValue);

                texture.SetPixel(x, y, color);

            }
        }

        //Apply texture
        texture.Apply();

        return texture;
    }

    void GenerateTerrain()
    {
        //print ("Generating Terrain...");

        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {

                perlinNoiseArrayFinalizedFlipped[y, x] = perlinNoiseArrayFinalized[x, y];

            }
        }

        //Set terrain height map
        terrainComponent = terrainObject.GetComponent<Terrain>();
        terrainComponent.Flush();
        terrainComponent.terrainData.SetHeights(0, 0, perlinNoiseArrayFinalizedFlipped);

        ClearNoise(perlinNoiseArray);
        ClearNoise(perlinNoiseArrayCell);
        ClearNoise(perlinNoiseArrayMPass);
        ClearNoise(perlinNoiseArrayFinalized);

    }


    //Creates and merges perlin noise for height variance
    void CreateMultiLayeredNoise()
    {

        float frequency = 2.0f;
        float noiseValue = 0.0f;

        //6 layers of noise
        for (int numLayers = 0; numLayers < 6; numLayers++)
        {

            float xOffset = Random.Range(0.0f, 99999.0f);
            float yOffset = Random.Range(0.0f, 99999.0f);

            if (numLayers == 0)
            {
                frequency = Random.Range(0.75f, 1.5f);

            }

            frequency += (numLayers * 4) + 1;

            for (int y = 0; y < SIZE_FULL; y++)
            {
                for (int x = 0; x < SIZE_FULL; x++)
                {

                    float xCoord = ((float)x / SIZE_FULL) * frequency + xOffset;
                    float yCoord = ((float)y / SIZE_FULL) * frequency + yOffset;

                    if (numLayers == 0)
                    {
                        perlinNoiseArray[x, y] += (Mathf.PerlinNoise(xCoord, yCoord) / 5.0f);

                    }
                    else
                    {
                        perlinNoiseArray[x, y] += (Mathf.PerlinNoise(xCoord, yCoord) / ((float)numLayers * (10.0f) + 40.0f));

                    }

                }
            }
        }

    }

    void CreateMountains()
    {

        float frequency = 100.0f;
        float frequency2 = 10.0f;

        float noiseValue = 0.0f;
        float noiseValue2 = 0.0f;

        float mountainXOffset = Random.Range(0.0f, 99999.0f);
        float mountainXOffset2 = Random.Range(0.0f, 99999.0f);
        float mountainYOffset = Random.Range(0.0f, 99999.0f);
        float mountainYOffset2 = Random.Range(0.0f, 99999.0f);

        List<int> xPointsMountainPeak = new List<int>();
        List<int> yPointsMountainPeak = new List<int>();
        //List<int> pointNumMountainPeak = new List<int> ();

        List<int> pointGroupMountainPeak = new List<int>();

        int numPoints = 0;

        //frequency = Random.Range(6.0f, 8.0f);
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {

                float xCoord = ((float)x / SIZE_FULL) * frequency + mountainXOffset;
                float xCoord2 = ((float)x / SIZE_FULL) * frequency2 + mountainXOffset2;
                float yCoord = ((float)y / SIZE_FULL) * frequency + mountainYOffset;
                float yCoord2 = ((float)y / SIZE_FULL) * frequency2 + mountainYOffset2;

                //perlinNoiseArrayMPass[x, y] += (Mathf.PerlinNoise(xCoord, yCoord));

                if ((Mathf.PerlinNoise(xCoord, yCoord) >= 0.70f))
                {

                    if (numPoints == 0 && terrainTypeGrid[x, y].a == 1.0f)
                    {

                        xPointsMountainPeak.Add(x);
                        yPointsMountainPeak.Add(y);

                        perlinNoiseArrayMPoints[x, y] = 1.0f;

                        //Increment number of points
                        numPoints += 1;
                    }
                    else if (numPoints > 0 && Mathf.Abs(x - xPointsMountainPeak[numPoints - 1]) > 15 && Mathf.Abs(y - yPointsMountainPeak[numPoints - 1]) > 15)
                    {

                        if (terrainTypeGrid[x, y].a == 1.0f)
                        {
                            xPointsMountainPeak.Add(x);
                            yPointsMountainPeak.Add(y);

                            perlinNoiseArrayMPoints[x, y] = 1.0f;

                            //Increment number of points
                            numPoints += 1;
                        }

                    }

                }
                else
                {
                    perlinNoiseArrayMPass[x, y] = 0;
                    perlinNoiseArrayMPoints[x, y] = 0.0f;

                    //perlinNoiseArray[x, y] += (Mathf.PerlinNoise(xCoord, yCoord) / ((float)numLayers * (2.0f) + 20.0f));

                }

                if (Mathf.PerlinNoise(xCoord2, yCoord2) >= 0.01)
                {
                    perlinNoiseArrayMBasePass[x, y] = Mathf.PerlinNoise(xCoord2, yCoord2);

                }
                else
                {
                    perlinNoiseArrayMBasePass[x, y] = 0;

                }

            }
        }

        float tempx;
        float tempy;
        float previousSmallestDist = 2000;
        float previousSmallestDist2 = 2000;
        int smallestPoint = 0;
        int smallestPoint2 = 0;
        List<int> maxNumPoints = new List<int>();

        for (int i = 0; i < xPointsMountainPeak.Count; i++)
        {
            for (int j = 0; j < xPointsMountainPeak.Count; j++)
            {
                tempx = Mathf.Abs(xPointsMountainPeak[j] - xPointsMountainPeak[i]);
                tempy = Mathf.Abs(yPointsMountainPeak[j] - yPointsMountainPeak[i]);

                if (Mathf.Abs(tempx + tempy) < previousSmallestDist && j != i)
                {
                    //print("Smallest Distance: " + Mathf.Abs(tempx + tempy));

                    previousSmallestDist = Mathf.Abs(tempx + tempy);
                    smallestPoint = j;
                }
                else if (Mathf.Abs(tempx + tempy) < previousSmallestDist2 && j != i)
                {
                    //print("2nd Smallest Distance: " + Mathf.Abs(tempx + tempy));

                    previousSmallestDist2 = Mathf.Abs(tempx + tempy);
                    smallestPoint2 = j;

                }

            }

            //print(i);
            connectMountainPeaks(xPointsMountainPeak[i], yPointsMountainPeak[i], xPointsMountainPeak[smallestPoint], yPointsMountainPeak[smallestPoint]);

            if (previousSmallestDist2 > previousSmallestDist * 2)
            {
                connectMountainPeaks(xPointsMountainPeak[i], yPointsMountainPeak[i], xPointsMountainPeak[smallestPoint2], yPointsMountainPeak[smallestPoint2]);
            }

            previousSmallestDist = 2000;
            previousSmallestDist2 = 2000;
            smallestPoint = 0;
            smallestPoint2 = 0;
        }

    }

    void connectMountainPeaks(int x1, int y1, int x2, int y2)
    {
        //Normal Vector Variables
        float dx = 0;
        float dy = 0;

        float normal1x = 0;
        float normal1y = 0;
        float normal2x = 0;
        float normal2y = 0;

        //Take points and make a line
        dx = x2 - x1;
        dy = y2 - y1;

        //print("X1 and Y1");
        //print(x1 + " " + y1);

        //print("X2 and Y2");
        //print(x2 + " " + y2);

        //Make normals
        //Point 1
        normal1x = -dy;
        normal1y = dx;

        //Point 2
        normal2x = dy;
        normal2y = -dx;

        //Difference Vector of Normal Vector(Red Line)
        float normalDX = normal2x - normal1x;
        float normalDY = normal2y - normal1y;

        float steps = 0;
        float stepsNormal = 0;
        //Unit vector kinda(Blue Line)
        if (Mathf.Abs(dx) > Mathf.Abs(dy)) //No clue what this is doing right now
        {
            steps = Mathf.Abs(dx);
        }
        else
        {
            steps = Mathf.Abs(dy);
        }

        //Unit vector kinda(Blue Line2)
        if (Mathf.Abs(normalDX) > Mathf.Abs(normalDY)) //No clue what this is doing right now
        {
            stepsNormal = Mathf.Abs(normalDX);
        }
        else
        {
            stepsNormal = Mathf.Abs(normalDY);
        }

        float moveX = dx / steps;
        float moveY = dy / steps;

        float moveXNormal = normalDX / stepsNormal;
        float moveYNormal = normalDY / stepsNormal;

        //Starting position
        float lineGradientX = x1;
        float lineGradientY = y1;

        //Starting position of Normal
        float lineGradientXNormal = x1;
        float lineGradientYNormal = y1;

        float size = 125;

        for (int i = 0; i <= steps; i++)
        {

            //Center line
            for (float j = size; j > 0; j--)
            {

                if (perlinNoiseArrayMPass[(int)lineGradientX, (int)lineGradientY] < 1.0f)
                {
                    perlinNoiseArrayMPass[(int)lineGradientX, (int)lineGradientY] = 1.0f;
                }
            }

            try
            {

                for (float j = size; j > 0; j--)
                {
                    //place pixel to the left and right of line perpendicularly
                    lineGradientXNormal += moveXNormal;
                    lineGradientYNormal += moveYNormal;

                    if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal] <= j / size)
                    {
                        perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal] = j / size;
                    }

                    if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 1] <= j / size)
                    {
                        perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 1] = j / size;
                    }

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 2] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 2] = j / size;
                    //}

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 3] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 3] = j / size;
                    //}

                    if (perlinNoiseArrayMPass[(int)lineGradientXNormal + 1, (int)lineGradientYNormal] <= j / size)
                    {
                        perlinNoiseArrayMPass[(int)lineGradientXNormal + 1, (int)lineGradientYNormal] = j / size;
                    }

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal + 2, (int)lineGradientYNormal] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal + 2, (int)lineGradientYNormal] = j / size;
                    //}

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal + 3, (int)lineGradientYNormal] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal + 3, (int)lineGradientYNormal] = j / size;
                    //}

                }

            }
            catch (Exception e)
            {
                //  print("point outside of map");

            }
            lineGradientXNormal = lineGradientX;
            lineGradientYNormal = lineGradientY;
            try
            {

                for (float j = size; j > 0; j--)
                {
                    //place pixel to the left and right of line perpendicularly
                    lineGradientXNormal -= moveXNormal;
                    lineGradientYNormal -= moveYNormal;

                    if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal] <= j / size)
                    {
                        perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal] = j / size;
                    }

                    if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 1] <= j / size)
                    {
                        perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 1] = j / size;
                    }

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 2] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 2] = j / size;
                    //}

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 3] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal, (int)lineGradientYNormal + 3] = j / size;
                    //}

                    if (perlinNoiseArrayMPass[(int)lineGradientXNormal + 1, (int)lineGradientYNormal] <= j / size)
                    {
                        perlinNoiseArrayMPass[(int)lineGradientXNormal + 1, (int)lineGradientYNormal] = j / size;
                    }

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal + 2, (int)lineGradientYNormal] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal + 2, (int)lineGradientYNormal] = j / size;
                    //}

                    //if (perlinNoiseArrayMPass[(int)lineGradientXNormal + 3, (int)lineGradientYNormal] <= j / size)
                    //{
                    //    perlinNoiseArrayMPass[(int)lineGradientXNormal + 3, (int)lineGradientYNormal] = j / size;
                    //}

                }
            }
            catch (Exception e)
            {
                // print("point outside of map");

            }

            //Move to next pixel
            lineGradientX += moveX;
            lineGradientY += moveY;

            lineGradientXNormal = lineGradientX;
            lineGradientYNormal = lineGradientY;
        }

        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {

                if (Mathf.Sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1)) < 175)
                {
                    if (perlinNoiseArrayMPass[x, y] < 1.0f - Mathf.Sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1)) / 175)
                    {
                        perlinNoiseArrayMPass[x, y] = 1.0f - Mathf.Sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1)) / 175;

                    }
                }

                if (Mathf.Sqrt((x - x2) * (x - x2) + (y - y2) * (y - y2)) < 175)
                {
                    if (perlinNoiseArrayMPass[x, y] < 1.0f - Mathf.Sqrt((x - x2) * (x - x2) + (y - y2) * (y - y2)) / 175)
                    {
                        perlinNoiseArrayMPass[x, y] = 1.0f - Mathf.Sqrt((x - x2) * (x - x2) + (y - y2) * (y - y2)) / 175;

                    }

                }

            }
        }
    }

    /* ------------------------------------------------------------------ */
    /* TERRAIN TYPE MANIPULATION BEFORE BLUR APPLICATION */
    /* The function implementations are ordered below as they are called. */
    /* ------------------------------------------------------------------ */

    // Alex's work
    // Creates Terrain Type Grid from the image file for inputMapTexture.
    void InitTerrainTypeGrid()
    {

        inputMapGrid = new Color[inputMapTextureDim, inputMapTextureDim];

        ColorCorrectInputMap();
        
        SetupTerrainTypeGridUntextured();

        FillAndWarpHorizBorders();
        WarpVertBorders();

        float timeStart = Time.realtimeSinceStartup;
        ApplyBlurPasses();
        print("Seconds to execute blur: " + (Time.realtimeSinceStartup - timeStart));
    }

    // This basically has to exist until inputs that have *only* 4 colours are given. Currently, the provided example images do not have only 4 colours.
    void ColorCorrectInputMap()
    {
        for (int i = 0; i < inputMapTexture.width; i++)
        {
            for (int j = 0; j < inputMapTexture.height; j++)
            {

                if (IsGreyMountain(inputMapTexture.GetPixel(i, j)))
                {
                    inputMapGrid[i, j] = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                }
                else if (IsBlueWater(inputMapTexture.GetPixel(i, j)))
                {
                    inputMapGrid[i, j] = new Color(0.0f, 0.0f, 1.0f, 0.0f);
                }
                else if (inputMapTexture.GetPixel(i, j).g > greenGrasslandTolerance)
                {
                    inputMapGrid[i, j] = new Color(0.0f, 1.0f, 0.0f, 0.0f);
                }
                // Currently, if the input pixel is not detected as Grassland, Water, or Mountain it will default to Forest.
                else
                {
                    inputMapGrid[i, j] = new Color(1.0f, 0.0f, 0.0f, 0.0f);
                }
            }
        }
    }

    // Check if the input Color in question is a Mountain as determined by greyMountainTolerance.
    bool IsGreyMountain(Color inputColor)
    {
        float lowestRGBValue = Mathf.Min(inputColor.r, inputColor.g, inputColor.b);
        return ((inputColor.r - lowestRGBValue < greyMountainTolerance) && (inputColor.g - lowestRGBValue < greyMountainTolerance) && (inputColor.b - lowestRGBValue < greyMountainTolerance));
    }

    // Check if the input Color in question is Water as determined by blueWaterTolerance.
    bool IsBlueWater(Color inputColor)
    {
        return ((inputColor.b - inputColor.r > blueWaterTolerance) && (inputColor.b - inputColor.g > blueWaterTolerance));
    }

    // terrainTypeGrid is filled with a value outside of the range of 0.0f to 1.0f to act as a flag for when a cell has not been assigned a terrain type.
    void SetupTerrainTypeGridUntextured()
    {
        for (int i = 0; i < SIZE_FULL; i++)
        {
            for (int n = 0; n < SIZE_FULL; n++)
            {
                terrainTypeGrid[i, n] = new Color(9.9f, 9.9f, 9.9f, 9.9f);
            }
        }
        //GenerateTTGOneImage(terrainTypeBeforeFillTexture, "TTGBeforeFill");
    }

    // Fill the terrainTypeGrid with values corresponding to the inputMapGrid and warp the shape of the horizontal borders between the terrain types.
    void FillAndWarpHorizBorders()
    {

        Color eyedropperColour;
        inputMapTextureDim = inputMapTexture.height;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++)
        {
            float xCoord = Random.Range(0.0f, 99999.0f);
            float yCoord = Random.Range(0.0f, 99999.0f);
            float offset = 0;
            for (int x = 0; x < SIZE_FULL; x++)
            {
                if (terrainTypeGrid[x, y].r == 9.9f)
                {
                    float startPerlinWalkCoord = (Mathf.PerlinNoise(xCoord + offset, yCoord));
                    offset += 0.01f;
                    // Horizontal Border Warping only if within bounds of terrainTypeGrid, of size SIZE_FULL * SIZE_FULL, and is determined to be a "border" cell.
                    if (y % (int)(SIZE_FULL / inputMapTextureDim) == 0 && y > 0 && y < SIZE_FULL - inputMapTextureDim)
                    {
                        int shiftValue = getBorderShiftValue(startPerlinWalkCoord);
                        // This If/Else determines which direction a border is warped; up or down. It also performs the warp terrain type assignment.
                        if (shiftValue > 0)
                        {
                            // Sample a colour from the inputMapTexture
                            eyedropperColour = inputMapGrid[Mathf.Min((int)(x / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min((int)(y / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1) - 1];
                            for (int i = 0; i < shiftValue; i++)
                            {
                                if ((y + i) < SIZE_FULL)
                                {
                                    terrainTypeGrid[x, y + i] = eyedropperColour;
                                    //terrainTypeGrid[x, y + i] = Color.yellow;
                                }
                            }
                        }
                        else
                        {
                            // Sample a colour from the inputMapTexture
                            eyedropperColour = inputMapGrid[Mathf.Min((int)(x / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min((int)(y / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                            for (int i = shiftValue; i <= 0; i++)
                            {
                                if ((y + i) > 0)
                                {
                                    terrainTypeGrid[x, y + i] = eyedropperColour;
                                    //terrainTypeGrid[x, y + i] = Color.yellow;
                                }
                            }
                        }
                    }
                    // Not on a border colouring
                    else
                    {
                        // Sample a colour from the inputMapTexture
                        eyedropperColour = inputMapGrid[Mathf.Min((int)(x / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min((int)(y / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                        terrainTypeGrid[x, y] = eyedropperColour;
                    }
                }
            }

        }
        //GenerateTTGOneImage(terrainTypeFilledAndHorizWarpTexture, "TTGFilledAndHorizWarp");
    }

    // Warp the shape of the vertical borders between the terrain types.
    void WarpVertBorders()
    {
        Color eyedropperColour;
        inputMapTextureDim = inputMapTexture.height;

        //Create the texture
        for (int x = 0; x < SIZE_FULL; x++)
        {
            float xCoord = Random.Range(0.0f, 99999.0f);
            float yCoord = Random.Range(0.0f, 99999.0f);
            float offset = 0;
            for (int y = 0; y < SIZE_FULL; y++)
            {
                float startPerlinWalkCoord = (Mathf.PerlinNoise(xCoord + offset, yCoord));
                offset += 0.01f;
                // Vertical Border Warping only if within bounds of terrainTypeGrid, of size SIZE_FULL * SIZE_FULL, and is determined to be a "border" cell.
                if (x % (int)(SIZE_FULL / inputMapTextureDim) == 0 && x > 0 && x < SIZE_FULL - inputMapTextureDim)
                {
                    int shiftValue = getBorderShiftValue(startPerlinWalkCoord);
                    // This If/Else determines which direction a border is warped; right or left. It also performs the warp terrain type assignment.
                    if (shiftValue > 0)
                    {
                        // Sample a colour from the inputMapTexture
                        eyedropperColour = inputMapGrid[Mathf.Min((int)(x / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1) - 1, Mathf.Min((int)(y / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                        for (int i = 0; i < shiftValue; i++)
                        {
                            if ((x + i) < SIZE_FULL)
                            {
                                terrainTypeGrid[x + i, y] = eyedropperColour;
                                //terrainTypeGrid[x + i, y] = Color.yellow;
                            }
                        }
                    }
                    else
                    {
                        // Sample a colour from the inputMapTexture
                        eyedropperColour = inputMapGrid[Mathf.Min((int)(x / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min((int)(y / (int)(SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                        for (int i = shiftValue; i <= 0; i++)
                        {
                            if ((x + i) > 0)
                            {
                                terrainTypeGrid[x + i, y] = eyedropperColour;
                                //terrainTypeGrid[x + i, y] = Color.yellow;
                            }
                        }
                    }
                }
            }
        }
        GenerateTTGOneImage(terrainTypeAfterVertWarpTexture, "TTGAfterVertWarp");
    }

    int getBorderShiftValue(float perlinShiftValue)
    {
        return (int)((perlinShiftValue - 0.5f) * terrainBorderShiftMod);
    }

    /* ------------------------------------------------------------------ */
    /* CALL FUNCTIONS TO APPLY DIFFERENT BLUR ALGORITHMS */
    /* ------------------------------------------------------------------ */

    // Calls the function that iterates over all terrain type cells BLUR_PASSES times.
    void ApplyBlurPasses()
    {
        if (doPseudoGaussBlur)
        {
            PseudoGaussBlur();
        }

        // If trueGaussBlurKernelDimNxN is 0 or less, then we shouldn't call the gauss blurring functions.
        if (trueGaussBlurKernelDimNxN > 0 && doGaussAndOrThenBoxBlur[0])
        {
            CreateConvBlurKernel();
            for (int i = 0; i < trueGaussNumPasses; i++)
            {
                terrainTypeGrid = TrueGaussBlurTerrainBorders();
            }
        }

        // If boxBlurKernelDimNxN is 0 or less, then we shouldn't call the box blurring function.
        if (boxBlurKernelDimNxN > 0 && doGaussAndOrThenBoxBlur[1])
        {
            for (int i = 0; i < boxBlurNumPasses; i++)
            {
                terrainTypeGrid = BoxBlurTerrainBorders();
            }
        }
    }

    /* ------------------------------------------------------------------ */
    /* TRUE GAUSSIAN AND BOX BLUR FUNCTIONS */
    /* The function implementations are ordered below as they are called. */
    /* ------------------------------------------------------------------ */

    // Builds a kernel of size trueGaussBlurKernelDimNxN * trueGaussBlurKernelDimNxN using the formula for Gaussian kernels.
    void CreateConvBlurKernel()
    {
        trueGaussBlurKernel = new float[trueGaussBlurKernelDimNxN, trueGaussBlurKernelDimNxN];
        float sumKernel = 0.0f;
        for (int x = -((trueGaussBlurKernelDimNxN - 1) / 2); x < ((trueGaussBlurKernelDimNxN + 1) / 2); x++)
        {
            for (int y = -((trueGaussBlurKernelDimNxN - 1) / 2); y < ((trueGaussBlurKernelDimNxN + 1) / 2); y++)
            {
                trueGaussBlurKernel[x + ((trueGaussBlurKernelDimNxN - 1) / 2), y + ((trueGaussBlurKernelDimNxN - 1) / 2)] = GetGaussianKernelValue(System.Math.Abs(x), System.Math.Abs(y));
                sumKernel += trueGaussBlurKernel[x + ((trueGaussBlurKernelDimNxN - 1) / 2), y + ((trueGaussBlurKernelDimNxN - 1) / 2)];
            }
        }
        float kernelMustBeSummedToOne = 1.0f / sumKernel;
        for (int x = 0; x < trueGaussBlurKernelDimNxN; x++)
        {
            for (int y = 0; y < trueGaussBlurKernelDimNxN; y++)
            {
                trueGaussBlurKernel[x, y] = trueGaussBlurKernel[x, y] * kernelMustBeSummedToOne;
            }
        }
    }

    // Given an offset location from the centre cell of the kernel, return the value for the offset location's cell using the Gaussian formula.
    float GetGaussianKernelValue(float xOffset, float yOffset)
    {
        return (float)(1.0f / (2.0f * Mathf.PI * Mathf.Pow(trueGaussSigma, 2.0f))) * Mathf.Pow((float)System.Math.E, -((Mathf.Pow(xOffset, 2.0f) + Mathf.Pow(yOffset, 2.0f)) / (2.0f * Mathf.PI * Mathf.Pow(trueGaussSigma, 2.0f))));
    }

    // Iterates over all terrain type cells calling the Gaussian blur function for a single cell's kernel.
    Color[, ] TrueGaussBlurTerrainBorders()
    {
        Color[, ] blurTerrain = new Color[(int)SIZE_FULL, (int)SIZE_FULL];
        for (int x = 0; x < SIZE_FULL; x++)
        {
            for (int y = 0; y < SIZE_FULL; y++)
            {
                blurTerrain[x, y] = ApplyGaussConvKernelToCell(x, y);
            }
        }
        return blurTerrain;
    }

    // Iterates over all terrain type cells calling the Box blur function for a single cell's kernel.
    Color[, ] BoxBlurTerrainBorders()
    {
        Color[, ] blurTerrain = new Color[(int)SIZE_FULL, (int)SIZE_FULL];
        for (int x = 0; x < SIZE_FULL; x++)
        {
            for (int y = 0; y < SIZE_FULL; y++)
            {
                blurTerrain[x, y] = ApplyBoxBlurConvKernelToCell(x, y);
            }
        }
        return blurTerrain;
    }

    // Returns a new Color using the weights in the built trueGaussBlurKernel[].
    Color ApplyGaussConvKernelToCell(int xCoord, int yCoord)
    {
        Color newWeight = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        int numContributors = 0;
        for (int x = -((trueGaussBlurKernelDimNxN - 1) / 2); x < ((trueGaussBlurKernelDimNxN + 1) / 2); x++)
        {
            for (int y = -((trueGaussBlurKernelDimNxN - 1) / 2); y < ((trueGaussBlurKernelDimNxN + 1) / 2); y++)
            {
                if (xCoord + x >= 0 && xCoord + x < SIZE_FULL && yCoord + y >= 0 && yCoord + y < SIZE_FULL)
                {
                    newWeight += terrainTypeGrid[xCoord + x, yCoord + y] * trueGaussBlurKernel[x + ((trueGaussBlurKernelDimNxN - 1) / 2), y + ((trueGaussBlurKernelDimNxN - 1) / 2)];
                }
            }
        }
        return newWeight;
    }

    // Returns a new Color using assumed equal weights across the kernel.
    Color ApplyBoxBlurConvKernelToCell(int xCoord, int yCoord)
    {
        Color newWeight = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        float numContributors = 0.0f;
        for (int x = -((boxBlurKernelDimNxN - 1) / 2); x < ((boxBlurKernelDimNxN + 1) / 2); x++)
        {
            for (int y = -((boxBlurKernelDimNxN - 1) / 2); y < ((boxBlurKernelDimNxN + 1) / 2); y++)
            {
                if (xCoord + x >= 0 && xCoord + x < SIZE_FULL && yCoord + y >= 0 && yCoord + y < SIZE_FULL)
                {
                    newWeight += terrainTypeGrid[xCoord + x, yCoord + y];
                    numContributors++;
                }
            }
        }
        return newWeight / numContributors;
    }

    /* ------------------------------------------------------------------ */
    /* IVAN KUTSKIR ADAPTED "GAUSSIAN" BLUR */
    /* Heavily modified and improved adaptation of Ivan Kutskir's "Fastest Gaussian Blur" article. http://blog.ivank.net/fastest-gaussian-blur.html */
    /* The function implementations are ordered below as they are called. */
    /* ------------------------------------------------------------------ */

    // Flattens terrainTypeGrid into a 1D array for the algorithm, determines the "ideal" 3 kernel sizes for the 3 passes of box blur that approximate a Gaussian blur, calls a modified box blur 3 times, then unflattens the array back to a 2D array.
    void PseudoGaussBlur()
    {
        Color[] source = Flatten2DTerrainTypeGrid(terrainTypeGrid);
        Color[] target = new Color[(int)SIZE_FULL * (int)SIZE_FULL];
        float[] boxKernelDims = GetPseudoGaussKernelDims(pseudoGaussSigma, 3.0f);
        target = PseudoGaussBlurTotal(source, target, (boxKernelDims[0] - 1.0f) / 2.0f);
        source = PseudoGaussBlurTotal(target, source, (boxKernelDims[1] - 1.0f) / 2.0f);
        target = PseudoGaussBlurTotal(source, target, (boxKernelDims[2] - 1.0f) / 2.0f);
        terrainTypeGrid = Unflatten2DTerrainTypeGrid(target);
    }

    // Takes terrainTypeGrid as a 2D array and returns it in the form of a 1D array
    Color[] Flatten2DTerrainTypeGrid(Color[, ] array2D)
    {
        Color[] flattenedArray = new Color[(int)SIZE_FULL * (int)SIZE_FULL];
        for (int i = 0; i < (int)SIZE_FULL; i++)
        {
            for (int n = 0; n < (int)SIZE_FULL; n++)
            {
                flattenedArray[i * (int)SIZE_FULL + n] = array2D[i, n];
            }
        }
        return flattenedArray;
    }

    // Given a standard deviation, sigma, returns an array of kernel sizes for the box blurs.
    float[] GetPseudoGaussKernelDims(float sigma, float numBoxes)
    {
        float wl = Mathf.Floor(Mathf.Sqrt((12.0f * sigma * sigma / numBoxes) + 1.0f));
        if (wl % 2.0f == 0.0f)
        {
            wl--;
        }
        float wu = wl + 2.0f;

        float m = Mathf.Round((12.0f * sigma * sigma - numBoxes * wl * wl - 4.0f * numBoxes * wl - 3.0f * numBoxes) / (-4.0f * wl - 4.0f));

        float[] dimOfBoxes = new float[(int)numBoxes];
        for (int i = 0; i < numBoxes; i++)
        {
            if (i < m)
            {
                dimOfBoxes[i] = wl;
            }
            else
            {
                dimOfBoxes[i] = wu;
            }
        }
        return dimOfBoxes;
    }

    // Box blur is seperable, so we can blur horizontally then vertically and have the same result as if they were done at the same time.
    Color[] PseudoGaussBlurTotal(Color[] sourceArray, Color[] targetArray, float kernelRadius)
    {
        targetArray = sourceArray;
        PseudoGaussBlurHor(targetArray, sourceArray, kernelRadius);
        PseudoGaussBlurVert(sourceArray, targetArray, kernelRadius);
        return targetArray;
    }

    // Blurs across the sourceArray horizontally placing the new values in targetArray.
    void PseudoGaussBlurHor(Color[] sourceArray, Color[] targetArray, float kernelRadius)
    {
        float iArr = 1 / (2.0f * kernelRadius + 1);
        for (int i = 0; i < (int)SIZE_FULL; i++)
        {
            float ti = i * SIZE_FULL;
            float li = ti;
            float ri = ti + kernelRadius;
            float fvR = sourceArray[(int)ti].r;
            float fvG = sourceArray[(int)ti].g;
            float fvB = sourceArray[(int)ti].b;
            float fvA = sourceArray[(int)ti].a;
            float lvR = sourceArray[(int)ti + (int)SIZE_FULL - 1].r;
            float lvG = sourceArray[(int)ti + (int)SIZE_FULL - 1].g;
            float lvB = sourceArray[(int)ti + (int)SIZE_FULL - 1].b;
            float lvA = sourceArray[(int)ti + (int)SIZE_FULL - 1].a;
            float valueR = (kernelRadius + 1) * fvR;
            float valueG = (kernelRadius + 1) * fvG;
            float valueB = (kernelRadius + 1) * fvB;
            float valueA = (kernelRadius + 1) * fvA;
            for (int j = 0; j < kernelRadius; j++)
            {
                valueR += sourceArray[(int)ti + j].r;
                valueG += sourceArray[(int)ti + j].g;
                valueB += sourceArray[(int)ti + j].b;
                valueA += sourceArray[(int)ti + j].a;
            }
            for (int j = 0; j <= kernelRadius; j++)
            {
                valueR += sourceArray[(int)ri].r - fvR;
                valueG += sourceArray[(int)ri].g - fvG;
                valueB += sourceArray[(int)ri].b - fvB;
                valueA += sourceArray[(int)ri++].a - fvA;
                targetArray[(int)ti].r = valueR * iArr;
                targetArray[(int)ti].g = valueG * iArr;
                targetArray[(int)ti].b = valueB * iArr;
                targetArray[(int)ti++].a = valueA * iArr;
            }
            for (int j = (int)kernelRadius + 1; j < SIZE_FULL - kernelRadius; j++)
            {
                valueR += sourceArray[(int)ri].r - sourceArray[(int)li].r;
                valueG += sourceArray[(int)ri].g - sourceArray[(int)li].g;
                valueB += sourceArray[(int)ri].b - sourceArray[(int)li].b;
                valueA += sourceArray[(int)ri++].a - sourceArray[(int)li++].a;
                targetArray[(int)ti].r = valueR * iArr;
                targetArray[(int)ti].g = valueG * iArr;
                targetArray[(int)ti].b = valueB * iArr;
                targetArray[(int)ti++].a = valueA * iArr;
            }
            for (int j = (int)SIZE_FULL - (int)kernelRadius; j < SIZE_FULL; j++)
            {
                valueR += lvR - sourceArray[(int)li].r;
                valueG += lvG - sourceArray[(int)li].g;
                valueB += lvB - sourceArray[(int)li].b;
                valueA += lvA - sourceArray[(int)li++].a;
                targetArray[(int)ti].r = valueR * iArr;
                targetArray[(int)ti].g = valueG * iArr;
                targetArray[(int)ti].b = valueB * iArr;
                targetArray[(int)ti++].a = valueA * iArr;
            }
        }
    }

    // Blurs across the sourceArray vertically placing the new values in targetArray.
    void PseudoGaussBlurVert(Color[] sourceArray, Color[] targetArray, float kernelRadius)
    {
        float iArr = 1.0f / (2.0f * kernelRadius + 1);
        for (int i = 0; i < (int)SIZE_FULL; i++)
        {
            float ti = i;
            float li = ti;
            float ri = ti + kernelRadius * SIZE_FULL;
            float fvR = sourceArray[(int)ti].r;
            float fvG = sourceArray[(int)ti].g;
            float fvB = sourceArray[(int)ti].b;
            float fvA = sourceArray[(int)ti].a;
            float lvR = sourceArray[(int)ti + (int)SIZE_FULL * ((int)SIZE_FULL - 1)].r;
            float lvG = sourceArray[(int)ti + (int)SIZE_FULL * ((int)SIZE_FULL - 1)].g;
            float lvB = sourceArray[(int)ti + (int)SIZE_FULL * ((int)SIZE_FULL - 1)].b;
            float lvA = sourceArray[(int)ti + (int)SIZE_FULL * ((int)SIZE_FULL - 1)].a;
            float valueR = (kernelRadius + 1) * fvR;
            float valueG = (kernelRadius + 1) * fvG;
            float valueB = (kernelRadius + 1) * fvB;
            float valueA = (kernelRadius + 1) * fvA;
            for (int j = 0; j < kernelRadius; j++)
            {
                valueR += sourceArray[(int)ti + j * (int)SIZE_FULL].r;
                valueG += sourceArray[(int)ti + j * (int)SIZE_FULL].g;
                valueB += sourceArray[(int)ti + j * (int)SIZE_FULL].b;
                valueA += sourceArray[(int)ti + j * (int)SIZE_FULL].a;
            }
            for (int j = 0; j <= kernelRadius; j++)
            {
                valueR += sourceArray[(int)ri].r - fvR;
                valueG += sourceArray[(int)ri].g - fvG;
                valueB += sourceArray[(int)ri].b - fvB;
                valueA += sourceArray[(int)ri].a - fvA;
                targetArray[(int)ti].r = valueR * iArr;
                targetArray[(int)ti].g = valueG * iArr;
                targetArray[(int)ti].b = valueB * iArr;
                targetArray[(int)ti].a = valueA * iArr;
                ri += SIZE_FULL;
                ti += SIZE_FULL;
            }
            for (int j = (int)kernelRadius + 1; j < (int)SIZE_FULL - kernelRadius; j++)
            {
                valueR += sourceArray[(int)ri].r - sourceArray[(int)li].r;
                valueG += sourceArray[(int)ri].g - sourceArray[(int)li].g;
                valueB += sourceArray[(int)ri].b - sourceArray[(int)li].b;
                valueA += sourceArray[(int)ri].a - sourceArray[(int)li].a;
                targetArray[(int)ti].r = valueR * iArr;
                targetArray[(int)ti].g = valueG * iArr;
                targetArray[(int)ti].b = valueB * iArr;
                targetArray[(int)ti].a = valueA * iArr;
                li += SIZE_FULL;
                ri += SIZE_FULL;
                ti += SIZE_FULL;
            }
            for (int j = (int)SIZE_FULL - (int)kernelRadius; j < (int)SIZE_FULL; j++)
            {
                valueR += lvR - sourceArray[(int)li].r;
                valueG += lvG - sourceArray[(int)li].g;
                valueB += lvB - sourceArray[(int)li].b;
                valueA += lvA - sourceArray[(int)li].a;
                targetArray[(int)ti].r = valueR * iArr;
                targetArray[(int)ti].g = valueG * iArr;
                targetArray[(int)ti].b = valueB * iArr;
                targetArray[(int)ti].a = valueA * iArr;
                li += SIZE_FULL;
                ti += SIZE_FULL;
            }
        }
    }

    // Takes terrainTypeGrid as a 1D array and returns it in the form of a 2D array while correcting for negative values coming from the algorithms margin of error.
    Color[, ] Unflatten2DTerrainTypeGrid(Color[] array1D)
    {
        Color[, ] unflattenedArray = new Color[(int)SIZE_FULL, (int)SIZE_FULL];
        for (int i = 0; i < (int)SIZE_FULL; i++)
        {
            for (int n = 0; n < (int)SIZE_FULL; n++)
            {
                unflattenedArray[i, n] = CorrectBlurredColour(array1D[i * (int)SIZE_FULL + n]);
            }
        }
        return unflattenedArray;
    }

    // Ensures no negative values are present in the Color type and that all weights sum to 1.
    Color CorrectBlurredColour(Color inputColor)
    {
        float sum = 0.0f;
        for (int i = 0; i < 4; i++)
        {
            if (inputColor[i] > 0.0f)
            {
                sum += inputColor[i];
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if (inputColor[i] > 0.0f)
            {
                inputColor[i] = inputColor[i] / sum;
            }
            else
            {
                inputColor[i] = 0.0f;
            }
        }
        return inputColor;
    }

    /* ------------------------------------------------------------------ */
    /* 3D TERRAIN TEXTURE MODIFICATION */
    /* This code took inspiration from the link below. */
    /* https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/ */
    /* ------------------------------------------------------------------ */

    // Modify the terrain texture to include sand and snow.
    void ModifyTerrainTexture()
    {
        Texture2D terrainTexture = new Texture2D(2048, 2048);
        Texture2D terTexOrig = (Texture2D)terrainComponent.materialTemplate.mainTexture;

        // Resizing the texture from 1024 -> 2048
        for (int i = 0; i < 2048; i++)
        {
            for (int j = 0; j < 2048; j++)
            {
                terrainTexture.SetPixel(i, j, terTexOrig.GetPixel(i/2, j/2));
            }
        }

        float xCoord = Random.Range(0.0f, 99999.0f);
        float yCoord = Random.Range(0.0f, 99999.0f);
        float heightOfSandMod = (Mathf.PerlinNoise(xCoord, yCoord));
        float perlinHeightOfSnow = (Mathf.PerlinNoise(xCoord, yCoord))*perlinSnowHeightMod;
        float snowWarp = 50f; // larger values give wider waves across the edge of the snow
        float snowStretch = 20f; // large values give taller waves up and down the mountain
        float sandWarp = 2f; // larger values give wider waves across the edge of the snow
        float sandStretch = 0.4f; // large values give taller waves up and down the mountain
        float mountainThreshold = 0.6f;


        for (int y = 0; y < (int)SIZE_FULL; y++)
        {
            for (int x = 0; x < (int)SIZE_FULL; x++)
            {
                // Variables that affect the sand speckling.
                float divisor = Mathf.PerlinNoise(xCoord+x, yCoord+y)*perlinSandVariationSpecklingMod;
                float sandColModR = (Mathf.PerlinNoise(xCoord+x, yCoord+y));
                float sandColModG = (Mathf.PerlinNoise(xCoord-x, yCoord+y));
                float sandColModB = (Mathf.PerlinNoise(xCoord+x, yCoord-y));
                float sandColModA = (Mathf.PerlinNoise(xCoord-x, yCoord-y));
                int sandRAddOrSub = sandColModR <= 0.5 ? -1 : 1;
                int sandGAddOrSub = sandColModG <= 0.5 ? -1 : 1;
                int sandBAddOrSub = sandColModB <= 0.5 ? -1 : 1;
                int sandAAddOrSub = sandColModA <= 0.5 ? -1 : 1;
                bool sandIsBlackSpeck = Random.Range(0, fractionOfBlackSand) == 0 ? true : false;

                // Sample the height to check for snow.
                float height = Terrain.activeTerrain.terrainData.GetHeight(x,y);

                float sandWiggle = sandStretch * (Mathf.PerlinNoise(xCoord + ((float)x) / sandWarp, yCoord + ((float)y) / sandWarp));
                // Snow terrain recolouring
                if (height > baseSnowHeight + Mathf.PerlinNoise(xCoord + ((float)x) / snowWarp, yCoord + ((float)y) / snowWarp) * snowStretch) { 
                    terrainTexture.SetPixel(x, y, Color.white);
                } 
                // Shoreline sand terrain recolouring
                else if(terrainTypeGrid[x,y].b >= Mathf.Min(waterManager.GetMinShoreThreshold() + heightOfSandMod + sandWiggle, waterManager.GetMaxShoreThreshold())){
                    if(sandIsBlackSpeck){
                        // Black speckling that often sand possesses.
                        terrainTexture.SetPixel(x, y, Color.black);
                    } else {
                        // Sand colour being shifted is various ways
                        terrainTexture.SetPixel(x, y, new Color(149.0f/255.0f + sandColModR * sandRAddOrSub /divisor, 130.0f/255.0f + sandColModG * sandGAddOrSub /divisor, 70.0f/255.0f + sandColModB * sandBAddOrSub /divisor, 0 + sandColModA * sandAAddOrSub /divisor));
                    }
                }
                else if (terrainTypeGrid[x, y].a >= mountainThreshold)
                {
                    Color curColor = terrainTexture.GetPixel(x, y);
                    if (sandIsBlackSpeck)
                    {
                        // Black speckling that often sand possesses.
                        terrainTexture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        //Color newColor = new Color(curColor.r, curColor.b, curColor.g, curColor.a);
                        // Mountain colour being shifted is various ways
                        float blendAmount = Mathf.Lerp(0f, 0.6f, (terrainTypeGrid[x, y].a - mountainThreshold) / (1.0f - mountainThreshold));
                        terrainTexture.SetPixel(x, y, Color.Lerp(curColor, Color.gray, blendAmount));
                    }
                }
            }
        }

        terrainTexture.Apply();
        Material terrainMaterial = new Material(Shader.Find("Legacy Shaders/Bumped Specular"));
        terrainMaterial.mainTexture = terrainTexture;
        terrainComponent.materialTemplate = terrainMaterial;
    }

    /* ------------------------------------------------------------------ */
    /* GETTERS & SETTERS */
    /* ------------------------------------------------------------------ */

    public Texture2D GetTerrainTypeTexture()
    {
        return terrainTypeTexture;
    }

    public Color GetTerrainTypeGridValueAt(int x, int y)
    {
        Color copyColor = new Color(terrainTypeGrid[x, y].r, terrainTypeGrid[x, y].g, terrainTypeGrid[x, y].b, terrainTypeGrid[x, y].a);
        return copyColor;
    }

    /* BRAINSTORMING SECTION - Alex */
    /**
            0.5 threshold, no warp
            take perlin value, minus 0.5 then multiply by 1000
            if negative, shift down or left by value divided by 10 floor
            if positive, shift up or right by value divided by 10 floor
            when shifting in a direction, cells that are "behind" get changed to shifted border cell colour

            2 ways of doing the above
            1)
                we do it during initial color setup
            2)
                we do it during a second limited pass

            TO-DO List
            Review logic for blending, bell curve
            Adding additional passes for border warping with different values, David sees the straight lines as undesirable.
            Review terrain fill for the occasional index out of bounds
            Comment code
            More elegant colour correction
            Speed up and/or multiprocessing


            Box Blur, take all values, add them up, divide by number of contributors
            Gaussian Blur, Bell shape distribution centered on a pixel
            https://www.ronja-tutorials.com/2018/08/27/postprocessing-blur.html
        

            https://stackoverflow.com/questions/98359/fastest-gaussian-blur-implementation

            http://blog.ivank.net/fastest-gaussian-blur.html

            https://github.com/mdymel/superfastblur/blob/master/SuperfastBlur/GaussianBlur.cs

            https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/

            **/
}