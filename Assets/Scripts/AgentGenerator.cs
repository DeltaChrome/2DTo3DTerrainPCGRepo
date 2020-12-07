using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AgentGenerator : MonoBehaviour
{
    public Element[] elements;
    public int elementSize = 25;
    public int elementSpacing = 3; // Every three units in world space, I am going to put the element down
    public Terrain terrain; // terrain to modify


    void Start()
    {
        /*Terrain terrainTemp = Terrain.activeTerrain;
            var temp = terrainTemp.terrainData;
                     Vector3 scale=temp.heightmapScale;
                     int x=300;
                     int z= 200;
          float hTemp = (float)temp.GetHeight((int)(x/scale.x),(int)(z/scale.z));

            Debug.Log("##Test Heightmap"+hTemp);
            Vector3 worldPosition = new Vector3(x,hTemp,z);

          GameObject temp1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
         float height = (float)(worldPosition.y*0.015);
         worldPosition.y = (worldPosition.y)+height;
           Debug.Log("##Test Y value"+ worldPosition.y);
             temp1.transform.position = worldPosition;
             */
        int pixelSize = 10;
        //float terrainSize = terrain.terrainData.size.z;
        float terrainSize = 1000;
        int x_tempValue = 1;
        int z_tempValue = 1;
        Color[,] terrainTypeGrid = GetComponent<TerrainManager>().terrainTypeGrid;

        //Debug.Log("##TEST terrain :" + terrainTypeGrid[1000,1000]);

        //while(x_tempValue<terrainSize)
        {
            for (int x = x_tempValue; x < terrainSize; x += 30)
            {
                for (int z = z_tempValue; z < terrainSize; z += 20)
                {
                    AgentGenAlgorithm(x, z);
                }

            }
        }

        //ColorChange();
        //AgentGenAlgorithm(Random.Range(1,900),Random.Range(1,900));

    }
    void Update()
    {

    }

    void ColorChange()
    {

        Color[,] terrainTypeGrid = GetComponent<TerrainManager>().terrainTypeGrid;

        Texture2D texture = new Texture2D(128, 128);

        GetComponent<Renderer>().material.mainTexture = texture;

        Color color = Color.gray;

        texture.SetPixel(128, 128, color);

        texture.Apply();


    }

    void AgentGenAlgorithm(int xTile, int zTile)
    {
        Color[,] terrainTypeGrid = GetComponent<TerrainManager>().terrainTypeGrid;
        int elementIndex = GettingColor(terrainTypeGrid[xTile, zTile]);
        Element element = elements[elementIndex];

        //   for (int i = 0; i < elements.Length; i++)
        // {
        if (element.CanPlace())
        {
            Vector3 position = new Vector3(xTile, heightValue(xTile, zTile), zTile);
            Vector3 offset = new Vector3(Random.Range(-0.75f, 1.75f), 0f, Random.Range(-0.75f, 1.75f));
            Vector3 rotation = new Vector3(Random.Range(0f, 5f), Random.Range(0, 360f), Random.Range(0f, 5f));

            //The first value of scale and yPosition should have the same amount
            Vector3 scale = Vector3.one * Random.Range(2.3f, 5.3f);

            GameObject newElement = Instantiate(element.GetRandom());
            newElement.transform.SetParent(transform);
            newElement.transform.position = position + offset;
            newElement.transform.eulerAngles = rotation;
            newElement.transform.localScale = scale;
            // break;
        }
        //  }
    }




    private float heightValue(int x, int z)
    {
        Terrain terrainTemp = Terrain.activeTerrain;
        var temp = terrainTemp.terrainData;
        Vector3 scale = temp.heightmapScale;
        float hTemp = (float)temp.GetHeight((int)(x / scale.x), (int)(z / scale.z));
        Vector3 worldPosition = new Vector3(x, hTemp, z);
        float height = (float)(worldPosition.y * 0.015);
        //worldPosition.y = (worldPosition.y)+height;
        return worldPosition.y;
    }


    [Obsolete]
    void AgentCreator(int tileVariable, int elementIndex)
    {
        elementSize += tileVariable;
        //  Debug.Log("TEST Prefabs Name : " + tileVariable.ToString());
        Vector3 scale = terrain.terrainData.heightmapScale;
        Vector3 locationTerrain = new Vector3(0, terrain.terrainData.GetHeight((int)(tileVariable / scale.y), (int)(tileVariable / scale.z)));

        //Debug.Log("TEST terrain :" + locationTerrain.y+"  "+ locationTerrain.z);

        Debug.Log("TEST terrain :" + locationTerrain.ToString());

        for (int x = tileVariable; x < elementSize; x += elementSpacing)
        {
            for (int z = 7; z < elementSize; z += elementSpacing)
            {
                Element element = elements[elementIndex];
                double j = locationTerrain.y + (locationTerrain.y) * 0.16;
                Vector3 position = new Vector3(x, (int)j, z);
                GameObject newElement = Instantiate(element.GetRandom());
                newElement.transform.position = position;



                float yPosition = 24.0f;
                /*    for (int i = 0; i < elements.Length; i++)
                    {
                       // Element element1 = elements[i];
                        if (element.CanPlace())
                        {

                                Vector3 position1 = new Vector3(x,yPosition,z);
                                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.75f, 0.75f), 0f, Random.Range(-0.75f, 0.75f));
                                Vector3 rotation = new Vector3(UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(0, 360f), Random.Range(0f, 5f));

                                //The first value of scale and yPosition should have the same amount
                                Vector3 scale1 = Vector3.one * Random.Range(2.3f, 2.3f); 

                                GameObject newElement1 = Instantiate(element.GetRandom());
                                newElement.transform.SetParent(transform);
                               newElement.transform.position = position + offset;
                                newElement.transform.eulerAngles = rotation;
                                newElement.transform.localScale = scale;
                                break;
                        }
                    }*/

            }

        }
    }


    public int GettingColor(Color32 rgba)
    {
        //determining the maximum color value
        int elementIndex = 0;
        //Debug.Log("TEST Color MaxValue: " + maxValue.ToString());

        // Debug.Log(Mathf.Max(rgba[0].r,rgba[0].g, rgba[0].b,rgba[0].a));

        // it determines the index value for determining the type of element
        List<int> colorListIndex = new List<int>();
        colorListIndex.Add(rgba.r);
        colorListIndex.Add(rgba.g);
        colorListIndex.Add(rgba.b);
        colorListIndex.Add(rgba.a);


        int maxIndexTest = colorListIndex.ToList().IndexOf(colorListIndex.Max());
        //determine the index value
        // Debug.Log("TEST Index : " + maxIndexTest.ToString());
        if (maxIndexTest == 0)
        {
            elementIndex = 0; //If element is tree
        }
        else if (maxIndexTest == 1)
        {
            elementIndex = 1; //If element is Grass
        }
        else if (maxIndexTest == 3)
        {
            elementIndex = 2; //If element is Rock
        }
        else
        {
            elementIndex = 3;
        }


        return elementIndex;
    }

    public Tuple<int, int> GetMultipleValue()
    {
        return Tuple.Create(1, 2);
    }


    public int funcDensity(int desnityValue)
    {
        // The range RGBA between (0-180,200-255,0-180) to define amount of density

        //Density low
        if (desnityValue > 200)
        {
            return 500;
        }
        //Density high
        if (desnityValue < 200)
        {
            return 1;
        }
        return 0;
    }




}



[System.Serializable]
public class Element
{
    public string name; //What type of elements this is.

    [Range(1, 10)]
    public float density = 0.05f; //every thr

    public GameObject[] prefabs;

    public bool CanPlace()
    {

        //Debug.Log("Elements-Name:  " + name.ToString());
        if (UnityEngine.Random.Range(1, 10) < density)
            return true;
        else
            return false;
    }

    public GameObject GetRandom()
    {
        return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
    }


}