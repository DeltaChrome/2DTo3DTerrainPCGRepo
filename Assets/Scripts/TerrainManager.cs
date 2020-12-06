using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/*  This Manager Manages all Managers :)
 *  Big Boss
 */

public class TerrainManager : MonoBehaviour {

    //Resolution of Terrain Grid & Height Map
    public const float SIZE_FULL = 2049.0f; //Resolution of the height map texture

    // inputMapTextureDim * inputMapTextureDim pixels is the size of the user defined 2D map. We expect a square texture.
    private int inputMapTextureDim = 20;
    // Our user defined and provided 2D tiled map as a texture2D
    public Texture2D inputMapTexture;
    private Color[, ] inputMapGrid;

    // Variables for the terrain type blending <-- USE THIS FOR TERRAIN COLOURS
    private Color[, ] terrainTypeGrid = new Color[(int) SIZE_FULL, (int) SIZE_FULL];
    // Modifies the degree of border warping between terrain type areas
    private int terrainBorderShiftMod = 200;

    // There is significant computational resource consumption on increasing the size of a blur, it's radius/dimensions. Consider increasing the number of passes.
    public const int boxBlurKernelDimNxN = 3;
    public const int BOX_BLUR_PASSES = 3;
    public static bool[] doGaussAndOrBoxBlur = new bool[] { false, false };
    private float[, ] gaussConvBlurKernel;
    public const int gaussBlurKernelDimNxN = 3;
    public const float sigmaWeight = 0.5f;
    public const int GAUSS_BLUR_PASSES = 3;

    // Terrain type grid texture for terrainTypeMiniMap
    private Texture2D terrainTypeTexture;
    // Variables for rendering the mini maps for perlin noise and terrsain types.
    private Image terrainTypeMiniMap;
    private Material terrainTypeMaterial;
    private Image perlinNoiseMiniMap;
    private Material perlinNoiseMaterial;

    // Perlin Noise Arrays.
    float[, ] perlinNoiseArray = new float[(int) SIZE_FULL, (int) SIZE_FULL];
    float[, ] perlinNoiseArrayMPass = new float[(int) SIZE_FULL, (int) SIZE_FULL];
    float[, ] perlinNoiseArrayMBasePass = new float[(int) SIZE_FULL, (int) SIZE_FULL];

    //To be accessed by Water Manager
    float[, ] perlinNoiseArrayFinalized = new float[(int) SIZE_FULL, (int) SIZE_FULL];

    // For Individual Terrain Cell.
    float[, ] perlinNoiseArrayCell = new float[(int) SIZE_FULL, (int) SIZE_FULL];

    // Variables to hold the scene's terrain data
    public GameObject terrainObject;
    Terrain terrainComponent;

    //Water Manager Script - Eric's Code
    public GameObject waterManagerGO;
    public WaterManager waterManager;

    //Agent Manager Script - Nader's Code
    //*Mountain Manager Script - Jacob's Code*

    // Start is called before the first frame update
    void Start () {

        terrainTypeTexture = new Texture2D ((int) SIZE_FULL, (int) SIZE_FULL);

        //Call Water Manager
        //waterManager = new GameObject ().AddComponent (typeof (WaterManager)) ///as WaterManager;
        //waterManager.name = "WaterManager";

        ClearNoise (perlinNoiseArray);
        ClearNoise (perlinNoiseArrayMPass);
        ClearNoise (perlinNoiseArrayFinalized);
        ClearNoise (perlinNoiseArrayCell);

        //Create Terrain Type Grid
        InitTerrainTypeGrid ();

        //Create Noise functions and merge them
        CreateHeightArray (); //Result is a filled perlinNoiseArrayFinalized array

        //Modify perlinNoiseArrayFinalized array for water manipulation
        //Call Water Manager
        //waterManager.Init(TerrainTypeGridTHINGY)
        //perlinNoiseArrayFinalized = waterManager.getHeights(perlinNoiseArrayFinalized)

        //Create Textures for Grids
        GenerateMapsGUI ();

        //Call Terrain creation functions
        GenerateTerrain ();

        //Call Agent Manager

    }

