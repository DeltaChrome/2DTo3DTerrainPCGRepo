using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;

/*
 * Author: @Eric Chan aka eepmon
 * The Water Manager!
 */
public class WaterManager : MonoBehaviour
{
    private WaterThresholds waterThresholds;          // aka data object
    public TextAsset imageAsset;
    public Texture2D waterTex;

    public Color32[] pixelData;

    // Random.Range(-variant, variant)

    void Awake()
    {
        Debug.Log("---------- Water Manager :: Awakened");
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("---------- WaterManager :: Started");

        // Create a texture. Texture size does not matter, since
        // LoadImage will replace with with incoming image size.
        //waterTex = new Texture2D(2, 2);
        //waterTex = new Texture2D(20, 20, TextureFormat.RGB24, false);

        //Load a Texture (Assets/Resources/Textures/texture01.png)

        // ### make sure texture is read/write enabled! (go to the texture in your resources and find it in the advance dropdown)
        waterTex = Resources.Load<Texture2D>("Textures/terrain2d");

       
        Debug.Log("waterTex size = (" + waterTex.width + ", " + waterTex.height + ")");

        // 1D Array of the entire RGBA values. Use modulus% to convert to 2Darry?
        pixelData = waterTex.GetPixels32();
    
        for (int x = 0; x < pixelData.Length; x++)
        {
            if(pixelData[x].b > 250)
            {
                Debug.Log("\tBLUE!!! " + pixelData[x]);
            }
        }

        /*
        for (int x = 0; x < waterTex.width; x++)
        {
           // Debug.Log(waterTex.GetPixel(x, 0));
            
            
            for (int y = 0; y < waterTex.height; y++)
            {
                if(waterTex.GetPixel(x, y).b > 0.9)
                {
                    Debug.Log(waterTex.GetPixels32(x, y));
                }
                    

                //Color color = ((x & y) != 0 ? Color.white : Color.gray);
                //texture.SetPixel(x, y, color);
                //Color col = texture.GetPixel(x, y);
                //Debug.Log(waterTex.GetPixel(x,y));
            }
            
        }
        */


        //waterThresholds = null;
    }

    // Update is called once per frame
    /*
    void Update()
    {
        
    }
    */

    public WaterThresholds getWaterThresholds() { return waterThresholds; }
}
