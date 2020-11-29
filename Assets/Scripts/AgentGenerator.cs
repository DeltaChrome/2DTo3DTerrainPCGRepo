using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class AgentGenerator : MonoBehaviour
{
     public Element[] elements;
    public int elementSize = 25;
    public int elementSpacing = 3; // Every three units in world space, I am going to put the element down
    public Terrain terrain; // terrain to modify


     void Start()
     {

    terrain = Terrain.activeTerrain;
    int  tileValue = 25;
    int   terrainSize = terrain.terrainData.detailWidth;

        for (int x = 0; x < 50; x += tileValue)
        {
            AgentCreator(x);
        }

     }
      void Update()
{
     Element element = elements[0];
    element.GetRandom();
}

void AgentCreator(int tileVariable)
{
    elementSize +=tileVariable;
    Debug.Log("TEST Prefabs Name : " + tileVariable.ToString());
 for (int x = tileVariable; x < elementSize; x += elementSpacing)
 {
     for (int z = tileVariable; z < elementSize; z += elementSpacing)
     {
          Element element = elements[0];
Vector3 position = new Vector3(x,3.0f,z);
GameObject newElement = Instantiate(element.GetRandom());
  newElement.transform.position = position;
     }

 }
}


public Tuple<int, int> GetMultipleValue()
{
     return Tuple.Create(1,2);
}

}



       [System.Serializable]
    public class Element
    {
        public string name; //What type of elements this is.

         [Range(1,10)]
        public float density = 0.05f; //every thr

        public GameObject[] prefabs;

        public bool CanPlace()
        {
            
            //Debug.Log("Elements-Name:  " + name.ToString());
            if (UnityEngine.Random.Range(0, 1) < density)
                return true;
            else
                return false;
        }

        public GameObject GetRandom()
        {
            try{
  //  Debug.Log("TEST Prefabs Name : " + prefabs.GetValue(0).ToString()); // Value 0 = trees,grass=1, rock=2;
            }
            catch
            {

            }
            return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        }


    }