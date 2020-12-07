using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @Author: Eric Chan aka eepmon
 * @Info: The WaterManager class
 * Detects and determines bodies of water and shorlines
 */
public class WaterManager : MonoBehaviour
{
    private int SIZE_FULL;
    private float MIN_SHORE_THRESHOLD = 0.53f;
    private float MAX_SHORE_THRESHOLD = 0.70f;
    private float WATER_HEIGHT;
    
    public List<WaterDataObject> shoreLines = new List<WaterDataObject>();  // this could be for Nader eventually
    public List<WaterDataObject> allWater   = new List<WaterDataObject>();
    
    public List<GameObject> waterPlanes     = new List<GameObject>();       // holds all water planes in the scene

    private int row, col;

    // for testing purpose until I get the acutal terrainTypeGrid data! 
    private string testImage = "Textures/terrain-200x200-a";
    //private string testImage = "Textures/terrain-monkey-island-200x200";
    private Color[,] colDataDummy;                    
    private Texture2D waterTex;

    // debugging variables
    private Color32[,] pixelData2D;                      // 2D array of the image data (yes that's how you declare a 2D arr)
    private Color32[,] pixelTest = new Color32[3,3];     // for debug

    void Awake()
    {
        print("---------- WaterManager :: Awakened");
    }
    
    void Start()
    {
        print("---------- WaterManager :: Started");
    }

    /*
     * @Author: Eric Chan aka eepmon 
     * @Info: return the shoreline data to the calling program
     */
    public List<WaterDataObject> getShorelines()
    {
        return shoreLines;
    }

    // Alex data go in here and manipulate
    public void initialize(int gridSize, Color[,] terrainTypeGrid)
    {
        print("---------- WaterManager :: initialize");

        // ### make sure texture is read/write enabled! (go to the texture in your resources and find it in the advance dropdown)
        //waterTex = Resources.Load<Texture2D>(testImage);
        //SIZE_FULL = waterTex.width;     // assuming that the grid size will always be a perfect square

        SIZE_FULL = 2049;
        /*
        colDataDummy = new Color[waterTex.width, waterTex.height];

        int row = -1;
        int col = 0;

        // GetPixels() returns a 1D array of RGBA values - convert to 2D so your brain can understand it
        for (int i = 0; i < waterTex.GetPixels().Length; i++)
        {
            if (i % 200 == 0)
            {
                row++;
                col = 0;
            }
            else
            {
                col++;
            }

            colDataDummy[row, col] = waterTex.GetPixels()[i];
        }
        */

        // find shoreline between these percentage values of water
        // small sample cuz it be foreva

        // also create an array that holds the shorline terrain types for NADER

        
        for (int x = 0; x < SIZE_FULL; x++)
        {
            for (int y = 0; y < SIZE_FULL; y++)
            {
                checkForWaterRegion(x, y, terrainTypeGrid[x, y]);     // ### FOR PRODUCTION!!
                //checkForWaterRegion(x, y, colDataDummy[x, y]);     // ### FOR DEBUGGING!!
            }
        }
    }


