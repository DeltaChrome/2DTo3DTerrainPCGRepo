using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @Author: Eric Chan aka eepmon / www.eepmon.com
 * @Info: The WaterManager class
 * Detects and determines bodies of water and shorlines
 */
public class WaterManager : MonoBehaviour
{
    private int SIZE_FULL;
    private float MIN_SHORE_THRESHOLD = 0.53f;
    private float MAX_SHORE_THRESHOLD = 0.95f;
    private float WATER_HEIGHT;
    
    public List<WaterDataObject> shoreLines = new List<WaterDataObject>();  // this could be for Nader eventually
    public List<WaterDataObject> allWater   = new List<WaterDataObject>();

    /* //////////////////////////////////////*/
    /* #### IGNORE DEBUGGING VARS BELOW #### */
    /* //////////////////////////////////////*/

    private int row, col;

    // for testing purpose until I get the acutal terrainTypeGrid data! 
    private string testImage = "Textures/terrain-200x200-a";
    private Color[,] colDataDummy;                    
    private Texture2D waterTex;

    // debugging variables
    private Color32[,] pixelData2D;                      // 2D array of the image data (yes that's how you declare a 2D arr)
    private Color32[,] pixelTest = new Color32[3,3];     // for debug

    void Awake() { print("---------- WaterManager :: Awakened");}
    void Start() { print("---------- WaterManager :: Started"); }

    /*
     * @Author: Eric Chan aka eepmon 
     * @Info: return the shoreline data to the calling program
     */
    public List<WaterDataObject> getShorelines()
    {
        return shoreLines;
    }

    /*
     * @Author: Eric Chan aka eepmon 
     * @Info: initializes the WaterManger with terrainTypeGrid data
     */
    public void initialize(int gridSize, Color[,] terrainTypeGrid)
    {
        print("---------- WaterManager :: initialize");

        SIZE_FULL = 2049;
       
        for (int x = 0; x < SIZE_FULL; x++)
        {
            for (int y = 0; y < SIZE_FULL; y++)
            {
                checkForWaterRegion(x, y, terrainTypeGrid[x, y]);     // ### FOR PRODUCTION!!  
            }
        }
    }

    /*
     * @Author: Eric Chan aka eepmon 
     * @Info: updates the perlinHeightData data with 
     * detected shoreline and bodies of water
     * @Returns: updated perlinHeightData
     */

    public float getWaterThreshold()
    {
        return MIN_SHORE_THRESHOLD;
    }

    public float[,] getHeights(float[,] perlinHeightData)
    {
        // get the lowest number in the 2D array
        WATER_HEIGHT = getLowestNumIn2DArr(perlinHeightData, SIZE_FULL) - 0.01f;
       
        // this loop goes through 100% WATER ONLY
        for (int i = 0; i < allWater.Count; i++)
        {
            perlinHeightData[allWater[i].x, allWater[i].y] = WATER_HEIGHT;
        }

        // this loop goes through SHORELINES ONLY
        for (int i = 0; i < shoreLines.Count; i++)
        {
            // get perline noise height
            float perlinHeight = perlinHeightData[shoreLines[i].x, shoreLines[i].y];

            // % terms where are we between min and max threshold
            float x = (shoreLines[i].weight - MIN_SHORE_THRESHOLD) / (MAX_SHORE_THRESHOLD - MIN_SHORE_THRESHOLD);

            // the GOLDEN ratio formula!!!!
            float height = x * WATER_HEIGHT + (1 - x) * perlinHeight;

            perlinHeightData[shoreLines[i].x, shoreLines[i].y] = height;
        }

        return perlinHeightData;
    }

