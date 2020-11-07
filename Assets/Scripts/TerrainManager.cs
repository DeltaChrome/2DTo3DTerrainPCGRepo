using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  This Manager Manages all Managers :)
 *  Big Boss
 */



public class TerrainManager : MonoBehaviour
{

    public const float SIZE = 257.0f;
    public const float SIZE_FULL = 2049.0f;

    public Texture2D ColourRepresentation;
    public Color32[,] Grid1TerrainType = new Color32[(int)SIZE_FULL, (int)SIZE_FULL];

    float[,] noiseFunctionOneArray = new float[(int)SIZE_FULL, (int)SIZE_FULL];

    //Individual Terrain Cell
    float[,] noiseFunctionOneArray2 = new float[(int)SIZE_FULL, (int)SIZE_FULL];

    public GameObject testImage;
    public GameObject terrainObj;
    public GameObject terrainObj2;

    Terrain terr;
    Terrain terr2;

    Renderer renderer;

    

    //Water Manager Script - Eric's Code
    //Agent Manager Script - Nader's Code
    //*Mountain Manager Script - Jacob's Code*


    
    // Start is called before the first frame update
    void Start()
    {
        //init
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseFunctionOneArray[x, y] = 0;
            }
        }
        //terrainObj.terr.GetComponent<Terrain>();

        terr = terrainObj.GetComponent<Terrain>();
        terr2 = terrainObj2.GetComponent<Terrain>();
        //terr.terrainData.heightmapResolution = 257;

        renderer = testImage.GetComponent<Renderer>();
        renderer.material.mainTexture = GenerateTexture();

        //Call TerrainType grid
        //Call Terrain creation functions
        //Call Water Manager
        //Call Agent Manager
    }

    Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D((int)SIZE_FULL, (int)SIZE_FULL);

        

        //print("x and y");
        //print(xOffset);
        //print(yOffset);

        float frequency = 2.0f;

        float noiseValue = 0.0f;

        //First layer of noise

        for (int numLayers = 0; numLayers < 8; numLayers++)
        {

            float xOffset = Random.Range(0.0f, 99999.0f);
            float yOffset = Random.Range(0.0f, 99999.0f);

            if(numLayers == 0)
            {
                frequency = Random.Range(0.75f, 1.5f);

            }

                frequency += (numLayers * 4) + 1;

            //float limit = 3.0f;
            //float layerDiv = 3.0f;

            //layerDiv = limit - numLayers;

            for (int y = 0; y < SIZE_FULL; y++)
            {
                for (int x = 0; x < SIZE_FULL; x++)
                {
                
                    float xCoord = ((float)x / SIZE_FULL) * frequency + xOffset;
                    float yCoord = ((float)y / SIZE_FULL) * frequency + yOffset;

                    if(numLayers == 0)
                    {
                        noiseFunctionOneArray[x, y] += (Mathf.PerlinNoise(xCoord, yCoord));
                
                    }
                    else
                    {
                        noiseFunctionOneArray[x, y] += (Mathf.PerlinNoise(xCoord, yCoord) / ((float)numLayers * (2.0f) + 20.0f));

                    }

                    //noiseValue = Mathf.PerlinNoise(xCoord, yCoord);

                    //noiseFunctionOneArray[x, y] = ((int)(Mathf.PerlinNoise(xCoord, yCoord) * 100.0f)) / 100;

                }
            }
        }

        //TerrainData terrainData = terrain.GetComponent<TerrainData>();
        //terrainData.Texture2D[0] = texture;

        //CHANGED FROM 1k to 513!!!!
        //Create the texture
        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseValue = noiseFunctionOneArray[x, y] / 2.0f;

                //print(noiseValue);

                Color color = new Color(noiseValue, noiseValue, noiseValue);

                texture.SetPixel(x, y, color);

                //Create Terrain Height Map Cell 1
                noiseFunctionOneArray2[x, y] = noiseValue;
               

            }
        }

        terr.terrainData.SetHeights(0, 0, noiseFunctionOneArray2);

        //TERRAIN 2
        //for (int y = 0; y < SIZE; y++)
        //{
        //    for (int x = (int) SIZE; x < SIZE * 2; x++)
        //    {
        //        noiseValue = noiseFunctionOneArray[x, y] / 5.0f;

        //        //print(noiseValue);

        //        //Color color = new Color(noiseValue, noiseValue, noiseValue);

        //        //texture.SetPixel(x, y, color);

        //        //Create Terrain Height Map Cell 1
        //        noiseFunctionOneArray2[x - (int)SIZE, y] = noiseValue;

        //    }
        //}
        //terr2.terrainData.SetHeights(0, 0, noiseFunctionOneArray2);

        //Apply texture
        texture.Apply();

        for (int y = 0; y < SIZE_FULL; y++)
        {
            for (int x = 0; x < SIZE_FULL; x++)
            {
                noiseFunctionOneArray[x, y] = 0;
            }
        }


        return texture;
    }

    // Update is called once per frame
    void Update()
    {
        //renderer.material.mainTexture = GenerateTexture();
    }

    void createTerrainCell(int cellNum)
    {



        ////TERRAIN 2
        //for (int y = 0; y < SIZE; y++)
        //{
        //    for (int x = (int)SIZE; x < SIZE * 2; x++)
        //    {
        //        noiseValue = noiseFunctionOneArray[x, y] / 5.0f;

        //        //print(noiseValue);

        //        //Color color = new Color(noiseValue, noiseValue, noiseValue);

        //        //texture.SetPixel(x, y, color);

        //        //Create Terrain Height Map Cell 1
        //        noiseFunctionOneArray2[x - (int)SIZE, y] = noiseValue;
        //        terr2.terrainData.SetHeights(0, 0, noiseFunctionOneArray2);

        //    }
        //}
    }

    //Creates Terrain Type Grid from the image file for colourRepresentation
    void createTerrainTypeGrid()
    {
        for(int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 20; j++)
            {

                Color tempRepColour;
                tempRepColour = ColourRepresentation.GetPixel(i, j);

                int tempR = 0; //Forest - Duh
                int tempB = 0; //Mountains - icy
                int tempG = 0; //Grassland
                int tempA = 0; //Water of course

                //All of these checks only consider the one channel and assumes that the 
                //other values are 0. Order might matter

                //Splitting the two greens up based on upper and lower half of green value
                if (tempRepColour.g >= 127)
                {
                    tempG = 255;
                }
                else if (tempRepColour.g >= 1)
                {
                    tempG = 126;
                }
                else
                {
                    tempG = 0;
                }

                //Blue converting to blue, but potentially needs to be converted to alpha instead
                if (tempRepColour.b >= 127)
                {
                    tempB = 255;
                }

                //This is supposed to be black
                if (tempRepColour.b == 0 && tempRepColour.r == 0 && tempRepColour.g == 0)
                {
                    tempA = 255;
                }

                for (int z = 0; z < 50; z++)
                {
                    Grid1TerrainType[i + z, j] = new Color32(255, 0, 0, 255);
                    Grid1TerrainType[i, j + z] = new Color32(255, 0, 0, 255);

                }


            }
        }
    }

    //Creates and merges perlin noise for height variance
    void createMultiLayeredNoise()
    {
        //First layer of noise
        for(int y = 0; y < 1000; y++)
        {
            for (int x = 0; x < 1000; x++)
            {

                float xCoord = ((float)x / 1000.0f) * 4.0f + 0.0f;
                float yCoord = ((float)y / 1000.0f) * 4.0f + 0.0f;

                noiseFunctionOneArray[x, y] = (Mathf.PerlinNoise(xCoord, yCoord));
                //noiseFunctionOneArray[x, y] = ((int)(Mathf.PerlinNoise(xCoord, yCoord) * 100.0f)) / 100;

            }
        }

        //asfdsgfgd

    }

}