    /*
     * this is where we manipulate the data based on Alex's values
     * and update the perlineheight data
     * and send it back to calling program
     * aka Monkey in the Middle
     */
    public float[,] getHeights(float[,] perlinHeightData)
    {
        // get the lowest number in the 2D array
        WATER_HEIGHT = getLowestNumIn2DArr(perlinHeightData) - 0.01f;
        
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
    public void createWaterPlanes(Terrain terrainComp)
    {
        float terrainHeight = terrainComp.terrainData.GetHeight(allWater[0].x, allWater[0].y);
        //float bodyWaterX = terrainComp.terrainData.bounds.center[0];
        //float bodyWaterY = terrainComp.terrainData.bounds.center[2];
        float bodyWaterX = 48.0f;
        float bodyWaterY = 52.0f;
        
        print("TERRAIN HEIGHT AT 0,0 = " + terrainHeight);
        print("TERRAIN BOUNDS MIN = " + terrainComp.terrainData.bounds.min);
        print("TERRAIN BOUNDS MAX = " + terrainComp.terrainData.bounds.max);
        print("TERRAIN BOUNDS CENTER = " + terrainComp.terrainData.bounds.center);
        print("TERRAIN BOUNDS SIZE = " + terrainComp.terrainData.bounds.size);
       
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        // hard code the scale values for now during testing (you'll need to adapt it for the whole 2049x2049 later...)
        // DO NEXT :: DON'T SCALE IT, JUST CREATE A QUAD BASED ON COORDS AT SIZE_FULL
        waterPlane.transform.localScale = new Vector3(100.0f, 1.0f, 100.0f);
        waterPlane.transform.position = new Vector3(bodyWaterX, terrainHeight + 0.01f, bodyWaterY);
        
        Renderer waterMaterial = waterPlane.GetComponent<Renderer>(); // grab the renderer component on the plane
        waterMaterial.material.SetColor("_Color", new Color(0.0f,0.0f,1.0f,0.3f));
        
        waterPlanes.Add(waterPlane); 
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
            //Debug.Log("\t WaterManager :: SHORELINE FOUND - DEAL WITH IT");
            // find what type of terrain it is based on strength of the component
            WaterDataObject shoreData = getShoreData(x,y,data);
            
            //WaterDataObject wObj = new WaterDataObject();

            // !!!!!!***** you'll need to have another function to generate the heights for the shoreline
            //wObj.init(x, y, shoreType, 2.4f);
            shoreLines.Add(shoreData);
        }
        else if (water.b > MAX_SHORE_THRESHOLD)
        {
            // it's 100% water go DEEP DIVE!
            //print("\t WaterManager :: 100% WATER FOUND - DEAL WITH IT");
            // save the water into a group (for potential use...maybe not needed)
            WaterDataObject wObj = new WaterDataObject();
            wObj.init(x, y, "water", 0.1f);
            allWater.Add(wObj);
        }else
        {
            print("Error: No Water / Shores Found");
        }
    }

    /*
     * finds out the shoreline type of terrain
     * and returns the name to the calling program
     * shoreline is determined by the weighted values in RGBA
     * Will probably have to split green to capture forest
     */
    private WaterDataObject getShoreData(int x, int y, Color cData)
    {
        WaterDataObject wObj = new WaterDataObject();

        if (cData.r > cData.g && cData.b > cData.g)
        {
            print("Must be TREE ZONE @ " + x + "," + y + " = " + cData);
            // can we just use the R component as the weight itself?
            //wObj.init(x, y, "forest", cData.r);
            wObj.init(x, y, "forest", cData.b);
        }
        else if (cData.g > cData.r && cData.b > cData.r)
        {
            print("Must be GRASS @ " + x + "," + y + " = " + cData);
            //wObj.init(x, y, "grass", cData.g);
            wObj.init(x, y, "grass", cData.b);
        }
        else if(cData.a >= 0.8f)        // play with this value later
        {
            print("Must be MOUNTAIN @ " + x + "," + y + " = " + cData);
            //wObj.init(x, y, "mountain", cData.a);
            wObj.init(x, y, "mountain", cData.b);
        }

        return wObj;
    }


    /* //////////////////////////////////*/
    /* #### START UTILITY FUNCTIONS #### */
    /* //////////////////////////////////*/

