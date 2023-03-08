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

    public int product_id;

    public Vector3 startingPos; // for box reset, constant 

    public Quaternion startingRot; // for box reset, constant

    public Vector3 startingSize; // for box reset, constant 

    public Vector3 boxSize; // for sensor, changes after selected action

    public Quaternion boxRot; // for sensor, changes after selected action

    public Vector3 boxVertex; // for sensor, changes after selected action

    public Color boxColor;

    public bool isOrganized = false; 

    public GameObject gameobjectBox;
}

public class Container
{
    public float Length {get; set;}
    public float Width {get; set;}
    public float Height {get; set;}
}

[System.Serializable]
public class BoxSize
{
    public Vector3 box_size;
}

public class Item
{
    public int Product_id { get; set; }
    public float Length { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public int Quantity { get; set; }
}


// Spawns in boxes with sizes from a json file
public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public List<Box> boxPool = new List<Box>();
    private List<Item> Items = new List<Item>();

    public List<Container> Containers = new List<Container>();

    public Container Container = new Container();

    public List<Color> Colors = new List<Color>();

    // The box area, which will be set manually in the Inspector
    public GameObject boxArea;
    
    public GameObject unitBox; 

    public int maxBoxQuantity;

    public BoxSize [] sizes;

    [HideInInspector] public int idx_counter = 0;

    string homeDir;


    public void Start()
    {
        homeDir = Environment.GetEnvironmentVariable("HOME");
    }