    /*
     * @Author: Eric Chan aka eepmon
     * @Info: creates the water planes to simulate bodies of water
     */
    public void createWaterPlane(Terrain terrainComp)
    {
        float lowestWaterHeight = getLowestWaterHeight(terrainComp);
        float bodyWaterX = terrainComp.terrainData.bounds.center[0];
        float bodyWaterY = terrainComp.terrainData.bounds.center[2];

        /*
        print("## LOWEST WATER TERRAIN HEIGHT AT LOWEST = " + lowestWaterHeight);
        print("TERRAIN BOUNDS MIN = " + terrainComp.terrainData.bounds.min);
        print("TERRAIN BOUNDS MAX = " + terrainComp.terrainData.bounds.max);
        print("TERRAIN BOUNDS CENTER = " + terrainComp.terrainData.bounds.center);
        print("TERRAIN BOUNDS SIZE = " + terrainComp.terrainData.bounds.size);
        */

        // create the single water plane to "dissect" the Terrain on the z-axis
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        waterPlane.transform.localScale = new Vector3(100.0f, 1.0f, 100.0f);
        waterPlane.transform.position = new Vector3(bodyWaterX, lowestWaterHeight + 0.05f, bodyWaterY);
        // apply blue material to simulate water
        Renderer waterMaterial = waterPlane.GetComponent<Renderer>(); // grab the renderer component on the plane
        waterMaterial.material.SetColor("_Color", new Color(0.0f,0.0f,1.0f,0.8f));
    }

    /*
     * @Author: Eric Chan aka eepmon
     * @Info: gets the lowest terrain height from bodies of water only
     * - List<WaterDataObject> allWater is a member variable of this class
     * @Returns: the lowet height value within the bodies of water
     */
    private float getLowestWaterHeight(Terrain terrainComp)
    {
        List<float> waterHeights = new List<float>();

        // collect all the height data for water only
        for (int i = 0; i < allWater.Count; i++)
        {
            waterHeights.Add(terrainComp.terrainData.GetHeight(allWater[i].x, allWater[i].y));
        }

        return getLowestNumInList(waterHeights);
    }


    /*
     * @Author: Eric Chan aka eepmon
     * @Info: checks to see if the data is of water type or shore
     */
    private void checkForWaterRegion(int x, int y, Color data)
    {
        Color water = data;

        // find shoreline between these percentage values of water
        if (water.b > MIN_SHORE_THRESHOLD && water.b < MAX_SHORE_THRESHOLD)
        { 
            // find what type of terrain it is based on strength of the component
            WaterDataObject shoreData = getShoreData(x,y,data);
            shoreLines.Add(shoreData);
        }
        else if (water.b > MAX_SHORE_THRESHOLD)
        {
            // it's 100% water go DEEP DIVE!
            WaterDataObject wObj = new WaterDataObject();
            wObj.init(x, y, "water", 0.1f);
            allWater.Add(wObj);
        }
        else
        {
            // nothing found
        }
    }

    /*
     * @Author: Eric Chan aka eepmon
     * @Info: finds out what type of shoreline, encapsulates
     * the data into a WaterDataObject. Shoreline is determined by the weighted values in RGBA.
     * @Returns: WaterDataObject to the calling program
     */
    private WaterDataObject getShoreData(int x, int y, Color cData)
    {
        WaterDataObject wObj = new WaterDataObject();

        // check shoreline type. Tree? Grass? Mountain?
        if (cData.r > cData.g && cData.b > cData.g)
        {
            wObj.init(x, y, "forest", cData.b);
        }
        else if (cData.g > cData.r && cData.b > cData.r)
        {
            wObj.init(x, y, "grass", cData.b);
        }
        else // clearly it must be a mountain
        {
            wObj.init(x, y, "mountain", cData.b);
        }

        return wObj;
    }


    /* //////////////////////////////////*/
    /* #### START UTILITY FUNCTIONS #### */
    /* //////////////////////////////////*/


    /*
     * @Author: Eric Chan aka eepmon
     * @Info: searches for the lowest number from incoming List of floats
     * @Returns: lowest number
     */
    private float getLowestNumInList(List<float> arr)
    {
        float lowest = arr[0];

        for (int i = 0; i < arr.Count; i++)
        {
            float num = arr[i];

            if (num < lowest)
            {
                lowest = num;
            }
        }
        return lowest;
    }

    /*
     * @Author: Eric Chan aka eepmon
     * @Info: searches for the highest number from incoming List of floats
     * @Returns: highest number
     */
    private float getHighestNumInList(List<float> arr)
    {
        float highest = arr[0];

        for (int i = 0; i < arr.Count; i++)
        {
            float num = arr[i];

            if (num > highest)
            {
                highest = num;
            }
        }
        return highest;
    }

