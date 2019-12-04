using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Threading;

/// <summary>
/// Manages receiving and storing spatial radiation data in an efficient way.
/// </summary>
public class RadiationPointSubscriber : PointCloudVisualizer2, DataServer.DataSubscriber
{
    /// <value> Attach DataServer object. If nonexistant, create an empty GameObject and attach the script `DataServer.cs`.</value>
    //public DataServer server;
    public Material mat;
    /// <value> Setting this to true will give a horizontal view of the data.</value>
    public bool flipYZ = false;
    public int total_particles;
    /// <value> The default size of each radiation voxel, if it is not specified in the data.</value>
    private float size = 1;
    /// <value> The maximum radiation value. Used for deciding how to map the range of radiation values to colors.</value>
    //private float max = 0.0007f;
    private float max = 34f;
    private float min = 1f;
    public GameObject parentObject;
    /// <value> The octree for holding/organizing the radiation voxels efficiently.</value>
    private BoundsOctree<float> boundsTree = null;

    private bool finished = false;
    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        SetShader("Particles/Standard Unlit");

        SetColor(new Color(1, 1, 1, 0.2f));
        SetEmissionColor(new Color(0, 0, 0, 0));


        //StartCoroutine(PointCloudGenerator());

    }
    IEnumerator PointCloudGenerator()
    {
       // for (int j = 0; j <= 27; j++)
       //{
            string path = "radiation/radiation_data27";

            List<Dictionary<string, string>> data = CSVReader.Read(path);

            total_particles = data.Count;

            float x, y, z, intensity;
        //Makes points from file

        for (int i = 0; i < total_particles; i++)
        {
            x = float.Parse(data[i]["x"]);
            y = float.Parse(data[i]["y"]);
            z = float.Parse(data[i]["z"]);
            intensity = float.Parse(data[i]["intensity"]);

            
            
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(x * 0.05f, z * 0.05f, y * 0.05f);
                cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                var cubeRenderer = cube.GetComponent<Renderer>();
            // sphereRenderer.material.SetFloat("_Mode", 3);
            //sphereRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            //sphereRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            //sphereRenderer.material.SetInt("_ZWrite", 0);
            //sphereRenderer.material.DisableKeyword("_ALPHATEST_ON");
            //sphereRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            //sphereRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            //sphereRenderer.material.renderQueue = 3000;
           // cubeRenderer.material.SetColor("_Color", Color.red);
            cubeRenderer.material.SetColor("_Color", colorFromIntensity(intensity));
            cube.transform.parent = parentObject.transform;
            cube.GetComponent<BoxCollider>().enabled = false;
            
  
        }
                yield return new WaitForSeconds(2.0f);
       
    }
    
    /// <summary>
    /// The callback for receiving data on the subscribed channel.
    /// Parses and checks if message is corrupted.
    /// Stores data ready for visualization.
    /// </summary>
    /// <param name="timestamp">The timestamp of the received message.</param>
    /// <param name="message">The raw contents of the message.</param>
    public void OnReceiveMessage(float timestamp, string message)
    {
        if (string.Compare(message.ToString(), "End of Radiation") == 0)
        {
            finished = true;
        }
        string[] parts = message.Split(',');
        float x, y, z, intensity, size;
        if (parts.Length >= 4 && float.TryParse(parts[0], out x) &&
            float.TryParse(parts[1], out y) && float.TryParse(parts[2], out z) &&
            float.TryParse(parts[3], out intensity))
        {
            if (boundsTree == null)
            {
                boundsTree = new BoundsOctree<float>(this.size * 100, new Vector3(0, 0, 0), this.size, 1f);
            }
            ParticleSystem.Particle p = new ParticleSystem.Particle();
            p.position = (flipYZ) ? new Vector3(x, z, y) : new Vector3(x, y, z);
            p.startSize = this.size;

            p.startColor = colorFromIntensity(intensity);

            AddParticle(p);

            // Adding the cube to the octree
            Vector3 bounds = new Vector3(this.size, this.size, this.size);
            boundsTree.Add(intensity, new Bounds(p.position, bounds));


        } else if (parts.Length == 1 && float.TryParse(parts[0], out size))
        {
            this.size = size;
        }
    }

    /// <summary>
    /// Search for the radiation voxels that a point belongs to.
    /// </summary>
    private List<float> radiationVals = new List<float>();
    public Color GetRadiationColor(Vector3 point)
    {
        radiationVals.Clear();
        boundsTree.GetColliding(radiationVals, new Bounds(point, new Vector3(this.size * 5, this.size * 5, this.size * 5)));
        if (radiationVals.Count == 0)
        {
            return new Color(1, 1, 1, 1);
        }
        Color c = colorFromIntensity(radiationVals[0]);
        for (int i = 1; i < radiationVals.Count; i++)
        {
            c += colorFromIntensity(radiationVals[i]);
        }
        return c / radiationVals.Count;
    }

    /// <summary>
    /// A utility method for assigning a color to a radiation intensity.
    /// </summary>
    /// <param name="intensity">The intensity of the radiation of the point to be colored.</param>
    /// <returns>The color that should be assigned to the point.</returns>
    /// <remarks> Currently the coloring scale is pretty arbitrary.</remarks>
    private Color colorFromIntensity(float intensity)
    {
        float intensity_norm = Mathf.Min(Mathf.Abs(intensity), max) / max;
        // We use 0.85 to prevent looping from red to red (since both 0 and 1 on the hue scale look like red).
        Color temp = Color.HSVToRGB(0, 1, 1*intensity_norm);
        // Use cubic scaling for alpha channel. We want points with low intensity to be deemphasized.
        //return new Color(temp.r, temp.g, temp.b, intensity_norm* intensity_norm* intensity_norm);
        return new Color(temp.r, temp.g, temp.b, intensity_norm);
    }
}
