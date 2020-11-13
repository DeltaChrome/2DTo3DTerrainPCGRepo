using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*  This Manager Manages all Managers :)
 *  Big Boss
 */

public class TerrainManager : MonoBehaviour {

    //Resolution of Terrain Grid & Height Map
    public const float SIZE_FULL = 2049.0f; //Resolution of the height map texture

    // Our user defined and provided 2D tiled map as a texture2D
    public Texture2D inputMapTexture;
    // inputMapTextureDim * inputMapTextureDim pixels is the size of the user defined 2D map. We expect a square texture.
    public int inputMapTextureDim;

    // Variables for the terrain type blending 
    public Color32[, ] terrainTypeGrid = new Color32[(int) SIZE_FULL, (int) SIZE_FULL];

    // Variables for rendering the mini maps for perlin noise and terrsain types.
    Image terrainTypeMiniMap;
    Material terrainTypeMaterial;
    Image perlinNoiseMiniMap;
    Material perlinNoiseMaterial;

    // Perlin Noise Arrays.
    float[, ] perlinNoiseArray = new float[(int) SIZE_FULL, (int) SIZE_FULL];
    // For Individual Terrain Cell.
    float[, ] perlinNoiseArrayCell = new float[(int) SIZE_FULL, (int) SIZE_FULL];

    // Variables to hold the scene's terrain data
    public GameObject terrainObject;
    Terrain terrainComponent;

    //Water Manager Script - Eric's Code
    //Agent Manager Script - Nader's Code
    //*Mountain Manager Script - Jacob's Code*

    // Start is called before the first frame update
    void Start () {

        ClearNoise(perlinNoiseArray);
        InitMaps();

        //Call Terrain creation functions
        //Call Water Manager
        //Call Agent Manager
    }

    // Resets noise array to hold zeros.
    void ClearNoise(float[, ] noiseArray){
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {
                noiseArray[x, y] = 0;
            }
        }        
    }

    void InitMaps () {
        // Set texture of perlinNoiseMiniMap.
        perlinNoiseMaterial = new Material (Shader.Find ("Unlit/Texture"));
        perlinNoiseMiniMap = GameObject.Find ("perlinNoiseMiniMap").GetComponent<Image> ();
        perlinNoiseMiniMap.material = perlinNoiseMaterial;
        perlinNoiseMiniMap.material.mainTexture = GenerateTexture ();

        // Set texture of terrainTypeMiniMap.
        terrainTypeMaterial = new Material (Shader.Find ("Unlit/Texture"));
        terrainTypeMiniMap = GameObject.Find ("terrainTypeMiniMap").GetComponent<Image> ();
        terrainTypeMiniMap.material = terrainTypeMaterial;
        terrainTypeMiniMap.material.mainTexture = InitTerrainTypeGrid ();
    }

    Texture2D GenerateTexture () {
        Texture2D texture = new Texture2D ((int) SIZE_FULL, (int) SIZE_FULL);

        float noiseValue = 0.0f;

        CreateMultiLayeredNoise ();

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {
                noiseValue = perlinNoiseArray[x, y] / 2.0f;

                Color color = new Color (noiseValue, noiseValue, noiseValue);

                texture.SetPixel (x, y, color);

                //Create Terrain Height Map Cell 1
                perlinNoiseArrayCell[x, y] = noiseValue;

            }
        }

        //Set terrain height map
        terrainComponent = terrainObject.GetComponent<Terrain> ();
        terrainComponent.terrainData.SetHeights (0, 0, perlinNoiseArrayCell);

        //Apply texture
        texture.Apply ();

        //I think the below can be removed since this is already done in Start()

        ClearNoise(perlinNoiseArray);
        ClearNoise(perlinNoiseArrayCell);

        return texture;
    }

    // Update is called once per frame
    void Update () {

    }

    //Creates and merges perlin noise for height variance
    void CreateMultiLayeredNoise () {

        float frequency = 2.0f;
        float noiseValue = 0.0f;

        //8 layers of noise
        for (int numLayers = 0; numLayers < 8; numLayers++) {

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
                        perlinNoiseArray[x, y] += (Mathf.PerlinNoise (xCoord, yCoord));

                    } else {
                        perlinNoiseArray[x, y] += (Mathf.PerlinNoise (xCoord, yCoord) / ((float) numLayers * (2.0f) + 20.0f));

                    }

                }
            }
        }

    }

    // Alex's rework
    // Creates Terrain Type Grid from the image file for inputMapTexture.
    Texture2D InitTerrainTypeGrid () {

        Texture2D terrainTypeTexture = new Texture2D ((int) SIZE_FULL, (int) SIZE_FULL);
        Color eyedropperColour;
        inputMapTextureDim = inputMapTexture.height;

        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++) {
            for (int x = 0; x < SIZE_FULL; x++) {
                if (y % (int) (SIZE_FULL / inputMapTextureDim) == 0 || x % (int) (SIZE_FULL / inputMapTextureDim) == 0) {
                    eyedropperColour = Color.red;
                } else {
                    eyedropperColour = inputMapTexture.GetPixel (y / (int) (SIZE_FULL / inputMapTextureDim), x / (int) (SIZE_FULL / inputMapTextureDim));
                }
                terrainTypeTexture.SetPixel (x, y, eyedropperColour);
            }

            //Apply texture
            terrainTypeTexture.Apply ();
        }
        return terrainTypeTexture;
    }
}