    // Resets noise array to hold zeros.
    void ClearNoise (float[, ] noiseArray) {
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {
                noiseArray[x, y] = 0;
            }
        }
    }

    //Generates the physical GUI
    void GenerateMapsGUI () {

        for (int x = 0; x < SIZE_FULL; x++) {
            for (int y = 0; y < SIZE_FULL; y++) {
                terrainTypeTexture.SetPixel (x, y, terrainTypeGrid[x, y]);
            }
        }
        //Apply texture
        terrainTypeTexture.Apply ();

        // Set texture of perlinNoiseMiniMap.
        perlinNoiseMaterial = new Material (Shader.Find ("Unlit/Texture"));
        perlinNoiseMiniMap = GameObject.Find ("perlinNoiseMiniMap").GetComponent<Image> ();
        perlinNoiseMiniMap.material = perlinNoiseMaterial;
        perlinNoiseMiniMap.material.mainTexture = GenerateTexture ();
        //GenerateTexture ();
        // Set texture of terrainTypeMiniMap.
        terrainTypeMaterial = new Material (Shader.Find ("Unlit/Texture"));
        terrainTypeMiniMap = GameObject.Find ("terrainTypeMiniMap").GetComponent<Image> ();
        terrainTypeMiniMap.material = terrainTypeMaterial;
        terrainTypeMiniMap.material.mainTexture = GetTerrainTypeTexture ();
    }

    void CreateHeightArray () {
        CreateMountains ();
        CreateMultiLayeredNoise ();

        //Merge noise functions
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {

                //print(terrainTypeGrid[x, y].a);

                //terrainTypeGrid
                if (terrainTypeGrid[x, y].a < perlinNoiseArray[x, y]) {
                    perlinNoiseArrayFinalized[x, y] = perlinNoiseArray[x, y];

                }
                //Probably need to replace MBasePass with the mountain terrain type grid(I think)
                //if (perlinNoiseArrayMBasePass[x, y] < perlinNoiseArray[x, y])
                //{
                //    perlinNoiseArrayFinalized[x, y] = perlinNoiseArray[x, y];

                //}
                else {

                    perlinNoiseArrayFinalized[x, y] = perlinNoiseArray[x, y] + (perlinNoiseArrayMBasePass[x, y] / 2.0f) * perlinNoiseArrayMPass[x, y];

                }

            }
        }

    }

    Texture2D GenerateTexture () {
        Texture2D texture = new Texture2D ((int) SIZE_FULL, (int) SIZE_FULL);

        float noiseValue = 0.0f;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {
                // noiseValue = perlinNoiseArray[x, y] / 2.0f;
                noiseValue = perlinNoiseArrayFinalized[x, y];

                Color color = new Color (noiseValue, noiseValue, noiseValue);

                texture.SetPixel (x, y, color);

                //Create Terrain Height Map Cell 1
                //perlinNoiseArrayCell[x, y] = noiseValue;

            }
        }

        //Apply texture
        texture.Apply ();

        return texture;
    }

    void GenerateTerrain () {
        //print ("Generating Terrain...");

        //Set terrain height map
        terrainComponent = terrainObject.GetComponent<Terrain> ();
        terrainComponent.terrainData.SetHeights (0, 0, perlinNoiseArrayFinalized);

        ClearNoise (perlinNoiseArray);
        ClearNoise (perlinNoiseArrayCell);
        ClearNoise (perlinNoiseArrayMPass);
        ClearNoise (perlinNoiseArrayFinalized);

    }

    // Update is called once per frame
    void Update () {

    }

    //Creates and merges perlin noise for height variance
    void CreateMultiLayeredNoise () {

        float frequency = 2.0f;
        float noiseValue = 0.0f;

        //6 layers of noise
        for (int numLayers = 0; numLayers < 6; numLayers++) {

            float xOffset = Random.Range (0.0f, 99999.0f);
            float yOffset = Random.Range (0.0f, 99999.0f);

            if (numLayers == 0) {
                frequency = Random.Range (0.75f, 1.5f);

            }

            frequency += (numLayers * 4) + 1;

            for (int y = 0; y < SIZE_FULL; y++) {
                for (int x = 0; x < SIZE_FULL; x++) {

                    float xCoord = ((float) x / SIZE_FULL) * frequency + xOffset;
                    float yCoord = ((float) y / SIZE_FULL) * frequency + yOffset;

                    if (numLayers == 0) {
                        perlinNoiseArray[x, y] += (Mathf.PerlinNoise (xCoord, yCoord) / 2.0f);

                    } else {
                        perlinNoiseArray[x, y] += (Mathf.PerlinNoise (xCoord, yCoord) / ((float) numLayers * (10.0f) + 20.0f));

                    }

                }
            }
        }

    }

    void CreateMountains () {

        float frequency = 100.0f;
        float frequency2 = 10.0f;

        float noiseValue = 0.0f;
        float noiseValue2 = 0.0f;

        float mountainXOffset = Random.Range (0.0f, 99999.0f);
        float mountainXOffset2 = Random.Range (0.0f, 99999.0f);
        float mountainYOffset = Random.Range (0.0f, 99999.0f);
        float mountainYOffset2 = Random.Range (0.0f, 99999.0f);

        List<int> xPointsMountainPeak = new List<int> ();
        List<int> yPointsMountainPeak = new List<int> ();
        //List<int> pointNumMountainPeak = new List<int> ();

        List<int> pointGroupMountainPeak = new List<int> ();

        int numPoints = 0;

        //frequency = Random.Range(6.0f, 8.0f);
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {

                float xCoord = ((float) x / SIZE_FULL) * frequency + mountainXOffset;
                float xCoord2 = ((float) x / SIZE_FULL) * frequency2 + mountainXOffset2;
                float yCoord = ((float) y / SIZE_FULL) * frequency + mountainYOffset;
                float yCoord2 = ((float) y / SIZE_FULL) * frequency2 + mountainYOffset2;

                //perlinNoiseArrayMPass[x, y] += (Mathf.PerlinNoise(xCoord, yCoord));

                if ((Mathf.PerlinNoise (xCoord, yCoord) >= 0.95f)) {

                    if (numPoints == 0) {

                        xPointsMountainPeak.Add (x);
                        yPointsMountainPeak.Add (y);

                        //Increment number of points
                        numPoints += 1;
                    } else if (Mathf.Abs (x - xPointsMountainPeak[numPoints - 1]) > 15 && Mathf.Abs (y - yPointsMountainPeak[numPoints - 1]) > 15) {
                        //if (Mathf.Abs (x - xPointsMountainPeak[numPoints - 1]) < 200 && Mathf.Abs (y - yPointsMountainPeak[numPoints - 1]) < 200) {
                        //print("num points 0, in loop");
                        if (terrainTypeGrid[x, y].a != 0) {
                            xPointsMountainPeak.Add (x);
                            yPointsMountainPeak.Add (y);
                            //Increment number of points
                            numPoints += 1;
                        }

                        //}

                    }
                    //}
                } else {
                    perlinNoiseArrayMPass[x, y] = 0;
                    //perlinNoiseArray[x, y] += (Mathf.PerlinNoise(xCoord, yCoord) / ((float)numLayers * (2.0f) + 20.0f));

                }

                if (Mathf.PerlinNoise (xCoord2, yCoord2) >= 0.05) {
                    perlinNoiseArrayMBasePass[x, y] = Mathf.PerlinNoise (xCoord2, yCoord2);

                } else {
                    perlinNoiseArrayMBasePass[x, y] = 0;

                }

            }
        }

        //print ("size of points: " + xPointsMountainPeak.Count);

        //Cluster mountain points
        /* pre- Cull all points that aren't in a mountain tile
         * 1. Take each point and compare the distance to every other point
         * 2. For any given point, store its neighbours based on a radius - how?
         * 3. 
         * 
         * 
         * 
         * 
         * 
         *
         */

        //increment group number once a point cannot be connected to another point
        int groupNum = 0;

        for (int y = 0; y < xPointsMountainPeak.Count; y++) {
            for (int x = 0; x < xPointsMountainPeak.Count; x++) {

                //for (int z = 0; z < xPointsMountainPeak.Count; z++)
                //{
                //    //If the point is within distance to another point
                //    if()
                //    {

                //    }
                //}

            }
        }

        float tempx;
        float tempy;
        float previousSmallestDist = 2000;
        float previousSmallestDist2 = 2000;
        int smallestPoint = 0;
        int smallestPoint2 = 0;

        for (int i = 0; i < xPointsMountainPeak.Count; i++) {
            for (int j = 0; j < xPointsMountainPeak.Count; j++) {
                tempx = Mathf.Abs (xPointsMountainPeak[j] - xPointsMountainPeak[i]);
                tempy = Mathf.Abs (yPointsMountainPeak[j] - yPointsMountainPeak[i]);

                if (Mathf.Abs (tempx + tempy) < previousSmallestDist && j != i) {
                    //print ("Smallest Distance: " + Mathf.Abs (tempx + tempy));

                    previousSmallestDist = Mathf.Abs (tempx + tempy);
                    smallestPoint = j;
                } else if (Mathf.Abs (tempx + tempy) < previousSmallestDist2 && j != i) {
                    //print ("2nd Smallest Distance: " + Mathf.Abs (tempx + tempy));

                    previousSmallestDist2 = Mathf.Abs (tempx + tempy);
                    smallestPoint2 = j;

                }

            }

            //print(i);
            connectMountainPeaks (xPointsMountainPeak[i], yPointsMountainPeak[i], xPointsMountainPeak[smallestPoint], yPointsMountainPeak[smallestPoint]);
            connectMountainPeaks (xPointsMountainPeak[i], yPointsMountainPeak[i], xPointsMountainPeak[smallestPoint2], yPointsMountainPeak[smallestPoint2]);
            previousSmallestDist = 2000;
            previousSmallestDist2 = 2000;
            smallestPoint = 0;
        }

        //Connect peaks
        //for (int i = 0; i < numPoints - 1; i++)
        //{
        //    connectMountainPeaks(xPointsMountainPeak[i], yPointsMountainPeak[i], xPointsMountainPeak[i + 1], yPointsMountainPeak[i + 1]);
        //}

    }

    void connectMountainPeaks (int x1, int y1, int x2, int y2) {
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
        if (Mathf.Abs (dx) > Mathf.Abs (dy)) //No clue what this is doing right now
        {
            steps = Mathf.Abs (dx);
        } else {
            steps = Mathf.Abs (dy);
        }

        //Unit vector kinda(Blue Line2)
        if (Mathf.Abs (normalDX) > Mathf.Abs (normalDY)) //No clue what this is doing right now
        {
            stepsNormal = Mathf.Abs (normalDX);
        } else {
            stepsNormal = Mathf.Abs (normalDY);
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

        for (int i = 0; i <= steps; i++) {

            try {

                for (float j = size; j > 0; j--) {
                    //place pixel to the left and right of line perpendicularly
                    lineGradientXNormal += moveXNormal;
                    lineGradientYNormal += moveYNormal;

                    if (perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal] < j / size) {
                        perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal] = j / size;
                    }

                    if (perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal + 1] < j / size) {
                        perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal + 1] = j / size;
                    }

                    if (perlinNoiseArrayMPass[(int) lineGradientXNormal + 1, (int) lineGradientYNormal] < j / size) {
                        perlinNoiseArrayMPass[(int) lineGradientXNormal + 1, (int) lineGradientYNormal] = j / size;
                    }

                }

            } catch (Exception e) {
                //  print("point outside of map");

            }
            lineGradientXNormal = lineGradientX;
            lineGradientYNormal = lineGradientY;
            try {

                for (float j = size; j > 0; j--) {
                    //place pixel to the left and right of line perpendicularly
                    lineGradientXNormal -= moveXNormal;
                    lineGradientYNormal -= moveYNormal;

                    if (perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal] < j / size) {
                        perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal] = j / size;
                    }

                    if (perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal + 1] < j / size) {
                        perlinNoiseArrayMPass[(int) lineGradientXNormal, (int) lineGradientYNormal + 1] = j / size;
                    }

                    if (perlinNoiseArrayMPass[(int) lineGradientXNormal + 1, (int) lineGradientYNormal] < j / size) {
                        perlinNoiseArrayMPass[(int) lineGradientXNormal + 1, (int) lineGradientYNormal] = j / size;
                    }

                }
            } catch (Exception e) {
                // print("point outside of map");

            }

            //Move to next pixel
            lineGradientX += moveX;
            lineGradientY += moveY;

            lineGradientXNormal = lineGradientX;
            lineGradientYNormal = lineGradientY;
        }

        //print("end of function");
    }

    /* ------------------------------------------------------------------ */
    /* TERRAIN TYPE MANIPULATION */
    /* The function implementations are ordered below as they are called. */
    /* ------------------------------------------------------------------ */

    // Alex's work
    // Creates Terrain Type Grid from the image file for inputMapTexture.
    void InitTerrainTypeGrid () {

        inputMapGrid = new Color[inputMapTextureDim, inputMapTextureDim];

        ColorCorrectInputMap ();
        SetupTerrainTypeGridUntextured ();

        FillAndWarpHorizBorders ();
        WarpVertBorders ();

        float timeStart = Time.realtimeSinceStartup;
        ApplyBlurPasses ();
        print ("Seconds to execute blur: " + (Time.realtimeSinceStartup - timeStart));
    }

    // This basically has to exist until inputs that have *only* 4 colours are given. Currently, the provided example images do not have only 4 colours.
    void ColorCorrectInputMap () {
        for (int i = 0; i < inputMapTexture.width; i++) {
            for (int j = 0; j < inputMapTexture.height; j++) {
                if (inputMapTexture.GetPixel (i, j).r == inputMapTexture.GetPixel (i, j).g && inputMapTexture.GetPixel (i, j).r == inputMapTexture.GetPixel (i, j).b) {
                    inputMapGrid[i, j] = new Color (0.0f, 00.0f, 0.0f, 1.0f);

                } else if (inputMapTexture.GetPixel (i, j).b > inputMapTexture.GetPixel (i, j).r && inputMapTexture.GetPixel (i, j).b > inputMapTexture.GetPixel (i, j).g) {
                    inputMapGrid[i, j] = new Color (0.0f, 0.0f, 1.0f, 0.0f);
                } else if (inputMapTexture.GetPixel (i, j).g > 0.9f) {
                    inputMapGrid[i, j] = new Color (0.0f, 1.0f, 0.0f, 0.0f);
                } else {
                    inputMapGrid[i, j] = new Color (1.0f, 0.0f, 0.0f, 0.0f);
                }
            }
        }
    }

    // terrainTypeGrid is filled with a value outside of the range of 0.0f to 1.0f to act as a flag for when a cell has not been assigned a terrain type.
    void SetupTerrainTypeGridUntextured () {
        for (int i = 0; i < SIZE_FULL; i++) {
            for (int n = 0; n < SIZE_FULL; n++) {
                terrainTypeGrid[i, n] = new Color (9.9f, 9.9f, 9.9f, 9.9f);
            }
        }
    }

    // Fill the terrainTypeGrid with values corresponding to the inputMapGrid and warp the shape of the horizontal borders between the terrain types.
    void FillAndWarpHorizBorders () {

        Color eyedropperColour;
        inputMapTextureDim = inputMapTexture.height;
        int index = 0;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++) {
            float xCoord = Random.Range (0.0f, 99999.0f);
            float yCoord = Random.Range (0.0f, 99999.0f);
            float offset = 0;
            for (int x = 0; x < SIZE_FULL; x++) {
                if (terrainTypeGrid[x, y].r == 9.9f) {
                    float startPerlinWalkCoord = (Mathf.PerlinNoise (xCoord + offset, yCoord));
                    offset += 0.01f;
                    // Horizontal Border Warping only if within bounds of terrainTypeGrid, of size SIZE_FULL * SIZE_FULL, and is determined to be a "border" cell.
                    if (y % (int) (SIZE_FULL / inputMapTextureDim) == 0 && y > 0 && y < SIZE_FULL - inputMapTextureDim) {
                        // Sample a colour from the inputMapTexture
                        eyedropperColour = inputMapGrid[Mathf.Min ((int) (y / (int) (SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min ((int) (x / (int) (SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                        int shiftValue = getBorderShiftValue (startPerlinWalkCoord);
                        // This If/Else determines which direction a border is warped; up or down. It also performs the warp terrain type assignment.
                        if (shiftValue > 0) {
                            for (int i = 0; i < shiftValue; i++) {
                                if ((y + i) < SIZE_FULL) {
                                    terrainTypeGrid[x, y + i] = eyedropperColour;
                                }
                            }
                        } else {
                            for (int i = shiftValue; i < 0; i++) {
                                if ((y + i) > 0) {
                                    terrainTypeGrid[x, y + i] = eyedropperColour;
                                }
                            }
                        }
                        terrainTypeGrid[x, y] = eyedropperColour;
                    }
                    // Not on a border colouring
                    else {
                        // Sample a colour from the inputMapTexture
                        eyedropperColour = inputMapGrid[Mathf.Min ((int) (y / (int) (SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min ((int) (x / (int) (SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                        terrainTypeGrid[x, y] = eyedropperColour;
                    }
                }
                index++;
            }

        }
    }

    // Warp the shape of the vertical borders between the terrain types.
    void WarpVertBorders () {
        Color eyedropperColour;
        inputMapTextureDim = inputMapTexture.height;

        //Create the texture
        for (int x = 0; x < SIZE_FULL; x++) {
            float xCoord = Random.Range (0.0f, 99999.0f);
            float yCoord = Random.Range (0.0f, 99999.0f);
            float offset = 0;
            for (int y = 0; y < SIZE_FULL; y++) {
                float startPerlinWalkCoord = (Mathf.PerlinNoise (xCoord + offset, yCoord));
                offset += 0.01f;
                // Vertical Border Warping only if within bounds of terrainTypeGrid, of size SIZE_FULL * SIZE_FULL, and is determined to be a "border" cell.
                if (x % (int) (SIZE_FULL / inputMapTextureDim) == 0 && x > 0 && x < SIZE_FULL - inputMapTextureDim) {
                    // Sample a colour from the inputMapTexture
                    eyedropperColour = inputMapGrid[Mathf.Min ((int) (y / (int) (SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1), Mathf.Min ((int) (x / (int) (SIZE_FULL / inputMapTextureDim)), inputMapTextureDim - 1)];
                    int shiftValue = getBorderShiftValue (startPerlinWalkCoord);
                    // This If/Else determines which direction a border is warped; right or left. It also performs the warp terrain type assignment.
                    if (shiftValue > 0) {
                        for (int i = 0; i < shiftValue; i++) {
                            if ((x + i) < SIZE_FULL) {
                                terrainTypeGrid[x + i, y] = eyedropperColour;
                            }
                        }
                    } else {
                        for (int i = shiftValue; i < 0; i++) {
                            if ((x + i) > 0) {
                                terrainTypeGrid[x + i, y] = eyedropperColour;
                            }
                        }
                    }
                }
            }
        }
    }

    int getBorderShiftValue (float perlinShiftValue) {
        return (int) ((perlinShiftValue - 0.5f) * terrainBorderShiftMod);
    }

    // Calls the function that iterates over all terrain type cells BLUR_PASSES times.
    void ApplyBlurPasses () {

        // If gaussBlurKernelDimNxN is 0 or less, then we shouldn't call the gauss blurring functions.
        if (gaussBlurKernelDimNxN > 0) {
            CreateConvBlurKernel ();
            for (int i = 0; i < GAUSS_BLUR_PASSES; i++) {
                terrainTypeGrid = BlurTerrainBorders ();
            }
            // If boxBlurKernelDimNxN is 0 or less, then we shouldn't call the box blurring function.
            if (boxBlurKernelDimNxN > 0) {
                for (int i = 0; i < BOX_BLUR_PASSES; i++) {
                    terrainTypeGrid = BlurTerrainBorders ();
                }
            }

        }
    }

    // Builds a kernel of size gaussBlurKernelDimNxN * gaussBlurKernelDimNxN using the formula for gaussian kernels.
    void CreateConvBlurKernel () {
        gaussConvBlurKernel = new float[gaussBlurKernelDimNxN, gaussBlurKernelDimNxN];
        float sumKernel = 0.0f;
        for (int x = -((gaussBlurKernelDimNxN - 1) / 2); x < ((gaussBlurKernelDimNxN + 1) / 2); x++) {
            for (int y = -((gaussBlurKernelDimNxN - 1) / 2); y < ((gaussBlurKernelDimNxN + 1) / 2); y++) {
                gaussConvBlurKernel[x + ((gaussBlurKernelDimNxN - 1) / 2), y + ((gaussBlurKernelDimNxN - 1) / 2)] = GetGaussianKernelValue (System.Math.Abs (x), System.Math.Abs (y));
                sumKernel += gaussConvBlurKernel[x + ((gaussBlurKernelDimNxN - 1) / 2), y + ((gaussBlurKernelDimNxN - 1) / 2)];
            }
        }
        float kernelMustBeSummedToOne = 1.0f / sumKernel;
        for (int x = 0; x < gaussBlurKernelDimNxN; x++) {
            for (int y = 0; y < gaussBlurKernelDimNxN; y++) {
                gaussConvBlurKernel[x, y] = gaussConvBlurKernel[x, y] * kernelMustBeSummedToOne;
            }
        }
    }

    // Given an offset location from the centre cell of the kernel, return the value for the offset location's cell using the gaussian formula.
    float GetGaussianKernelValue (float xOffset, float yOffset) {
        return (float) (1.0f / (2.0f * Mathf.PI * Mathf.Pow (sigmaWeight, 2.0f))) * Mathf.Pow ((float) System.Math.E, -((Mathf.Pow (xOffset, 2.0f) + Mathf.Pow (yOffset, 2.0f)) / (2.0f * Mathf.PI * Mathf.Pow (sigmaWeight, 2.0f))));
    }

    // Iterates over all terrain type cells calling the blur function for a single cell's kernel.
    Color[, ] BlurTerrainBorders () {
        Color[, ] blurTerrain = terrainTypeGrid;
        //Check if either blur type has been specified to execute. Otherwise the above blurTerrain will be returned unchanged as equal to terrainTypeGrid.
        if (doGaussAndOrBoxBlur[0] == true || doGaussAndOrBoxBlur[1] == true) {
            blurTerrain = new Color[(int) SIZE_FULL, (int) SIZE_FULL];
        }
        // Perform the Gaussian blur if true.
        if (doGaussAndOrBoxBlur[0] == true) {
            for (int x = 0; x < SIZE_FULL; x++) {
                for (int y = 0; y < SIZE_FULL; y++) {
                    blurTerrain[x, y] = ApplyGaussConvKernelToCell (x, y);
                }
            }
        }
        // Perform the Box blur if true.
        if (doGaussAndOrBoxBlur[1] == true) {
            for (int x = 0; x < SIZE_FULL; x++) {
                for (int y = 0; y < SIZE_FULL; y++) {
                    blurTerrain[x, y] = ApplyBoxBlurConvKernelToCell (x, y);
                }
            }
        }
        return blurTerrain;
    }

    // Returns a new Color using the weights in the built gaussConvBlurKernel[].
    Color ApplyGaussConvKernelToCell (int xCoord, int yCoord) {
        Color newWeight = new Color (0.0f, 0.0f, 0.0f, 0.0f);
        int numContributors = 0;
        for (int x = -((gaussBlurKernelDimNxN - 1) / 2); x < ((gaussBlurKernelDimNxN + 1) / 2); x++) {
            for (int y = -((gaussBlurKernelDimNxN - 1) / 2); y < ((gaussBlurKernelDimNxN + 1) / 2); y++) {
                if (xCoord + x >= 0 && xCoord + x < SIZE_FULL && yCoord + y >= 0 && yCoord + y < SIZE_FULL) {
                    newWeight += terrainTypeGrid[xCoord + x, yCoord + y] * gaussConvBlurKernel[x + ((gaussBlurKernelDimNxN - 1) / 2), y + ((gaussBlurKernelDimNxN - 1) / 2)];
                }
            }
        }
        return newWeight;
    }

    // Returns a new Color using assumed equal weights across the kernel.
    Color ApplyBoxBlurConvKernelToCell (int xCoord, int yCoord) {
        Color newWeight = new Color (0.0f, 0.0f, 0.0f, 0.0f);
        float numContributors = 0.0f;
        for (int x = -((boxBlurKernelDimNxN - 1) / 2); x < ((boxBlurKernelDimNxN + 1) / 2); x++) {
            for (int y = -((boxBlurKernelDimNxN - 1) / 2); y < ((boxBlurKernelDimNxN + 1) / 2); y++) {
                if (xCoord + x >= 0 && xCoord + x < SIZE_FULL && yCoord + y >= 0 && yCoord + y < SIZE_FULL) {
                    newWeight += terrainTypeGrid[xCoord + x, yCoord + y];
                    numContributors++;
                }
            }
        }
        return newWeight / numContributors;
    }

    /* ------------------------------------------------------------------ */
    /* GETTERS & SETTERS */
    /* ------------------------------------------------------------------ */

    public Texture2D GetTerrainTypeTexture () {
        return terrainTypeTexture;
    }

    public Color[, ] GetTerrainTypeGrid () {
        return terrainTypeGrid;
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

            **/
}