    /*
     * @Author: Eric Chan aka EEPMON
     * @Info: returns the lowest number from incoming 2D array
     */
    private float getLowestNumIn2DArr(float[,] arr)
    {
        float lowest = arr[0, 0];
       
        for (int i = 0; i < SIZE_FULL; i++)
        {
            for (int j = 0; j < SIZE_FULL; j++)
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
     * @Author: Eric Chan aka EEPMON
     * @Info: returns the highest number from incoming 2D array
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

            Debug.Log("getWeightAverageAt :::: target x,y = " + x + "," + y);
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
     * @Info: FOR DEBUGGING ONLY!
     * Searches the neighbourhood around x,y coordinates of the
     * pixelData2D array. If it is found, it will draw a sphere with its corresonding terrain colour
     * Using try/catch so that the search space does not fall into ArrayIndexOutOfBounds
     */
    private void srchAndDrwNBsAt(int x, int y, int s = 1)
    {
        // identify the cell (x,y)
        Debug.Log("+++++++++AND NOW WE WILL LOOK A NEIGHBOURS OF (" + x + " , " + y + ") +++++++++");
        
        // s is the search space that cannot exceed SEARCHSPACE_MAX?
        for (int j = 1; j < s; j++)
        {
            // check to see if searching upper left even exists. If so draw it!
            try 
            {
                Color32 col32 = pixelData2D[x - j, y + j];
                drawPixelData(x - j, y + j, col32);
                Debug.Log("Upper Left EXIST @ " + (x - j) + "," + (y + j));
            }
            catch (Exception e) { Debug.Log("Upper Left NOT EXIST @ " + (x - j) + "," + (y + j));}

            // check to see if searching top exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x, y + j];
                drawPixelData(x, y + j, col32);
                Debug.Log("Top EXIST @ " + x  + "," + (y + j));
            }
            catch (Exception e) { Debug.Log("Top NOT EXIST @ " + x + "," + (y + j)); }

            // check to see if searching upper right exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x + j, y + j];
                drawPixelData(x + j, y + j, col32);
                Debug.Log("Upper Right EXIST @ " + (x + j) + "," + (y + j));
            }
            catch (Exception e) { Debug.Log("Upper Right EXIST @ " + (x + j) + "," + (y + j)); }

            // check to see if searching right exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x + j, y];
                drawPixelData(x + j, y, col32);
                Debug.Log("Right EXIST @ " + (x + j) + "," + y);
            }
            catch (Exception e) { Debug.Log("Right EXIST @ " + (x + j) + "," + y); }

            // check to see if searching bottom right exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x + j, y - j];
                drawPixelData(x + j, y - j, col32);
                Debug.Log("Bottom Right EXIST @ " + (x + j) + "," + (y - j));
            }
            catch (Exception e) { Debug.Log("Bottom Right EXIST @ " + (x + j) + "," + (y - j)); }

            // check to see if searching bottom exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x, y - j];
                drawPixelData(x, y - j, col32);
                Debug.Log("Bottom EXIST @ " + x + "," + (y - j));
            }
            catch (Exception e) { Debug.Log("Bottom EXIST @ " + x + "," + (y - j)); }

            // check to see if searching bottom left exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x - j, y - j];
                drawPixelData(x - j, y - j, col32);
                Debug.Log("Bottom Left EXIST @ " + (x-j) + "," + (y - j));
            }
            catch (Exception e) { Debug.Log("Bottom Left EXIST @ " + (x - j) + "," + (y - j)); }

            // check to see if searching left exists. If so draw it!
            try
            {
                Color32 col32 = pixelData2D[x - j, y];
                drawPixelData(x - j, y, col32);
                Debug.Log("Bottom Left EXIST @ " + (x - j) + "," + y);
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
   * @Author: Eric Chan aka EEPMON
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
    * @Author: Eric Chan aka EEPMON
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
    * @Author: Eric Chan aka EEPMON
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
   * @Author: Eric Chan aka EEPMON
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
    * @Author: Eric Chan aka EEPMON
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
    * @Author: Eric Chan aka EEPMON
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
    * @Author: Eric Chan aka EEPMON
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

/* /////////////////////////////////////////*/
/* #### END DEBUGGING FUNCTIONS BELOW. #### */
/* /////////////////////////////////////////*/
