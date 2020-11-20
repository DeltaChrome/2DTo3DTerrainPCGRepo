using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Author: @Eric Chan aka eepmon
 * The Data Object that will store
 * all neccesary values 
 */
public class TerrainObject
{
    public int x = 0;
    public int y = 0;
    public string type = "";
    public Color32 col32;
    
    public float weight = 0.0f;


    public void init(int x, int y, Color32 col32, string type, float weight)
    {
        this.x = x;
        this.y = y;
        this.col32 = col32;
        this.type = type;
        this.weight = weight;
    }

    /*
     * Author: Eric Chan
     * code = specifies which cell to instantiate
     * x = x coord of the neighbour
     * y = y coord of the neighbour
     * col32 = the colour of the neighbout
     */
     /*
    public void hasNeighbourLocatedAt(int code, int x, int y, Color32 col32)
    {
        switch (code)
        {
            case 0:
                upperLeftOfMe = createPixelVO(x, y, col32);
                break;
            case 1:
                topOfMe = createPixelVO(x, y, col32);
                break;
            case 2:
                upperRightOfMe = createPixelVO(x, y, col32);
                break;
            case 3:
                rightOfMe = createPixelVO(x, y, col32);
                break;
            case 4:
                bottomRightOfMe = createPixelVO(x, y, col32);
                break;
            case 5:
                bottom = createPixelVO(x, y, col32);
                break;
            case 6:
                bottomLeftOfMe = createPixelVO(x, y, col32);
                break;
            case 7:
                leftOfMe = createPixelVO(x, y, col32);
                break;
            default:
                break; 
        }
    }
    */

    /*
    * @Author: Eric Chan
    * @Info: creates the pixel value object to 
    * hold its data
    */
    /*
    private PixelVO createPixelVO(int x, int y, Color32 col32)
    {
        PixelVO pxVO = new PixelVO();
        pxVO.col32 = col32;
        pxVO.x = x;
        pxVO.y = y;
        pxVO.weight = getWeightFromColor32(col32);

        return pxVO;
    }
    */

    /*
     * @Author: Eric Chan
     * @Info: gets the weight value depending on the colour
     * of the pixel
     * 
     * !!!!NOTE: Eventually we want interpolate weight instead of fix values
     * So these hardcoded values will most likley be changed in the future
     */
    /*
    private float getWeightFromColor32(Color32 col32)
    { 
        if (col32.b > 250)              // check for water
        {
            return -0.25f;
        }
        else if (col32.g >= 250)        // check for lightgreen (grass)
        {
            return 0.25f;
        }
        else if (col32.g <= 106)        // check for darkgreen (forest?)
        {
            return 0.75f;
        }
        else if (col32.g == 160)       // check for grey mountain
        {
            return 1.5f;
        }
        else
        {
            return 0.0f;
        } 
    }
    */
}