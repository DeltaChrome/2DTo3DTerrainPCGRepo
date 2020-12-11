
using System.ComponentModel;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentGenerator : MonoBehaviour
{
    public Element[] elements;
    private float density;
    //determining the maximum color value
    private int elementIndex = 0;
    private Color[,] terrainTypeGrid;

    float waterThreshold = 0;

    /*
         These variables are related to "IntiateAgentGenerator" function
    */
    //This variables need to be change to have a different view
    int x_tempValue = 0;  //Start point, which means the first value of terrainTypeGrid
    int z_tempValue = 0;  //Start point, which means the first value of terrainTypeGrid
    float terrainSize = 2049; //The size of terrain

    int inc = 0;//:)

    /*
       these can be changed based on what to you want to see, for example, how many place left then put the tree 
    */
    float x_elemntSpacing = 50;
    float z_elemntSpacing = 40;

    const float maxSpacingTrees = 40;
    const float minSpacingTrees = 12;

    const float maxSpacingGrass = 15;
    const float minSpacingGrass = 8;

    const float maxSpacingRocks = 40;
    const float minSpacingRocks = 20;

    //REMOVE LATER
    float[,] heightMapArray = new float[2049, 2049];

    void Start()
    {

    }


    public void IntiateAgentGenerator(Color[,] terrainTypeGridTemp, float wt, float[,] heightMapArrayT)
    {
        terrainTypeGrid = terrainTypeGridTemp;

        waterThreshold = wt;

        //remove this line later
        heightMapArray = heightMapArrayT;

        for (int x = x_tempValue; x < terrainSize; x += (int)x_elemntSpacing)
        {
            for (int z = z_tempValue; z < terrainSize; z += (int)z_elemntSpacing)
            {
                AgentGenAlgorithm(x, z);

            }

        }

    }

    void AgentGenAlgorithm(int xTile, int zTile)
    {

        int elementIndex = ElementRecognization(terrainTypeGrid[xTile, zTile]);

        if (elementIndex < 3)
        {
            Element element = elements[elementIndex];

            //z_elemntSpacing = ((100.0f - density) * 10) + 10;

            //z_elemntSpacing = CanPlace() * 100.0f;
            //if (CanPlace() > 0)
            //{

            float heightOffSet = 0;

            if (elementIndex == 2)
            {
                heightOffSet = -1.5f;

            }


            Vector3 offset = new Vector3(Random.Range(-5.0f, 5.0f), heightOffSet, Random.Range(-5.0f, 5.0f));
   
            //Vector3 position = new Vector3(xTile / 2.049f + offset[0], heightValue(xTile / 2.049f + offset[0], zTile / 2.049f + offset[2]), zTile / 2.049f + offset[2]);
            Vector3 position = new Vector3(Mathf.Clamp(xTile / 2.049f + offset[0], Terrain.activeTerrain.GetPosition().x, Terrain.activeTerrain.terrainData.size.x), heightValue(xTile / 2.049f + offset[0], zTile / 2.049f + offset[2]), Mathf.Clamp(zTile / 2.049f + offset[2], Terrain.activeTerrain.GetPosition().z, Terrain.activeTerrain.terrainData.size.z));


            Vector3 rotation;
            if (elementIndex > 0)
            {
                float slope = Terrain.activeTerrain.terrainData.GetSteepness(position.x / Terrain.activeTerrain.terrainData.size.x, position.z / Terrain.activeTerrain.terrainData.size.z);
            
                Vector3 slopeNormal = Terrain.activeTerrain.terrainData.GetInterpolatedNormal(position.x / Terrain.activeTerrain.terrainData.size.x, position.z / Terrain.activeTerrain.terrainData.size.z);

                rotation = new Vector3(Quaternion.FromToRotation(Vector3.up,slopeNormal).x * slope, Quaternion.FromToRotation(Vector3.up,slopeNormal).y * slope, Quaternion.FromToRotation(Vector3.up,slopeNormal).z * slope);
            } else {
                float slopeModToPos = Terrain.activeTerrain.terrainData.GetSteepness(position.x / Terrain.activeTerrain.terrainData.size.x, position.z / Terrain.activeTerrain.terrainData.size.z) / 100;
                position.y -= slopeModToPos;

                rotation = new Vector3(0, Random.Range(0, 360f), 0);
            }


            //The first value of scale and heightValue should have the same amount
            Vector3 scale = Vector3.one * Random.Range(0.8f, 1.8f);

            GameObject newElement = Instantiate(element.GetRandom());
            newElement.transform.SetParent(transform);
            newElement.transform.position = position;
            newElement.transform.eulerAngles = rotation;
            newElement.transform.localScale = scale;
            //}
        }
    }



    // Determine the correct HightValue
    //Values of xWeighted and zWeighted or between 0 and 1
    private float heightValue(float xWeighted, float zWeighted)
    {
        Terrain terrainTemp = Terrain.activeTerrain;
        var temp = terrainTemp.terrainData;
        Vector3 scale = temp.heightmapScale;

        float hTemp = (float)temp.GetHeight((int)Mathf.Round(xWeighted / scale.x), (int)Mathf.Round(zWeighted / scale.z));


        //if (inc < 20)
        //{
        //    print(temp.GetHeight((int)(xWeighted / scale.x), (int)(zWeighted / scale.z)));
        //    print(heightMapArray[(int)(xWeighted * 2.049f), (int)(zWeighted * 2.049f)]);
        //}

        //inc++;

        Vector3 worldPosition = new Vector3(xWeighted, hTemp, zWeighted);
        //float height = (float)(worldPosition.y * 0.015);
        //worldPosition.y = (worldPosition.y)+height;
        return worldPosition.y;
    }


    public int ElementRecognization(Color32 rgba)
    {
        // it determines the index value for determining the type of element
        List<int> colorListIndex = new List<int>();
        colorListIndex.Add(rgba.r);
        colorListIndex.Add(rgba.g);
        colorListIndex.Add(rgba.b);
        colorListIndex.Add(rgba.a);

        int maxIndexTest;

        //If not water
        if (rgba.b <= waterThreshold)
        {

            //Check percentages to allow a gradient
            float sum = rgba.r + rgba.g + rgba.a;

            float pForest = rgba.r / sum;
            //float pMountain = rgba.a / sum;
            float pGrass = rgba.g / sum;

            float picker = UnityEngine.Random.Range(0.0f, 1.0f);

            //pick tree
            if (picker < pForest)
            {
                maxIndexTest = 0;
            }
            else if (picker - pForest < pGrass)//pick plant
            {
                maxIndexTest = 1;
            }
            else//default to rock kinda
            {
                maxIndexTest = 2;
            }
        }
        else
        {
            maxIndexTest = 4;
        }
        




        //int maxIndexTest = colorListIndex.ToList().IndexOf(colorListIndex.Max());
        /*determine the index value, which index represent one element, for example: 
        if R has a heighest value, the element index will be zero, which it indicates the Tree
        */



        if (maxIndexTest == 0)
        {
            elementIndex = 0; //If element is tree
            density = funcDensity(colorListIndex[maxIndexTest]);
            //print("Density" + density);

            x_elemntSpacing = Mathf.Max((1.0f - density) * maxSpacingTrees, minSpacingTrees);
            z_elemntSpacing = Mathf.Max((1.0f - density) * maxSpacingTrees, minSpacingTrees);
        }
        else if (maxIndexTest == 1)
        {
            elementIndex = 1; //If element is Grass
            density = funcDensity(colorListIndex[maxIndexTest]);

            x_elemntSpacing = Mathf.Max((1.0f - density) * maxSpacingGrass, minSpacingGrass);
            z_elemntSpacing = Mathf.Max((1.0f - density) * maxSpacingGrass, minSpacingGrass);
        }
        else if (maxIndexTest == 2)
        {
            elementIndex = 2; //If element is Rock
            density = funcDensity(colorListIndex[maxIndexTest]);

            x_elemntSpacing = Mathf.Max((1.0f - density) * maxSpacingRocks, minSpacingRocks);
            z_elemntSpacing = Mathf.Max((1.0f - density) * maxSpacingRocks, minSpacingRocks);
        }
        else
        {
            elementIndex = 3;
        }


        
        //if (((100.0f - density) + 10) >= 40)
        //{
        //    x_elemntSpacing = 40;
        //}
        //else
        //{
        //    x_elemntSpacing = ((100.0f - density)) + 10;
        //}

        //if (((100.0f - density) + 10) >= 90)
        //{
        //    z_elemntSpacing = 70;
        //}
        //else
        //{
        //    z_elemntSpacing = ((100.0f - density)) + 10;
        //}


        return elementIndex;
    }

    // This function calculate the percentage of density
    public float funcDensity(float desnityValue)
    {
        return ((desnityValue / 255));
    }


    //public float CanPlace()
    //{

    //    //Adequate density for the first element => Tree
    //    if (elementIndex == 0)
    //    {
    //        //  if (UnityEngine.Random.Range(0,100)<density)
    //        if (density > 95)
    //        {
    //            return UnityEngine.Random.Range(0.95f, 1.0f);
    //        }
    //        else if (density > 90)
    //        {
    //            return UnityEngine.Random.Range(0.90f, 1.0f);
    //        }
    //        else if (density > 80)
    //        {
    //            return UnityEngine.Random.Range(0.80f, 1.0f);
    //        }
    //        else if (density > 50)
    //        {

    //            return UnityEngine.Random.Range(0.5f, 1.0f);
    //        }
    //        else if (density > 30)
    //        {

    //            return UnityEngine.Random.Range(0.30f, 1.0f);
    //        }
    //        else if (density > 10)
    //        {
    //            return UnityEngine.Random.Range(0.1f, 1.0f);
    //        }
    //    }
    //    //Adequate density for the second element => Grass
    //    if (elementIndex == 1)
    //    {
    //        if (density > 95)
    //        {
    //            return UnityEngine.Random.Range(0.95f, 1.0f);
    //        }
    //        else if (density > 90)
    //        {
    //            return UnityEngine.Random.Range(0.90f, 1.0f);
    //        }
    //        else if (density > 80)
    //        {
    //            return UnityEngine.Random.Range(0.80f, 1.0f);
    //        }
    //        else if (density > 50)
    //        {

    //            return UnityEngine.Random.Range(0.5f, 1.0f);
    //        }
    //        else if (density > 30)
    //        {

    //            return UnityEngine.Random.Range(0.30f, 1.0f);
    //        }
    //        else if (density > 10)
    //        {
    //            return UnityEngine.Random.Range(0.1f, 1.0f);
    //        }
    //    }

    //    //Adequate density for the second element => Rock
    //    if (elementIndex == 2)
    //    {
    //        if (density > 95)
    //        {
    //            return UnityEngine.Random.Range(0.95f, 1.0f);
    //        }
    //        else if (density > 90)
    //        {
    //            return UnityEngine.Random.Range(0.90f, 1.0f);
    //        }
    //        else if (density > 80)
    //        {
    //            return UnityEngine.Random.Range(0.80f, 1.0f);
    //        }
    //        else if (density > 50)
    //        {

    //            return UnityEngine.Random.Range(0.5f, 1.0f);
    //        }
    //        else if (density > 30)
    //        {

    //            return UnityEngine.Random.Range(0.30f, 1.0f);
    //        }
    //        else if (density > 10)
    //        {
    //            return UnityEngine.Random.Range(0.1f, 1.0f);
    //        }
    //    }
    //    return 0.0f;
    //}


}



[System.Serializable]
public class Element
{
    public string name; //What type of elements this is.

    [Range(1, 10)]
    public float density = 0.05f; //every thr

    public GameObject[] prefabs;

    /*
    Different vegetation is done, now terrain will have different vegetation,
     which the algorithm is selected randomly.
    */
    public GameObject GetRandom()
    {
        return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
    }


}