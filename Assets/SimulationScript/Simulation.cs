using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine.Rendering.PostProcessing;

public class ParticleEntity
{
    public Vector3 position;
    public float mass;
    public Vector3 acceleration = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
    public float temperature = 0;
    public Vector3 size;
    public GameObject particleObject;
    public ParticleEntity(Vector3 size, Vector3 velocity, float mass, float temperature, GameObject particleObject)
    {
        this.size = size;
        position = particleObject.transform.position;
        this.mass = mass;
        this.velocity = velocity;
        this.particleObject = particleObject;
        this.temperature = temperature;
    }
    public void setPosition(Vector3 newPosition)
    {
        position = newPosition;
        particleObject.transform.position = newPosition;
    }
}

public struct Star
{
    public string Type;
    public float Mass; // in solar masses
    public float Temperature; // in Kelvin

    public Star(string type, float mass, float temperature)
    {
        Type = type;
        Mass = mass;
        Temperature = temperature;
    }
}


public class Simulation : MonoBehaviour
{
    public int particlesCount = 200;
    private List<ParticleEntity> particles = new List<ParticleEntity>();
    private const float G = 6.67430f;
    private float _deltaTime = 0;
    public TMP_Text particlesCountText;
    public TMP_Text iterationsPerSecText;
    public TMP_Text yearPassedText;
    public bool showOrbitLines = false;
    public bool showBloom = false;
    private double iterationsPerSec = 0;
    private int yearPassed = 0;
    private double nextUpdate = 1;
    private int clusterParticlesCount = 20;
    public static Vector3 massCenter = Vector3.zero;

    // Method to get the color of a star based on its temperature
    public static Color GetStarColor(float temperature)
    {
        // Define temperature ranges for different star types
        const float BROWN_DWARF_MAX_TEMP = 2500f;
        const float RED_MAX_TEMP = 3700f;
        const float ORANGE_MAX_TEMP = 5200f;
        const float YELLOW_MAX_TEMP = 6000f;
        const float YELLOW_WHITE_MAX_TEMP = 7500f;
        const float WHITE_MAX_TEMP = 10000f;
        const float BLUE_WHITE_MAX_TEMP = 30000f;
        // No need for a max for Blue Star as they are the hottest considered

        // Define color values for different star types
        Color brownDwarfColor = new Color(0.6f, 0.3f, 0f); // Dark red-brown
        Color redStar = new Color(1f, 0.5f, 0.5f); // Deep red
        Color orangeStar = new Color(1f, 0.65f, 0.4f); // Orange
        Color yellowStar = new Color(1f, 1f, 0.5f); // Yellow
        Color yellowWhiteStar = new Color(1f, 1f, 0.9f); // Yellow-White
        Color whiteStar = new Color(0.9f, 0.9f, 1f); // White
        Color blueWhiteStar = new Color(0.7f, 0.7f, 1f); // Blue-White
        Color blueStar = new Color(0.5f, 0.5f, 1f); // Deep Blue

        // Determine star color based on temperature
        if (temperature <= BROWN_DWARF_MAX_TEMP)
        {
            return brownDwarfColor; // Fixed color for Brown Dwarfs
        }
        else if (temperature <= RED_MAX_TEMP)
        {
            return Color.Lerp(brownDwarfColor, redStar, (temperature - BROWN_DWARF_MAX_TEMP) / (RED_MAX_TEMP - BROWN_DWARF_MAX_TEMP));
        }
        else if (temperature <= ORANGE_MAX_TEMP)
        {
            return Color.Lerp(redStar, orangeStar, (temperature - RED_MAX_TEMP) / (ORANGE_MAX_TEMP - RED_MAX_TEMP));
        }
        else if (temperature <= YELLOW_MAX_TEMP)
        {
            return Color.Lerp(orangeStar, yellowStar, (temperature - ORANGE_MAX_TEMP) / (YELLOW_MAX_TEMP - ORANGE_MAX_TEMP));
        }
        else if (temperature <= YELLOW_WHITE_MAX_TEMP)
        {
            return Color.Lerp(yellowStar, yellowWhiteStar, (temperature - YELLOW_MAX_TEMP) / (YELLOW_WHITE_MAX_TEMP - YELLOW_MAX_TEMP));
        }
        else if (temperature <= WHITE_MAX_TEMP)
        {
            return Color.Lerp(yellowWhiteStar, whiteStar, (temperature - YELLOW_WHITE_MAX_TEMP) / (WHITE_MAX_TEMP - YELLOW_WHITE_MAX_TEMP));
        }
        else if (temperature <= BLUE_WHITE_MAX_TEMP)
        {
            return Color.Lerp(whiteStar, blueWhiteStar, (temperature - WHITE_MAX_TEMP) / (BLUE_WHITE_MAX_TEMP - WHITE_MAX_TEMP));
        }
        else // For the hottest stars
        {
            return Color.Lerp(blueWhiteStar, blueStar, (temperature - BLUE_WHITE_MAX_TEMP) / (50000 - BLUE_WHITE_MAX_TEMP)); // Assume an arbitrary upper limit for temperature
        }
    }

