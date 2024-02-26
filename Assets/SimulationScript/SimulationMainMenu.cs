using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Unity.VisualScripting;
using Random = UnityEngine.Random;


public class SimulationMainMenu : MonoBehaviour
{
    public GameObject particlePrefab;
    private List<ParticleEntity> particles = new List<ParticleEntity>();
    private readonly float G = 6.67430f;
    private float _deltaTime = 0;
    private double nextUpdate = 1;
    private string[] STAR_TYPE = { "Brown Dwarf", "Red Dwarf", "Orange Dwarf", "Yellow Dwarf", "Yellow-White Dwarf", "White Star", "Blue-White Star", "Blue Star" };
    private Vector3 _particleSize = new Vector3(0.1f, 0.1f, 0.1f);
    public OctreeNode octree { get; private set; }
    public bool isMainMenu = false;

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

    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        var cameraPosition = Camera.main.transform.position;
        cameraPosition = new Vector3(cameraPosition.x, cameraPosition.y, cameraPosition.z + 5);
        CreateCluster(currentScene, cameraPosition, 5);
    }

    void Update()
    {
        _deltaTime = Time.deltaTime / 70;
        SimulateGravity();
    }

    private void CreateCluster(Scene currentScene, Vector3 position, int count = 20)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(position.x - 2, position.x + 2);
            float y = Random.Range(position.y - 2, position.y + 2);
            float z = position.z;
            Vector3 newPosition = new Vector3(x, y, z);
            particles.Add(AddParticle(currentScene, newPosition, Vector3.zero));
        }
    }

    public Star GetStarTypeByTemperature(float temperature, float mass, bool isBlackHole = false)
    {
        float[,] massRanges = { { 0.01f, 0.02f }, { 0.02f, 0.45f }, { 0.45f, 0.8f }, { 0.8f, 1.04f }, { 1.04f, 1.4f }, { 1.4f, 2.1f }, { 2.1f, 16f }, { 16f, 50f } };
        float[,] tempRanges = { { 500, 2500 }, { 2500, 3700 }, { 3700, 5200 }, { 5200, 6000 }, { 6000, 7500 }, { 7500, 10000 }, { 10000, 30000 }, { 30000, 50000 } };

        for (int i = 0; i < STAR_TYPE.Length; i++)
        {
            if (temperature >= tempRanges[i, 0] && temperature <= tempRanges[i, 1] && mass >= massRanges[i, 0] && mass <= massRanges[i, 1])
            {
                return new Star(STAR_TYPE[i], mass, temperature, isBlackHole ? Color.black : GetStarColor(temperature));
            }
        }
        return new Star("Unknown", mass, temperature, isBlackHole ? Color.black : GetStarColor(temperature));
    }

    public Star GenerateRandomStar(bool isBlackHole = false)
    {
        float[,] massRanges = { { 0.01f, 0.02f }, { 0.02f, 0.45f }, { 0.45f, 0.8f }, { 0.8f, 1.04f }, { 1.04f, 1.4f }, { 1.4f, 2.1f }, { 2.1f, 16f }, { 16f, 50f } };
        float[,] tempRanges = { { 500, 2500 }, { 2500, 3700 }, { 3700, 5200 }, { 5200, 6000 }, { 6000, 7500 }, { 7500, 10000 }, { 10000, 30000 }, { 30000, 50000 } };
        // Generate a random type based on the types array
        int typeIndex = Random.Range(0, STAR_TYPE.Length);
        string selectedType = STAR_TYPE[typeIndex];

        // Generate random mass and temperature within selected type range
        float mass = Random.Range(massRanges[typeIndex, 0], massRanges[typeIndex, 1]);
        float temperature = Random.Range(tempRanges[typeIndex, 0], tempRanges[typeIndex, 1]);

        // Return a new Star object with the generated properties
        return new Star(selectedType, mass, temperature, isBlackHole ? Color.black : GetStarColor(temperature));
    }

    private GameObject CreateParticle(Vector3 size, Color color, float? x = null, float? y = null, float? z = null, bool isBlackHole = false, bool isStellar = true)
    {
        GameObject particle;
        if (isBlackHole)
        {
            particle = Instantiate(Resources.Load<GameObject>("BlackHolePrefab"));
        }
        else
        {
            particle = Instantiate(particlePrefab);
        }

        TrailRenderer trailRenderer = particle.AddComponent<TrailRenderer>();
        trailRenderer.startWidth = 0.01f;
        trailRenderer.endWidth = 0.001f;
        trailRenderer.time = 4f;
        trailRenderer.startColor = color;
        trailRenderer.endColor = new Color(color.r, color.g, color.b, 0);
        trailRenderer.material = Resources.Load<Material>("OrbitLine");
        trailRenderer.emitting = true;
        particle.transform.localScale = size;

        if (x != null && y != null && z != null)
        {
            particle.transform.position = new Vector3((float)x, (float)y, (float)z);
        }
        else
        {
            particle.transform.position = Random.insideUnitSphere * 20;
        }

        Color particleColor = !isBlackHole ? color : Color.black;
        // Set the color
        Renderer particleRenderer = particle.GetComponent<Renderer>();
        particleRenderer.material.color = particleColor; // Apply color based on mass
        particleRenderer.material.EnableKeyword("_EMISSION"); // Enable emission
        particleRenderer.material.SetColor("_EmissionColor", particleColor); // Set emission color

        return particle;
    }

    private bool CheckCollision(ParticleEntity body1, ParticleEntity body2)
    {
        float radiusSum = body1.size.magnitude + body2.size.magnitude;
        float distance = Vector3.Distance(body1.position, body2.position);
        return distance < radiusSum / 2;
    }

    private ParticleEntity AddParticle(Scene currentScene, Vector3? position = null, Vector3? velocity = null, float mass = 1)
    {
        Star star = GenerateRandomStar();
        Color color = GetStarColor(star.Temperature);
        GameObject particleObject = CreateParticle(_particleSize, color, position?.x, position?.y, position?.z);

        SceneManager.MoveGameObjectToScene(particleObject, currentScene);
        return new(_particleSize, velocity ?? Vector3.zero, star.Mass, star.Temperature, star.Type, particleObject);
    }

    // Simulate gravity O(n^2) complexity
    private void SimulateGravity()
    {
        Parallel.For(0, particles.Count, i =>
        {
            try
            {
                if (i >= particles.Count) return; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                ParticleEntity currentEntity = particles[i];
                int startFrom = particles.Count < 3 ? 0 : i + 1;
                for (int j = startFrom; j < particles.Count; j++) // Inizia da j = i + 1 per evitare calcoli duplicati e autointerazioni
                {
                    if (j == i) continue; // Evita calcoli duplicati e autointerazioni
                    if (j >= particles.Count) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                    if (i >= particles.Count) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                    ParticleEntity nextEntity = particles[j];
                    if (nextEntity == null) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                    if (currentEntity == null) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                    Vector3 distanceVector = nextEntity.position - currentEntity.position;
                    float distance = distanceVector.magnitude;
                    Vector3 forceDirection = distanceVector.normalized;

                    if (!CheckCollision(currentEntity, nextEntity))
                    {
                        float forceMagnitude = G * (currentEntity.mass * nextEntity.mass) / (distance * distance);
                        Vector3 force = forceDirection * forceMagnitude;

                        currentEntity.acceleration += force / currentEntity.mass;
                        nextEntity.acceleration -= force / nextEntity.mass;
                    }
                    else if (distance == 0)
                    {
                        // MergeParticle(currentEntity, nextEntity);
                        continue;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        });

        foreach (var particle in particles)
        {
            if (particle == null) return;
            particle.velocity += particle.acceleration * _deltaTime;
            particle.SetPosition(particle.position + particle.velocity * _deltaTime);
            particle.acceleration = Vector3.zero;
            particle.particleObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            particle.particleObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", GetStarColor(particle.temperature));
        }

        if (Time.time > nextUpdate)
        {
            nextUpdate = Time.time + 1;
        }
    }
}
