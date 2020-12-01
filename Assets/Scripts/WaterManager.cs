using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @Author: Eric Chan aka eepmon
 * @Info: The WaterManager class!
 * 
 */
public class WaterManager : MonoBehaviour
{
    private List<TerrainObject> waterGroup = new List<TerrainObject>();       // contains the list of neighbours!

    private float MIN_WATER_THRESHOLD = 0.53f;
    private float MAX_WATER_THRESHOLD = 0.70f;

    private GameObject terraineManagerGO;

    public int row, col;

    public TextAsset imageAsset;
    public Texture2D waterTex;
    
    public Color32[,] pixelData2D;                      // 2D array of the image data (yes that's how you declare a 2D arr)
    public Color32[,] pixelTest = new Color32[3,3];     // for debug


    void Awake()
    {
        Debug.Log("---------- Water Manager :: Awakened");
    }


    /*
    * @Author: Eric Chan aka EEPMON
    * @Info: WaterManager initialization
    */
    void Start()
    {
        Debug.Log("---------- WaterManager :: Started");

        // ### make sure texture is read/write enabled! (go to the texture in your resources and find it in the advance dropdown)
        waterTex = Resources.Load<Texture2D>("Textures/terrain-200x200-a");
        
        // init the 2D array with waterText size
        row = waterTex.height;
        col = waterTex.width;

        pixelData2D = new Color32[row,col];


        // TEST
        //terrainObj = terrainObject.GetComponent();

        //Debug.Log("waterTex size = (" + row + ", " +col + ")");

        //Debug.Log("HELLO")

        int x = 0;
        int y = -1;
       
        // GetPixels32() returns a 1D array of RGB values
        for (int i = 0; i < waterTex.GetPixels32().Length; i++)
        {
            if (i%200 == 0)
            {
                x = 0;
                y++;
            }
            else
            {
                x++;
            }
            
            pixelData2D[x, y] = waterTex.GetPixels32()[i];
        }

        //findWater();        // this is originally used for the 20x20 pixel array that Jacob created (now defunct)
        findWaterFrom4D();    // this is the 2nd version which looks at Vector4D and determines if this is indeed water based on 4D weights in the array
        //drawTerrain();
    }

    // alex data go here and manipulate
    public void initialize()
    {
        Debug.Log("WaterManger.initialize");
    }

    public float[,] applyWaterHeights(float[,] perlinNoiseArrayFinalized)
    {
        Debug.Log("WaterManger.applyWaterHeights");
        return perlinNoiseArrayFinalized;
    }

    public void drawTerrain()
    {
        //drawPixelData(x - j, y + j, col32);
        for (int x = 0; x < col; x++)
        {
            for (int y = 0; y < row; y++)
            {
                drawPixelData(x, y, pixelData2D[x,y]);
            }
        }
    }

    /*
    * @Author: Eric Chan aka EEPMON
    * @Info:  searches through the 2D array of RGBA
    * values for water. This is determined by the 4D weights
    * calculated R, G, B, A values
    */
    public void findWaterFrom4D()
    {
        for (int x = 50; x < 150; x++)
        {
            for (int y = 50; y < 150; y++)
            {
                // check to see if the pixel is indeed weighted towards water
                if(isWaterArea(x,y,pixelData2D[x, y]) == true)
                {
                    //Debug.Log("Water Found");
                }
            }
        }
    }

    /*
    * @Author: Eric Chan aka EEPMON
    * @Info: searches through the 2D array of RGB
    * values for water. Once the water cell is identified
    * it proceeds to look at its 8 adjacent neighbours with 
    * search space of 1
    */
    public void findWater()
    {
        //Debug.Log("col = " + col + " :: row = " + row);

        for (int x = 0; x < col; x++)
        {
            for (int y = 0; y < row; y++)
            {
                // checks to see if the pixel is water
                if (pixelData2D[x, y].b > 250)
                {
                    // water isolated, draw it!
                    drawPixelData(x, y, pixelData2D[x, y]);
                    
                    // save the water into a group (for potential use...maybe not needed)
                    TerrainObject waterObj = new TerrainObject();
                    waterObj.init(x,y, pixelData2D[x, y],"water",0.1f);
                    waterGroup.Add(waterObj);

                    // now look for adjacent cell of n size
                    srchAndDrwNBsAt(x, y, 4);
                }
            }
        }
        
        float weightAvg = getWeightAverageAt(11,16, 5);
        //Debug.Log("WEIGHT VALUE AT 11,16 = " + weightAvg);
    }

    /*
     * @Author: Eric Chan a.k.a.EEPMON
     * @Info: give the target x,y and search space value
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
            //Debug.Log(j + " finalWeightAvg = " + finalWeightAvg);
        }

        return finalWeightAvg;
    }


    /*
     * @Author: Eric Chan aka EEPMON
     * @Info: searches the neighbourhood around x,y coordinates of the
     * pixelData2D array. If it is found, it will draw a sphere with
     * its corresonding terrain colour
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
     * @Info: Draws the pixel into the scene at specified location and colour
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
   * @Info: Colours the game object
   * 
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
    * based on hard coded color values
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


    private bool isWaterArea(int x, int y, Color32 data)
    {
        float forest = data.r/255.0f;
        float plains = data.g / 255.0f;
        float water = data.b / 255.0f;
        float mountain = data.a / 255.0f;

        // find shoreline between these percentage values of water
        if (water > MIN_WATER_THRESHOLD && water < MAX_WATER_THRESHOLD)
        {
            /*
            Debug.Log("-----------------------");
            Debug.Log("forest = " + forest);
            Debug.Log("plains = " + plains);
            Debug.Log("water = " + water);
            Debug.Log("mountain = " + mountain);    // ignore mountains for now
            */
            Debug.Log("(" + x + "," + y + "):: water = " + water);
        }
       
        return true;
    }

    /* ------------ FOR DEBUGGING ------------ */

    /* 
    * @Author: Eric Chan aka EEPMON
    * @Info: For Debugging
    * Test to create a row/col of primitive spheres
    * based on pixel grid graphic
    */
    public void drawPixelData2D()
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