    private void CreateCluster(Scene currentScene, Vector3 position)
    {
        // Create a cluster of particles
        for (int i = 0; i < clusterParticlesCount; i++)
        {
            float? x = null;
            float? y = null;
            float? z = null;
            Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
            Star star = GenerateRandomStar();
            x ??= Random.Range(position.x - 10, position.x + 10); 
            y ??= Random.Range(position.y - 10, position.y + 10);
            z ??= Random.Range(position.z - 10, position.z + 10);
            
            GameObject particleObject = CreateParticle(size, star.Temperature, x, y, z);

            SceneManager.MoveGameObjectToScene(particleObject, currentScene);
            ParticleEntity particle = new(size, Vector3.zero, star.Mass, star.Temperature, particleObject);
            particles.Add(particle);
        }
    }

    public static Star GenerateRandomStar()
    {
        // Define star category probabilities and characteristics
        // These ranges are approximations and can be adjusted for more accuracy or different interpretations
        string[] types = { "Brown Dwarf", "Red Dwarf", "Orange Dwarf", "Yellow Dwarf", "Yellow-White Dwarf", "White Star", "Blue-White Star", "Blue Star" };
        float[,] massRanges = { { 0.01f, 0.08f }, { 0.08f, 0.45f }, { 0.45f, 0.8f }, { 0.8f, 1.04f }, { 1.04f, 1.4f }, { 1.4f, 2.1f }, { 2.1f, 16f }, { 16f, 50f } };
        float[,] tempRanges = { { 500, 2500 }, { 2500, 3700 }, { 3700, 5200 }, { 5200, 6000 }, { 6000, 7500 }, { 7500, 10000 }, { 10000, 30000 }, { 30000, 50000 } };

        // Generate a random type based on the types array
        int typeIndex = Random.Range(0, types.Length);
        string selectedType = types[typeIndex];

        // Generate random mass and temperature within selected type range
        float mass = Random.Range(massRanges[typeIndex, 0], massRanges[typeIndex, 1]);
        float temperature = Random.Range(tempRanges[typeIndex, 0], tempRanges[typeIndex, 1]);

        // Return a new Star object with the generated properties
        return new Star(selectedType, mass, temperature);
    }
        
    GameObject CreateParticle(Vector3 size, float temperature, float? x = null, float? y = null, float? z = null)
    {
        GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        TrailRenderer trailRenderer = particle.AddComponent<TrailRenderer>();
        trailRenderer.startWidth = 0.05f;
        trailRenderer.endWidth = 0.01f;
        trailRenderer.time = 4f;
        trailRenderer.material = Resources.Load<Material>("OrbitLine");
        particle.transform.localScale = size;

        if (x != null && y != null && z != null)
        {
            particle.transform.position = new Vector3((float)x, (float)y, (float)z);
        }
        else
        {
            particle.transform.position = Random.insideUnitSphere * 20;
        }

        Color particleColor = GetStarColor(temperature);
        // Set the color
        Renderer particleRenderer = particle.GetComponent<Renderer>();
        if (particleRenderer != null)
        {
            particleRenderer.material.color = particleColor; // Apply color based on mass
            particleRenderer.material.EnableKeyword("_EMISSION"); // Enable emission
            particleRenderer.material.SetColor("_EmissionColor", particleColor); // Set emission color
        }

        return particle;
    }
 
    bool CheckCollision(ParticleEntity body1, ParticleEntity body2)
    {
        float radiusSum = body1.size.magnitude + body2.size.magnitude;
        float distance = Vector3.Distance(body1.position, body2.position);
        return distance < radiusSum / 2;
    }

    private void AddParticle(Scene currentScene, Vector3? position = null)
    {
        Star star = GenerateRandomStar();
        Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
        GameObject particleObject = CreateParticle(size, star.Temperature, position?.x, position?.y, position?.z);

        SceneManager.MoveGameObjectToScene(particleObject, currentScene);
        ParticleEntity particle = new(size, Vector3.zero, star.Mass, star.Temperature, particleObject);
        particles.Add(particle);
    }

