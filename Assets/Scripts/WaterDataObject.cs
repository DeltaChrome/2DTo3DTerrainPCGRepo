using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @Author: Eric Chan aka eepmon
 * @Info: The Data Object that will store all neccesary 
 * values for shorline and water implementation
 */
public class WaterDataObject
{
    public int x = 0;
    public int y = 0;
    
    public string type = "";
    public float weight = 0.0f;
    public float yPos = 0;

    public void init(int x, int y, string type, float weight)
    {
        this.x = x;
        this.y = y;
        this.type = type;
        this.weight = weight;
    }
}