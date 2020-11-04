using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  This Manager Manages all Managers :)
 *  Big Boss
 */

public class TerrainManager : MonoBehaviour
{

    public Texture2D ColourRepresentation;
    public Color32[,] Grid1TerrainType = new Color32[1000,1000];


    //Water Manager Script - Eric's Code
    //Agent Manager Script - Nader's Code
    //*Mountain Manager Script - Jacob's Code*
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        //Call TerrainType grid
        //Call Terrain creation functions
        //Call Water Manager
        //Call Agent Manager
    }

    // Update is called once per frame
    void Update()
    {

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

        for(int y = 0; y < 1000; y++)
        {
            for (int x = 0; x < 1000; x++)
            {



            }
        }

        //float xCoord = xOrg + x / noiseTex.width * scale;
        //float yCoord = yOrg + y / noiseTex.height * scale;

        //float sample = Mathf.PerlinNoise(xCoord, yCoord);

    }

}