    /*
     * @Author: Eric Chan aka eepmon
     * @Info: searches for the lowest number from incoming 2D-array of floats
     * @Return: lowest number
     */
    private float getLowestNumIn2DArr(float[,] arr, int size)
    {
        float lowest = arr[0, 0];
       
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float num = arr[i, j];

                if (num < lowest)
                {
                    lowest = num;
                }
            }
        }
        return lowest;
    }

    /*
     * @Author: Eric Chan aka eepmon
     * @Info: searches for the highest number from incoming 2D-array of floats
     * @Returns: highest number
     */
    private float getHighestNumIn2DArr(float[,] arr)
    {
        float highest = arr[0, 0];

        for (int i = 0; i < SIZE_FULL; i++)
        {
            for (int j = 0; j < SIZE_FULL; j++)
            {
                float num = arr[i, j];

                if (num > highest)
                {
                    highest = num;
                }
            }
        }
        return highest;
    }

    /* ////////////////////////////////*/
    /* #### END UTILITY FUNCTIONS #### */
    /* ////////////////////////////////*/


    /* ////////////////////////////////////////////////////////////////////////////////////////*/
    /* #### START DEBUGGING FUNCTIONS BELOW. NOT USED IN FINAL PCG SYSTEM IMPLEMENTATION! #### */
    /* ////////////////////////////////////////////////////////////////////////////////////////*/


    /*
     * @Author: Eric Chan aka eepmon
     * @Info: FOR DEBUGGING ONLY!
     * Gives the target x,y and search space value. 
     * Using try/catch so that the search space does not fall into ArrayIndexOutOfBounds
     * @Returns: weighted average based on the area where target is at the centre
     */
    private float getWeightAverageAt(int x, int y, int s = 2)
    {
        float finalWeightAvg = 0.0f;

        for (int j = 1; j < s; j++)
        {
            //Debug.Log("getWeightAverageAt :::: target x,y = " + x + "," + y);
            float weightSum = 0.0f;
            int numPx = 0;

            // traverses from around the target x,y like a snake and gets weighted values
            for(int k=(x-j)+1; k < (x+j)+1; k++)
            {
                //Debug.Log("GO TOP LEFT TO TOP RIGHT ///////////////////// @ " + k + "," + (y+j));
                try
                {
                    Color32 col32 = pixelData2D[k, y + j];
                    weightSum += getTerrainWeightByColor32(col32);
                    numPx++;
                }
                catch (Exception e) { }
            }

            for(int k=(y+j)-1; k > (y-j)-1; k--)
            {
                //Debug.Log("GO TOP RIGHT TO BOTTOM RIGHT  ///////////////////// @ " + (x+j) + "," + k);
                try
                {
                    Color32 col32 = pixelData2D[x+j, k];
                    weightSum += getTerrainWeightByColor32(col32);
                    numPx++;
                }
                catch (Exception e) { }
            }

            for (int k = (x + j)-1; k > (x-j)-1; k--)
            {
                //Debug.Log("GO BOTTOM RIGHT TO BOTTOM LEFT  ///////////////////// @ " + k + "," + (y-j));
                try
                {
                    Color32 col32 = pixelData2D[k, y - j];
                    weightSum += getTerrainWeightByColor32(col32);
                    numPx++;
                }
                catch (Exception e) { }
            }

            for (int k = (y - j)+1; k < (y + j) + 1; k++)
            {
                //Debug.Log("GO BOTTOM LEFT TO TOP LEFT  ///////////////////// @ " + (x-j) + "," + k);
                try
                {
                    Color32 col32 = pixelData2D[x-j, k];
                    weightSum += getTerrainWeightByColor32(col32);
                    numPx++;
                }
                catch (Exception e) { }
            }
            
            finalWeightAvg += (weightSum / numPx);
        }

        return finalWeightAvg;
    }


    /*
     * @Author: Eric Chan aka eepmon
     * @Info: FOR DEBUGGING ONLY!
     * Searches the neighbourhood around x,y coordinates of the
     * pixelData2D array. If it is found, it will draw a sphere with its corresonding terrain colour
     * Using try/catch so that the search space does not fall into ArrayIndexOutOfBounds
     */
    private void srchAndDrwNBsAt(int x, int y, int s = 1)
    {
        // identify the cell (x,y)
        //Debug.Log("+++++++++AND NOW WE WILL LOOK A NEIGHBOURS OF (" + x + " , " + y + ") +++++++++");
        
        // s is the search space that cannot exceed SEARCHSPACE_MAX?
        for (int j = 1; j < s; j++)
        {
            // check to see if searching upper left even exists. If so draw it!
            try 
            {
                Color32 col32 = pixelData2D[x - j, y + j];
                drawPixelData(x - j, y + j, col32);
                //Debug.Log("Upper Left EXIST @ " + (x - j) + "," + (y + j));
            }
            catch (Exception e) { Debug.Log("Upper Left NOT EXIST @ " + (x - j) + "," + (y + j));}

            // check to see if searching top exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x, y + j];
                drawPixelData(x, y + j, col32);
                //Debug.Log("Top EXIST @ " + x  + "," + (y + j));
            }
            catch (Exception e) { Debug.Log("Top NOT EXIST @ " + x + "," + (y + j)); }

            // check to see if searching upper right exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x + j, y + j];
                drawPixelData(x + j, y + j, col32);
                //Debug.Log("Upper Right EXIST @ " + (x + j) + "," + (y + j));
            }
            catch (Exception e) { Debug.Log("Upper Right EXIST @ " + (x + j) + "," + (y + j)); }

            // check to see if searching right exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x + j, y];
                drawPixelData(x + j, y, col32);
                //Debug.Log("Right EXIST @ " + (x + j) + "," + y);
            }
            catch (Exception e) { Debug.Log("Right EXIST @ " + (x + j) + "," + y); }

            // check to see if searching bottom right exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x + j, y - j];
                drawPixelData(x + j, y - j, col32);
                //Debug.Log("Bottom Right EXIST @ " + (x + j) + "," + (y - j));
            }
            catch (Exception e) { Debug.Log("Bottom Right EXIST @ " + (x + j) + "," + (y - j)); }

            // check to see if searching bottom exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x, y - j];
                drawPixelData(x, y - j, col32);
                //Debug.Log("Bottom EXIST @ " + x + "," + (y - j));
            }
            catch (Exception e) { Debug.Log("Bottom EXIST @ " + x + "," + (y - j)); }

            // check to see if searching bottom left exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x - j, y - j];
                drawPixelData(x - j, y - j, col32);
                //Debug.Log("Bottom Left EXIST @ " + (x-j) + "," + (y - j));
            }
            catch (Exception e) { Debug.Log("Bottom Left EXIST @ " + (x - j) + "," + (y - j)); }

            // check to see if searching left exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x - j, y];
                drawPixelData(x - j, y, col32);
                //Debug.Log("Bottom Left EXIST @ " + (x - j) + "," + y);
            }
            catch (Exception e) { Debug.Log("Bottom Left EXIST @ " + (x - j) + "," + y); }
        }
    }


    /*
     * @Author: Eric Chan aka eepmon
     * @Info: FOR DEBUGGING ONLY!
     * Draws the pixel into the scene at specified location and colour
     */
    public void drawPixelData(int x, int y, Color32 col32)
    {
        GameObject spherePx;
        spherePx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spherePx.name = getTerrainNameFromColor32(col32) + x + "," + y;
        spherePx.transform.position = new Vector3(x, 0, y);
        colorizeObj(spherePx, col32);
    }


   /* 
    * @Author: Eric Chan aka eepmon
    * @Info: FOR DEBUGGING ONLY!
    * Colours the game object 
    */
    public void colorizeObj(GameObject ob, Color32 col32)
    {
        if (ob.GetComponent<MeshRenderer>() == null)
        {
            ob.AddComponent<MeshRenderer>();
        }

        Renderer obRenderer = ob.GetComponent<Renderer>();
        obRenderer.material.SetColor("_Color", col32);
    }


    /* 
    * @Author: Eric Chan aka eepmon
    * @Info: Returns the name of the terrain
    * based on hard coded Color32 values
    */
    private string getTerrainNameFromColor32(Color32 col32)
    {
        if(col32.b > 250)
        {
            return "water";
        }
        else if (col32.g >= 250)             // check for lightgreen (grass)
        {
            return "grass";
        }
        else if (col32.g <= 106)        // check for darkgreen (forest?)
        {
            return"forest";
        }
        else if (col32.g == 160)      // check for grey mountain
        {
            return "mountain";
        }
        else
        {
            return "unknown";
        }
    }


    /* 
    * @Author: Eric Chan aka eepmon
    * @Info: Returns the weight value of the terrain
    * based on hard coded color values
    */
    private float getTerrainWeightByColor32(Color32 col32)
    {
        if (col32.b > 250)              //blue for water
        {
            return 0.1f;
        }
        else if (col32.g >= 250)             // check for lightgreen (grass)
        {
            return 0.25f;
        }
        else if (col32.g <= 106)        // check for darkgreen (forest?)
        {
            return 1.0f;
        }
        else if (col32.g == 160)      // check for grey mountain
        {
            return 3.0f;
        }
        else
        {
            return 0.0f;        // unknown
        }
    }


    /*
   * @Author: Eric Chan aka eepmon
   * @Info: FOR DEBUGGING ONLY!
   * Searches through the 2D array of RGBA and renders (draws) the data into the scene view
   */
    public void drawTerrain()
    {
        //drawPixelData(x - j, y + j, col32);
        for (int x = 0; x < col; x++)
        {
            for (int y = 0; y < row; y++)
            {
                drawPixelData(x, y, pixelData2D[x, y]);
            }
        }
    }

    /*
    * @Author: Eric Chan aka eepmon
    * @Info: FOR DEBUGGING ONLY!
    * Searches through the 2D array of RGBA
    * values for water. This is determined by the 4D weights
    * calculated R, G, B, A values
    */

    public void findWaterFrom4D()
    {
        for (int x = 0; x < 100; x++)
        {
            for (int y = 0; y < 100; y++)
            {
                // check to see if the pixel is indeed weighted towards water
                if (isWaterArea(x, y, pixelData2D[x, y]) == true)
                {
                    Debug.Log("Water Found");
                }
            }
        }
    }

    /*
    * @Author: Eric Chan aka eepmon
    * @Info: FOR DEBUGGING ONLY! OLD STUFF!
    * Returns true/false if the area is of water based on the 
    * value of the incoming Color32
    */
    private bool isWaterArea(int x, int y, Color32 data)
    {
        float forest = data.r/255.0f;
        float plains = data.g / 255.0f;
        float water = data.b / 255.0f;
        float mountain = data.a / 255.0f;

        // find shoreline between these percentage values of water
        if (water > MIN_SHORE_THRESHOLD && water < MAX_SHORE_THRESHOLD)
        {
            
            Debug.Log("-----------------------");
            Debug.Log("forest = " + forest);
            Debug.Log("plains = " + plains);
            Debug.Log("water = " + water);
            Debug.Log("mountain = " + mountain);    // ignore mountains for now
            
            Debug.Log("(" + x + "," + y + "):: water = " + water);
        }
       
        return true;
    }

    /* 
    * @Author: Eric Chan aka eepmon
    * @Info: FOR DEBUGGING ONLY! OLD STUFF!
    * Test to create a row/col of primitive spheres
    * based on pixel grid graphic
    */
    private void drawPixelData2D()
    {
        GameObject spherePx;

        for (int x = 0; x < col; x++)
        {
            for (int y = 0; y < row; y++)
            {
                spherePx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spherePx.name = x + "," + y;
                spherePx.transform.position = new Vector3(x, 0, y);
                colorizeObj(spherePx, pixelData2D[x, y]);
            }
        }
    }
}

/* //////////////////////////////////*/
/* #### END DEBUGGING FUNCTIONS #### */
/* //////////////////////////////////*/
