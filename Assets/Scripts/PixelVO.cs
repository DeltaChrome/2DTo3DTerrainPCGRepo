using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Author: @Eric Chan aka eepmon
 * The Data Object properties of the pixel
 */
public class PixelVO 
{
    public Color32 col32;
    public int x = 0;
    public int y = 0;
    public float weight = 0.0f;
    public float weightAverage = 0.0f;  // based on neighbourhood
}