    public void SetUpBoxes(string box_type, int seed=123) 
    {
        if (box_type == "uniform_random")
        {
            RandomBoxGenerator("uniform_random", seed);
            // Read random boxes using existing ReadJson function
            ReadJson($"{homeDir}/Unity/data/Boxes_RandomUniform.json", seed);
            PadZeros();
            // Delete the created json file to reuse the name next iteration
            File.Delete($"{homeDir}/Unity/data/Boxes_RandomUniform.json");

        }
        else if (box_type == "mix_random")
        {
            RandomBoxGenerator("mix_random", seed);
            // Read random boxes using existing ReadJson function
            ReadJson($"{homeDir}/Unity/data/Boxes_RandomMix.json", seed);
            PadZeros();
            // Delete the created json file to reuse the name next iteration
            File.Delete($"{homeDir}/Unity/data/Boxes_RandomMix.json");
        }
        else
        {
            if (Items.Count==0)
            {
                ReadJson($"{homeDir}/Unity/data/{box_type}.json", seed);
                PadZeros();
            }
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
                    product_id = Items[idx].Product_id,
                    boxColor = Colors[idx],
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

    public void RandomBoxGenerator(string box_type, int seed)
    {
        if (box_type == "uniform_random") 
        {
            UnityEngine.Random.InitState(seed);
            Color randomColor = UnityEngine.Random.ColorHSV();
            int random_num_x =  UnityEngine.Random.Range(1, 4);
            int random_num_y =  UnityEngine.Random.Range(1, 4);
            int random_num_z =  UnityEngine.Random.Range(1, 6);
            float x_dimension =  (float)Math.Floor(Container.Width/random_num_x * 100)/100;
            float y_dimension =  (float)Math.Floor(Container.Height/random_num_y * 100)/100;
            float z_dimension = (float)Math.Floor(Container.Length/random_num_z * 100)/100;
            int quantity = random_num_x*random_num_y*random_num_z;
            //Debug.Log($"RUF RANDOM UNIFORM BOX NUM: {random_num_x*random_num_y*random_num_z} | x:{x_dimension} y:{y_dimension} z:{z_dimension}");
            List<Item> items = new List<Item>();
            items.Add(new Item
            {
                Product_id = 0,
                Length = z_dimension,
                Width = x_dimension,
                Height = y_dimension,
                Quantity = random_num_x*random_num_y*random_num_z,
            });
            // Set color of the boxes
            Colors.Clear();
            for (int i= 0; i<quantity; i++)
            {
                Colors.Add(randomColor);
            }
            // Create a new object with the Items list
            var data = new { Items = items };
            // Serialize the object to json
            var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            // Write the json to a file
            File.WriteAllText($"{homeDir}/Unity/data/Boxes_RandomUniform.json", json);
        }
        if (box_type == "mix_random")
        {
            int bin_z = (int) Math.Floor(Container.Length);
            int bin_x = (int) Math.Floor(Container.Width);
            int bin_y = (int) Math.Floor(Container.Height);
            UnityEngine.Random.InitState(seed);
            List<Item> items = new List<Item>();
            Colors.Clear();
            List<int> x_dimensions = new List<int>();
            List<int> y_dimensions = new List<int>();
            List<int> z_dimensions = new List<int>();
            // chop up the x, y, z dimensions
            int random_num_x =  UnityEngine.Random.Range(2, 4);
            int random_num_y =  UnityEngine.Random.Range(2, 4);
            int random_num_z =  UnityEngine.Random.Range(2, 6);
            x_dimensions.Add(bin_x);
            while (x_dimensions.Count<random_num_x)
            {
                int largest = x_dimensions.Max();
                int newPiece = UnityEngine.Random.Range(1, largest);
                x_dimensions.Remove(largest);
                x_dimensions.Add(newPiece);
                x_dimensions.Add(largest - newPiece);
            }
            y_dimensions.Add(bin_y);
            while (y_dimensions.Count<random_num_y)
            {
                int largest = y_dimensions.Max();
                int newPiece = UnityEngine.Random.Range(1, largest);
                y_dimensions.Remove(largest);
                y_dimensions.Add(newPiece);
                y_dimensions.Add(largest - newPiece);
            }
            z_dimensions.Add(bin_z);
            while (z_dimensions.Count<random_num_z)
            {
                int largest = z_dimensions.Max();
                int newPiece = UnityEngine.Random.Range(1, largest);
                z_dimensions.Remove(largest);
                z_dimensions.Add(newPiece);
                z_dimensions.Add(largest - newPiece);
            }
            int id = 0;
            for (int x=0; x<x_dimensions.Count; x++){
                for (int y=0; y<y_dimensions.Count; y++){
                    for (int z=0; z<z_dimensions.Count; z++){
                        Color randomColor = UnityEngine.Random.ColorHSV();
                        Colors.Add(randomColor);
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

    public void SetUpBin()
    {
        // Randomly generate a bin
        Container.Length =  (float) Math.Round(UnityEngine.Random.Range(10.0f, 60.0f), 2);
        Container.Height = (float) Math.Round(UnityEngine.Random.Range(10.0f, 25.0f), 2);
        Container.Width = (float) Math.Round(UnityEngine.Random.Range(10.0f, 30.0f));
    }


    // Read from json file and construct box, then add box to sizes array of boxes
    // Schema of .json: { "Product_id": string, "Length": float, "Width": float, "Height": float, "Quantity": int },
    public void ReadJson(string filename, int seed) 
    {
        UnityEngine.Random.InitState(seed);
        idx_counter = 0;
        using (var inputStream = File.Open(filename, FileMode.Open)) {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(inputStream, new System.Xml.XmlDictionaryReaderQuotas()); 
            //var root = XElement.Load(jsonReader);
            var root = XDocument.Load(jsonReader);
            var boxes = root.XPathSelectElement("//Items").Elements();
            foreach (XElement box in boxes)
            {
                int id = int.Parse(box.XPathSelectElement("./Product_id").Value);
                float length = float.Parse(box.XPathSelectElement("./Length").Value);
                float width = float.Parse(box.XPathSelectElement("./Width").Value);
                float height = float.Parse(box.XPathSelectElement("./Height").Value);
                int quantity = int.Parse(box.XPathSelectElement("./Quantity").Value);
                Color randomColor = UnityEngine.Random.ColorHSV();
                //Debug.Log($"JSON BOX LENGTH {length} WIDTH {width} HEIGHT {height} QUANTITY {quantity}");
                for (int n = 0; n<quantity; n++)
                {
                    sizes[idx_counter].box_size = new Vector3(width, height, length);
                    Items.Add(new Item
                    {
                        Product_id = id,
                        Length = width,
                        Width = height,
                        Height = length,
                        Quantity = quantity,
                    });
                    // Set color of boxes (same id (same size) with same color)
                    Colors.Add(randomColor);
                    idx_counter++;
                }   
            }
        }
    }

    public void ReadJsonForBin(string box_file) 
    {
        homeDir = Environment.GetEnvironmentVariable("HOME");
        string filename = $"{homeDir}/Unity/data/{box_file}.json";
        using (var inputStream = File.Open(filename, FileMode.Open)) {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(inputStream, new System.Xml.XmlDictionaryReaderQuotas()); 
            //var root = XElement.Load(jsonReader);
            var root = XDocument.Load(jsonReader);
            var containers = root.XPathSelectElement("//Container").Elements();
            foreach (XElement container in containers)
            {
                float length = float.Parse(container.XPathSelectElement("./Length").Value)/10f;
                float width = float.Parse(container.XPathSelectElement("./Width").Value)/10f;
                float height = float.Parse(container.XPathSelectElement("./Height").Value)/10f;   
                //Debug.Log($"JSON CONTAINER LENGTH {Container.Length} WIDTH {Container.Width} HEIGHT {Container.Height}");
                Containers.Add(new Container
                    {
                        Length = width,
                        Width = height,
                        Height = length,
                    });
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
