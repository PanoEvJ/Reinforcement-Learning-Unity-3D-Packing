using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;



namespace Boxes {



public class Box
{
    public Rigidbody rb;

    public Vector3 startingPos; // for box reset, constant 

    public Quaternion startingRot; // for box reset, constant

    public Vector3 startingSize; // for box reset, constant 

    public Vector3 boxSize; // for sensor, changes after selected action

    public Quaternion boxRot; // for sensor, changes after selected action

    public Vector3 boxVertex; // for sensor, changes after selected action

    public bool isOrganized = false; 

    public GameObject gameobjectBox;
}


// public class Blackbox
// {
//     public Vector3 position;
//     public Vector3 vertex;
//     public float volume;
//     public Vector3 size;

//     public GameObject gameobjectBlackbox;
// }


[System.Serializable]
public class BoxSize
{
    public Vector3 box_size;
}

public class Item
{
    public int Product_id { get; set; }
    public double Length { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int Quantity { get; set; }
}


// Spawns in boxes with sizes from a json file
public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public List<Box> boxPool = new List<Box>();

    // The box area, which will be set manually in the Inspector
    public GameObject boxArea;
    
    public GameObject unitBox; 

    public int maxBoxQuantity= 50; // max number of boxes in the scene; default of 50 here is arbitrary
    private bool usePadding = false; // if true, will pad the json file with zeros to maxBoxQuantity; use when using one-hot encoding in packerhand.cs

    public BoxSize [] sizes;

    [HideInInspector] public int idx_counter = 0;

    string homeDir;




    public void Start()
    {
        homeDir = Environment.GetEnvironmentVariable("HOME");
    }

    public void SetUpBoxes(string box_type, bool pickRandom, int num_boxes_x, int num_boxes_y, int num_boxes_z, int seed)
    {
        if (box_type == "uniform_random")
        {
            RandomBoxGenerator("uniform_random", pickRandom, num_boxes_x, num_boxes_y, num_boxes_z, seed);
            // Read random boxes using existing ReadJson function
            ReadJson($"{homeDir}/Unity/data/Boxes_RandomUniform.json");
            if (usePadding){PadZeros();}
            // Delete the created json file to reuse the name next iteration
            File.Delete($"{homeDir}/Unity/data/Boxes_RandomUniform.json");

        }
        else if (box_type == "mix_random")
        {
            RandomBoxGenerator("mix_random", pickRandom, num_boxes_x, num_boxes_y, num_boxes_z, seed);
            // Read random boxes using existing ReadJson function
            ReadJson($"{homeDir}/Unity/data/Boxes_RandomMix.json");
            if (usePadding){PadZeros();}
            // Delete the created json file to reuse the name next iteration
            File.Delete($"{homeDir}/Unity/data/Boxes_RandomMix.json");
        }
        else
        {
            ReadJson($"{homeDir}/Unity/data/{box_type}.json");
            if (usePadding){PadZeros();}
        }

        var idx = 0;
        foreach(BoxSize s in sizes) 
        {
            Vector3 box_size = new Vector3(s.box_size.x, s.box_size.y, s.box_size.z);
            // if box is not of size zeros
            if (box_size.x != 0) 
            {
                // Create GameObject box
                var position = GetRandomSpawnPos();
                GameObject box = Instantiate(unitBox, position, Quaternion.identity);
                box.transform.localScale = box_size;
                box.transform.position = position;
                // Add compoments to GameObject box
                box.AddComponent<Rigidbody>();
                box.AddComponent<BoxCollider>();
                box.name = idx.ToString();
                var m_rb = box.GetComponent<Rigidbody>();
                Collider [] m_cList = box.GetComponentsInChildren<Collider>();
                foreach (Collider m_c in m_cList) 
                {
                    m_c.isTrigger = true;
                }
                // not be affected by forces or collisions, position and rotation will be controlled directly through script
                m_rb.isKinematic = true;
                // Transfer GameObject box properties to Box object 
                var newBox = new Box
                {
                    rb = box.GetComponent<Rigidbody>(), 
                    startingPos = box.transform.position,
                    startingRot = box.transform.rotation,
                    startingSize = box.transform.localScale,
                    boxSize = box.transform.localScale,
                    boxRot = box.transform.rotation,
                    gameobjectBox = box,
                };
                // Add box to box pool
                boxPool.Add(newBox);  
                idx+=1;     
            }
        }
        // // Create sizes_American_pallets = new float[][] { ... }  48" X 40" = 12.19dm X 10.16dm 
        // // Create sizes_EuropeanAsian_pallets = new float[][] { ... }  47.25" X 39.37" = 12dm X 10dm
        // // Create sizes_AmericanEuropeanAsian_pallets = new float[][] { ... }  42" X 42" = 10.67dm X 10.67dm
    }


    public Vector3 GetRandomSpawnPos()
    {
        var areaBounds = boxArea.GetComponent<Collider>().bounds;
        var randomPosX = UnityEngine.Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        var randomPosZ = UnityEngine.Random.Range(areaBounds.extents.z, areaBounds.extents.z);
        var randomSpawnPos = boxArea.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
        return randomSpawnPos;
    }

