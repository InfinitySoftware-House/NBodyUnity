using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using PimDeWitte.UnityMainThreadDispatcher;
using Unity.VisualScripting;
using System.Linq;
using UnityEditor;
using Random = UnityEngine.Random;
using System.Collections;
using System;


public class Simulation : MonoBehaviour
{
    public GameObject particlePrefab;
    private List<ParticleEntity> particles = new List<ParticleEntity>();
    private readonly float G = 6.67430f; // Gravitational constant
    private float _deltaTime = 0;
    public TMP_Text particlesCountText;
    public TMP_Text iterationsPerSecText;
    public TMP_Text yearPassedText;
    // private TMP_Text newStarVelocityText;
    public bool showOrbitLines = false;
    public bool showBloom = true;
    private double iterationsPerSec = 0;
    private int yearPassed = 0;
    private double nextUpdate = 1;
    public GameObject objectInfoObject;
    private int _starVelocity = 10;
    private ParticleEntity lockedParticle;
    private string[] STAR_TYPE = { "Brown Dwarf", "Red Dwarf", "Orange Dwarf", "Yellow Dwarf", "Yellow-White Dwarf", "White Star", "Blue-White Star", "Blue Star" };
    // private float BLACK_HOLE_MASS = 10000;
    private bool showKineticEnergy = false;
    private bool showVelocityColor = false;
    public GameObject legendPanel;
    private Vector3 _particleSize = new Vector3(0.1f, 0.1f, 0.1f);
    public TMP_Text showBloomText;
    public TMP_Text showOrbitLinesText;
    public TMP_Text showKineticEnergyText;
    public TMP_Text showVelocityColorText;
    public OctreeNode octree { get; private set; }
    private bool runSimulation = true;
    private bool isGalaxy = false;
    private bool showMass = false;
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

    private void CreateCluster(Scene currentScene, Vector3 position, int count = 20)
    {
        if (isGalaxy){
            Vector3 massCenter = position;
            particles.Add(AddParticle(currentScene, massCenter, Vector3.zero, mass: 1000));
        }
        // Create a cluster of particles
        for (int i = 0; i < count; i++)
        {
            Vector3 newPosition;
            if(isGalaxy){
                float innerRadius = 12f; // Inner radius of the ring
                float outerRadius = 14f; // Outer radius of the ring
                float angle = i * 2.0f * Mathf.PI / count; // Distribute particles evenly around the circle

                // Randomize radius within the ring bounds
                float radius = Random.Range(innerRadius, outerRadius);
                // Calculate x, y, z coordinates for a ring-shaped galaxy
                float x = position.x + radius * Mathf.Cos(angle);
                float y = position.y + radius * Mathf.Sin(angle);
                float z = position.z; // Assuming you want the ring to be horizontal, keep z constant
                newPosition = new Vector3(x, y, z);
            }else{
                // Randomize position within a rectangular area
                float multiplier = count / 100f;
                float minX = position.x - multiplier; // Minimum x coordinate of the rectangular area
                float maxX = position.x + multiplier; // Maximum x coordinate of the rectangular area
                float minY = position.y - multiplier; // Minimum y coordinate of the rectangular area
                float maxY = position.y + multiplier; // Maximum y coordinate of the rectangular area
                float minZ = position.z - multiplier; // Minimum z coordinate of the rectangular area
                float maxZ = position.z + multiplier; // Maximum z coordinate of the rectangular area

                float x = Random.Range(minX, maxX);
                float y = Random.Range(minY, maxY);
                float z = Random.Range(minZ, maxZ);
                newPosition = new Vector3(x, y, z);
            }

            Vector3 velocity = Vector3.zero;
            if (isGalaxy){
                velocity.x = 6f;
            }
            particles.Add(AddParticle(currentScene, newPosition, velocity));
        }
    }
    private void CreateOctree()
    {
        Vector3 massCenterForSim = GetMassCenter();
        int universeSize = 1000;
        octree = new OctreeNode(massCenterForSim, universeSize, G);

        // Add all particles to the octree
        foreach (var particle in particles)
        {
            octree.AddParticle(particle);
        }
    }