    // Start is called before the first frame update
    void Start()
    {
        particlesCountText.text = "Objects: " + particles.Count.ToString();
        iterationsPerSecText.text = "0it/s";
        yearPassedText.text = yearPassed.ToString() + " Y";
        Vector3 centerOfCamera = Camera.main.transform.position;
        Vector3 targetPosition = new Vector3(centerOfCamera.x, centerOfCamera.y, centerOfCamera.z + 5);
        AddParticle(SceneManager.GetActiveScene(), targetPosition);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            #if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector3 mousePosition = Input.mousePosition;   
            mousePosition.z = Camera.main.nearClipPlane + 1;

            Vector3 newParticlePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            AddParticle(SceneManager.GetActiveScene(), newParticlePosition);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"{hit.collider.name} Detected",
                    hit.collider.gameObject);
            }
        }

        _deltaTime = Time.deltaTime / 10;
        SimulateGravity();

        particlesCountText.text = "Objects: " + particles.Count.ToString();
        if(Input.GetKeyDown(KeyCode.Return))
        {
            Scene currentScene = SceneManager.GetActiveScene();
            for (int i = 0; i < particlesCount; i++)
            {
                AddParticle(currentScene);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            showOrbitLines = !showOrbitLines;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            showBloom = !showBloom;
            if (showBloom)
            {
                foreach (ParticleEntity particle in particles)
                {
                    particle.particleObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                foreach (ParticleEntity particle in particles)
                {
                    particle.particleObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                }
            }
        }

        yearPassedText.text = yearPassed.ToString() + " Y";
    }

    // Simulate gravity
    private void SimulateGravity()
    {
        Parallel.For(0, particles.Count - 1, i =>
        {
            if (i >= particles.Count) return; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
            ParticleEntity currentEntity = particles[i];

            for (int j = i + 1; j < particles.Count - 1; j++) // Inizia da j = i + 1 per evitare calcoli duplicati e autointerazioni
            { 
                if (j >= particles.Count) break; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                ParticleEntity nextEntity = particles[j];

                Vector3 distanceVector = nextEntity.position - currentEntity.position;
                float distance = distanceVector.magnitude;
                Vector3 forceDirection = distanceVector.normalized;
                
                if (distance > 100) { // Evita calcoli inutili (particelle troppo lontane tra loro)
                    continue;
                } 
               
                if (CheckCollision(currentEntity, nextEntity) == false)
                { // Evita la divisione per zero e l'interazione della particella con se stessa
                    float forceMagnitude = G * (currentEntity.mass * nextEntity.mass) / (distance * distance);
                    Vector3 force = forceDirection * forceMagnitude;

                    // Update the acceleration of the current entity
                    currentEntity.acceleration += force / currentEntity.mass;

                    // Update the acceleration of the next entity (in the opposite direction)
                    nextEntity.acceleration -= force / nextEntity.mass;
                }else{
                    // Collisione e fusione di particelle
                    float newMass = currentEntity.mass + nextEntity.mass;
                    Vector3 newPosition = (currentEntity.position * currentEntity.mass + nextEntity.position * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
                    Vector3 newVelocity = (currentEntity.velocity * currentEntity.mass + nextEntity.velocity * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
                    // Vector3 newSize = currentEntity.size + nextEntity.size;
                    Vector3 newSize = new Vector3(0.1f, 0.1f, 0.1f);
                    float newTemperature = (currentEntity.temperature + nextEntity.temperature) / 2;
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        GameObject newParticleObject = CreateParticle(newSize, newTemperature, newPosition.x, newPosition.y, newPosition.z);
                        ParticleEntity mergedParticle = new(newSize, newVelocity, newMass, newTemperature, newParticleObject);
                        particles.Add(mergedParticle);
                    });
                    particles.Remove(currentEntity);
                    particles.Remove(nextEntity);
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        Destroy(currentEntity.particleObject);
                        Destroy(nextEntity.particleObject);
                    });
                }
            }
        });

        foreach (ParticleEntity particle in particles)
        {
            particle.velocity += particle.acceleration * _deltaTime;

            particle.setPosition(particle.position + particle.velocity * _deltaTime);

            particle.acceleration = Vector3.zero;

            if (showOrbitLines)
            {
                TrailRenderer trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
                trailRenderer.emitting = true;
            }
            else
            {
                TrailRenderer trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
                trailRenderer.emitting = false;
            }
        }

        yearPassed++;
        
        if (Time.time > nextUpdate)
        {
            nextUpdate = Time.time + 1;
            iterationsPerSec = 1 / _deltaTime;
            iterationsPerSecText.text = iterationsPerSec.ToString("F0") + "it/s";
        }
    }

    void OnApplicationQuit()
    {
        foreach (ParticleEntity particle in particles)
        {
            Destroy(particle.particleObject);
        }

        particles.Clear();
    }
}