    public void RandomBoxGenerator(string box_type, bool pickRandomNumberofBoxes, int num_boxes_x, int num_boxes_y, int num_boxes_z, int seed)
    {
        if (box_type == "uniform_random" || box_type == "mix_random") 
        {
            float bin_z = 59f;
            float bin_x = 23.5f;
            float bin_y = 23.9f;
            UnityEngine.Random.InitState(seed);
            int random_num_x;
            int random_num_y;
            int random_num_z;
            if (pickRandomNumberofBoxes)
            {
                random_num_x =  UnityEngine.Random.Range(1, num_boxes_x);
                random_num_y =  UnityEngine.Random.Range(1, num_boxes_y);
                random_num_z =  UnityEngine.Random.Range(1, num_boxes_z);
            }
            else
            {
                random_num_x = num_boxes_x;
                random_num_y = num_boxes_y;
                random_num_z = num_boxes_z;
            }
            List<Item> items = new List<Item>();
            
            if (box_type == "uniform_random"){
                float x_dimension =  (float)Math.Floor(bin_x/random_num_x * 100)/100;
                float y_dimension =  (float)Math.Floor(bin_y/random_num_y * 100)/100;
                float z_dimension = (float)Math.Floor(bin_z/random_num_z * 100)/100;
                Debug.Log($"RUF RANDOM UNIFORM BOX NUM: {random_num_x*random_num_y*random_num_z} | x:{x_dimension} y:{y_dimension} z:{z_dimension}");

                items.Add(new Item
                {
                    Product_id = 0,
                    Length = z_dimension,
                    Width = x_dimension,
                    Height = y_dimension,
                    Quantity = random_num_x*random_num_y*random_num_z
                });
                // Create a new object with the Items list
                var data = new { Items = items };
                // Serialize the object to json
                var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                // Write the json to a file
                File.WriteAllText($"{homeDir}/Unity/data/Boxes_RandomUniform.json", json);
            }
            else if (box_type == "mix_random")
            {
                List<float> x_dimensions = new List<float>();
                List<float> y_dimensions = new List<float>();
                List<float> z_dimensions = new List<float>();

                x_dimensions.Add(bin_x);
                while (x_dimensions.Count<random_num_x)
                {
                    float largest = x_dimensions.Max();
                    float newPiece = UnityEngine.Random.Range(1, largest);
                    x_dimensions.Remove(largest);
                    x_dimensions.Add(newPiece);
                    x_dimensions.Add(largest - newPiece);
                }
                y_dimensions.Add(bin_y);
                while (y_dimensions.Count<random_num_y)
                {
                    float largest = y_dimensions.Max();
                    float newPiece = UnityEngine.Random.Range(1, largest);
                    y_dimensions.Remove(largest);
                    y_dimensions.Add(newPiece);
                    y_dimensions.Add(largest - newPiece);
                }
                z_dimensions.Add(bin_z);
                while (z_dimensions.Count<random_num_z)
                {
                    float largest = z_dimensions.Max();
                    float newPiece = UnityEngine.Random.Range(1, largest);
                    z_dimensions.Remove(largest);
                    z_dimensions.Add(newPiece);
                    z_dimensions.Add(largest - newPiece);
                }
                int id = 0;
                for (int x=0; x<x_dimensions.Count; x++){
                    for (int y=0; y<y_dimensions.Count; y++){
                        for (int z=0; z<z_dimensions.Count; z++){
                            items.Add(new Item{
                                Product_id = id,
                                Length = z_dimensions[z],
                                Width = x_dimensions[x],
                                Height = y_dimensions[y],
                                Quantity = 1

                            }); id++;
                        }
                    }
                }

                var data = new { Items = items };
                // Serialize the object to json
                var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                // Write the json to a file
                File.WriteAllText($"{homeDir}/Unity/data/Boxes_RandomMix.json", json);           
            }
        }
    }


    // Read from json file and construct box, then add box to sizes array of boxes
    // Schema of .json: { "Product_id": string, "Length": float, "Width": float, "Height": float, "Quantity": int },
    public void ReadJson(string filename, bool randomNumberOfBoxes = false) 
    {
        idx_counter = 0;
        using (var inputStream = File.Open(filename, FileMode.Open)) {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(inputStream, new System.Xml.XmlDictionaryReaderQuotas()); 
            //var root = XElement.Load(jsonReader);
            var root = XDocument.Load(jsonReader);
            var boxes = root.XPathSelectElement("//Items").Elements();
            foreach (XElement box in boxes)
            {
                string id = box.XPathSelectElement("./Product_id").Value;
                float length = float.Parse(box.XPathSelectElement("./Length").Value);
                float width = float.Parse(box.XPathSelectElement("./Width").Value);
                float height = float.Parse(box.XPathSelectElement("./Height").Value);
                int quantity = int.Parse(box.XPathSelectElement("./Quantity").Value);
                //Debug.Log($"JSON BOX LENGTH {length} WIDTH {width} HEIGHT {height} QUANTITY {quantity}");
                // Debug.Log($"idx_counter A ================ {idx_counter}");
                for (int n = 0; n<quantity; n++)
                {
                    // Debug.Log($"n           B ================ {n}");
                    sizes[idx_counter].box_size = new Vector3(width, height, length);
                    idx_counter++;
                    // Debug.Log($"idx_counter B ================ {idx_counter}");
                }   
            }
        }
    }

    public void PadZeros()
    {
        for (int m=idx_counter; m<maxBoxQuantity; m++)
        {
            // pad with zeros
            sizes[m].box_size = Vector3.zero;
        }
    }
}


}