    private Vector3 GetMassCenter()
    {
        Vector3 massCenter = Vector3.zero;
        foreach (ParticleEntity particle in particles)
        {
            massCenter += particle.position;
        }
        return massCenter / particles.Count;
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
        if (!isBlackHole && isStellar)
        {
            particleRenderer.material.color = particleColor; // Apply color based on mass
            particleRenderer.material.EnableKeyword("_EMISSION"); // Enable emission
            particleRenderer.material.SetColor("_EmissionColor", particleColor); // Set emission color
        }
        else if (!isStellar)
        {
            particleRenderer.material.DisableKeyword("_EMISSION");
            particleRenderer.material.color = particleColor;
        }

        if (isBlackHole)
        {
            particleRenderer.material.color = particleColor;
            particleRenderer.material.EnableKeyword("_EMISSION");
            particleRenderer.material.SetColor("_EmissionColor", particleColor);
        }

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

    // Start is called before the first frame update
    private void Start()
    {
        objectInfoObject.SetActive(false);
        particlesCountText.text = "Objects: " + particles.Count.ToString();
        iterationsPerSecText.text = "0it/s";
        yearPassedText.text = yearPassed.ToString() + " Y";
        // newStarVelocityText.text = "v " + _starVelocity.ToString();
        showBloomText.color = showBloom ? Color.green : Color.white;
        showOrbitLinesText.color = showOrbitLines ? Color.green : Color.white;
        showKineticEnergyText.color = showKineticEnergy ? Color.green : Color.white;
        showVelocityColorText.color = showVelocityColor ? Color.green : Color.white;
    }

    private ObjectInfoModel GetObjectInfoModel(ParticleEntity particle)
    {
        ObjectInfoModel objectInfoModel = new ObjectInfoModel
        {
            Name = particle.name,
            Type = particle.type,
            Temperature = particle.temperature,
            Mass = particle.mass,
            Position = particle.position
        };
        return objectInfoModel;
    }

    // Update is called once per frame
    private void Update()
    {
        yearPassedText.text = yearPassed.ToString() + " Y";

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Show the main menu
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        if(Input.GetKeyDown(KeyCode.Space)){
            runSimulation = !runSimulation;
            iterationsPerSecText.color = runSimulation ? Color.white : Color.red;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                ParticleEntity selectedParticle = particles.FirstOrDefault(p => p.particleObject == hit.collider.gameObject);
                if (selectedParticle != null)
                {
                    ParticleEntity particle = selectedParticle;
                    ObjectInfoModel objectInfoModel = GetObjectInfoModel(particle);
                    lockedParticle = particle;
                    objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
                    objectInfoObject.SetActive(true);
                }
                else
                {
                    lockedParticle = null;
                    objectInfoObject.SetActive(false);
                }
            }
            else
            {
                lockedParticle = null;
                objectInfoObject.SetActive(false);
            }
        }

        if (lockedParticle != null)
        {
            // Update locked particle position
            ObjectInfoModel objectInfoModel = GetObjectInfoModel(lockedParticle);
            objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
            // Call the coroutine to animate the LookAt function
            Camera.main.transform.LookAt(lockedParticle.particleObject.transform);
        }

        _deltaTime = Time.deltaTime / 100;

        if (runSimulation)
        {
            CreateOctree();
            SimulateBarnesHut();
            yearPassed += 1;
        }
        else
        {
            iterationsPerSecText.text = "Simulation paused";
        }

        particlesCountText.text = "Objects: " + particles.Count.ToString();
        // 100 particles
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Scene currentScene = SceneManager.GetActiveScene();
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            CreateCluster(currentScene, currentPosition, 100);
        }
        // 20 particles
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition);
        }
        // 200 particles
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition, 200);
        }
        // 400 particles
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition, 400);
        }

        // 1000 particles
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition, 1000);
        }

        // 10000 particles
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition, 10000);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            showOrbitLines = !showOrbitLines;
            showOrbitLinesText.color = showOrbitLines ? Color.green : Color.white;
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            if (_starVelocity < 20)
                _starVelocity += 1;
            // newStarVelocityText.text = "v " + _starVelocity.ToString();
        }
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            if (_starVelocity > 1)
                _starVelocity -= 1;
            // newStarVelocityText.text = "v " + _starVelocity.ToString();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // reset the scene
            // destroy all particles
            foreach (ParticleEntity particle in particles)
            {
                if (particle != null && particle.particleObject != null && !particle.particleObject.IsDestroyed())
                    Destroy(particle.particleObject);
            }
            // garbage collect
            particles = null;
            // create a new list of particles
            particles = new List<ParticleEntity>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            showBloom = !showBloom;
            foreach (ParticleEntity particle in particles)
            {
                if (!showBloom)
                {
                    particle.particleObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
                    particle.particleObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
                }
                else
                {
                    particle.particleObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                    particle.particleObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", GetStarColor(particle.temperature));
                }
            }
            showBloomText.color = showBloom ? Color.green : Color.white;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            showKineticEnergy = !showKineticEnergy;
            showKineticEnergyText.color = showKineticEnergy ? Color.green : Color.white;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            legendPanel.SetActive(!legendPanel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            showVelocityColor = !showVelocityColor;
            showVelocityColorText.color = showVelocityColor ? Color.green : Color.white;
        }

        if(Input.GetKeyDown(KeyCode.M))
        {
            showMass = !showMass;
        }

        if(Input.GetKeyDown(KeyCode.G)){
            isGalaxy = !isGalaxy;
            // Show a message to the user
            Debug.Log("Galaxy mode: " + (isGalaxy ? "ON" : "OFF"));
        }

        // Garbage collect every 1000 iterations
        if (yearPassed % 1000 == 0 && particles.Count > 1000)
        {
            GC.Collect();
        }
    }

    private float CalculateKineticEnergy(ParticleEntity particle)
    {
        return 0.5f * particle.mass * particle.velocity.sqrMagnitude;
    }

    private Color GetMassColor(float mass)
    {
        return Color.Lerp(Color.blue, Color.red, mass / 1000);
    }

    // Simulate gravity using the Barnes-Hut algorithm (O(n log n) complexity)
    private void SimulateBarnesHut()
    {
        try
        {
            TrailRenderer trailRenderer;
            Renderer particleRenderer;

            Parallel.ForEach(particles, particle =>
            {
                particle.acceleration = octree.CalculateForceBarnesHut(particle, octree, 0.5f);
                particle.velocity += particle.acceleration * _deltaTime;
                particle.acceleration = Vector3.zero;
            });

            for (int i = 0; i < particles.Count; i++)
            {
                ParticleEntity particle = particles[i];
                particle.SetPosition(particle.position + particle.velocity * _deltaTime);

                // Cache components
                trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
                particleRenderer = particle.particleObject.GetComponent<Renderer>();

                // Aggiorna lo stato del TrailRenderer in base alla variabile 'showOrbitLines'
                if (trailRenderer != null)
                {
                    trailRenderer.emitting = showOrbitLines;
                }

                // Aggiorna il colore della particella in base alle sue proprietà
                if (particleRenderer != null)
                {
                    Color particleColor;
                    if (showKineticEnergy)
                    {
                        particle.kineticEnergy = CalculateKineticEnergy(particle);
                        particleColor = GetKineticEnergyColor(particle.kineticEnergy);
                        if (particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                            particleRenderer.material.DisableKeyword("_EMISSION");
                    }
                    else if (showVelocityColor)
                    {
                        particleColor = GetVelocityColor(particle.velocity);
                        if (particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                            particleRenderer.material.DisableKeyword("_EMISSION");
                    }
                    else
                    {
                        particleColor = GetStarColor(particle.temperature);
                        if (!particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                            particleRenderer.material.EnableKeyword("_EMISSION");
                    }

                    particleRenderer.material.color = particleColor;
                }
            }

            if (Time.time > nextUpdate)
            {
                nextUpdate = Time.time + 1;
                iterationsPerSec = 1 / Time.deltaTime;
                iterationsPerSecText.text = iterationsPerSec.ToString("F0") + "it/s";
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
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

        Parallel.ForEach(particles, particle =>
        {
            if (particle == null) return;
            particle.velocity += particle.acceleration * _deltaTime;

            particle.SetPosition(particle.position + particle.velocity * _deltaTime);

            particle.acceleration = Vector3.zero;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                TrailRenderer trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
                trailRenderer.emitting = showOrbitLines;
                Renderer particleRenderer = particle.particleObject.GetComponent<Renderer>();

                // Aggiorna lo stato del TrailRenderer in base alla variabile 'showOrbitLines'
                if (trailRenderer != null)
                {
                    trailRenderer.emitting = showOrbitLines;
                }

                // Aggiorna il colore della particella in base alle sue proprietà
                if (particleRenderer != null)
                {
                    if (showKineticEnergy)
                    {
                        particle.kineticEnergy = CalculateKineticEnergy(particle);
                        particleRenderer.material.color = GetKineticEnergyColor(particle.kineticEnergy);
                        if (particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                            particleRenderer.material.DisableKeyword("_EMISSION");
                    }
                    else if (showVelocityColor)
                    {
                        particleRenderer.material.color = GetVelocityColor(particle.velocity);
                        if (particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                            particleRenderer.material.DisableKeyword("_EMISSION");
                    }
                    else
                    {
                        particleRenderer.material.color = GetStarColor(particle.temperature);
                        if (!particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                            particleRenderer.material.EnableKeyword("_EMISSION");
                    }
                }
            });
        });

        if (Time.time > nextUpdate)
        {
            nextUpdate = Time.time + 1;
            iterationsPerSec = 1 / Time.deltaTime;
            iterationsPerSecText.text = iterationsPerSec.ToString("F0") + "it/s";
        }
    }

    private Color GetKineticEnergyColor(float kineticEnergy)
    {
        return Color.Lerp(Color.blue, Color.red, kineticEnergy / 1000);
    }

    private Color GetVelocityColor(Vector3 velocity)
    {
        float speed = velocity.magnitude;
        return Color.Lerp(Color.blue, Color.red, speed / 100);
    }

    private void MergeParticle(ParticleEntity currentEntity, ParticleEntity nextEntity)
    {
        float newMass = currentEntity.mass + nextEntity.mass;
        Vector3 newPosition = (currentEntity.position * currentEntity.mass + nextEntity.position * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
        Vector3 newVelocity = (currentEntity.velocity * currentEntity.mass + nextEntity.velocity * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
        Vector3 newSize = currentEntity.size + nextEntity.size;
        float newTemperature = (currentEntity.temperature + nextEntity.temperature) / 2;
        bool isBlackHole = false; // Disable black hole creation for now
        Star newMergedObject = GetStarTypeByTemperature(newTemperature, newMass, isBlackHole);
        string type = isBlackHole ? "Black Hole" : newMergedObject.Type;
        newMergedObject.color = isBlackHole ? Color.black : GetStarColor(newTemperature);

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            GameObject newParticleObject = CreateParticle(newSize, newMergedObject.color, newPosition.x, newPosition.y, newPosition.z, isBlackHole);
            ParticleEntity mergedParticle = new ParticleEntity(newSize, newVelocity, newMass, newTemperature, type, newParticleObject)
            {
                isBlackHole = isBlackHole
            };
            particles = particles.Where(p => p != currentEntity && p != nextEntity).ToList();
            particles.Add(mergedParticle);

            if (lockedParticle != null)
            {
                ObjectInfoModel objectInfoModel = GetObjectInfoModel(mergedParticle);
                objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
                lockedParticle = mergedParticle;
            }

            if (currentEntity.particleObject != null && !currentEntity.particleObject.IsDestroyed())
                Destroy(currentEntity.particleObject);
            if (nextEntity.particleObject != null && !nextEntity.particleObject.IsDestroyed())
                Destroy(nextEntity.particleObject);
        });
    }

    private void OnApplicationQuit()
    {
        foreach (ParticleEntity particle in particles)
        {
            if (particle != null && particle.particleObject != null && !particle.particleObject.IsDestroyed())
                Destroy(particle.particleObject);
        }

        particles = null;
    }
}
