using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

namespace Boxes {



public class Box 
{
    //public SensorDetectBox sdb;
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public void ResetBoxes(Box box)
    {
        // Reset box position
        box.rb.transform.position = box.startingPos;
    }
}



public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public List<Box> boxPool = new List<Box>();

    /// <summary>
    /// The box area, which will be set manually in the Inspector
    /// </summary>
    public GameObject boxArea;


    public void SetUpBoxes() 
    {
        //for each box in json, get a list of box sizes;
        //sizes = readJson(); 

        //temporary box sizes array (to be fed from json later)
        float[][] sizes = new float[][] 
        {
            new float[] { 1.0f, 2.0f, 3.0f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.0f, 2.0f, 3.5f },
            new float[] { 2.0f, 2.0f, 2.0f },
            new float[] { 1.0f, 1.0f, 2.0f },
            new float[] { 3.0f, 4.0f, 4.0f },
            new float[] { 1.0f, 2.0f, 3.5f },
            new float[] { 1.0f, 1.5f, 0,5f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.5f, 0.5f, 0.5f },
            new float[] { 2.0f, 3.0f, 4.0f },
            new float[] { 0.5f, 0.5f, 0.5f },
            new float[] { 1.0f, 2.0f, 3.5f },
            new float[] { 1.0f, 1.5f, 0,5f },
            new float[] { 3.0f, 3.0f, 3.0f },
        };
        
        foreach(var size in sizes) 
        {
            // Create GameObject box
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.localScale = new Vector3(size[0], size[1], size[2]); 
            var position = GetRandomSpawnPos();
            box.transform.position = position;
            // Add compoments to GameObject box
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            box.tag = "0";
            var m_rb = box.GetComponent<Rigidbody>();
            // not be affected by forces or collisions, position and rotation will be controlled directly through script
            //m_rb.isKinematic = true;
            // Transfer GameObject box properties to Box object 
            var newBox = new Box
            {
                rb = box.transform.GetComponent<Rigidbody>(), 
                startingPos = box.transform.position,
                boxSize = box.transform.localScale,
            };
            // Add box to box pool
            boxPool.Add(newBox);          
        }
    }


    public Vector3 GetRandomSpawnPos()
    {
        var areaBounds = boxArea.GetComponent<Collider>().bounds;
        var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
        var randomSpawnPos = boxArea.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
        return randomSpawnPos;
    }
}



}