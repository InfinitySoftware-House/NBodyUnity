using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using PimDeWitte.UnityMainThreadDispatcher;
using Unity.VisualScripting;
using UnityEditor.Animations;
using System.Linq;
using System;
using UnityEditor;
using Random = UnityEngine.Random;

public class ParticleEntity
{
    public string name;
    public Vector3 position;
    public float mass;
    public Vector3 acceleration = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
    public float temperature = 0;
    public Vector3 size;
    public GameObject particleObject;
    public string type;
    public bool isBlackHole = false;
    public float kineticEnergy;

    public ParticleEntity(Vector3 size, Vector3 velocity, float mass, float temperature, string type, GameObject particleObject, string name = "")
    {
        this.size = size;
        position = particleObject.transform.position;
        this.mass = mass;
        this.velocity = velocity;
        this.particleObject = particleObject;
        this.temperature = temperature;
        this.size = size;
        this.type = type;
        this.name = string.IsNullOrEmpty(name) ? RandomNameGenerator() : name;
        isBlackHole = mass >= 1000;
    }
    private string RandomNameGenerator()
    {
        string[] prefixes = { "Al", "Bet", "Sir", "Veg", "Rig", "Prox", "Cap", "Veg", "Can", "Poll" };
        string[] middles = { "tar", "gol", "nix", "phar", "lux", "crix", "bell", "dor", "hul", "mir" };
        string[] suffixes = { "us", "a", "ion", "en", "ar", "os", "ra", "is", "es", "ia" };
        string[] romanNumerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        string[] greekLetters = { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa" };

        return prefixes[Random.Range(0, prefixes.Length)] + middles[Random.Range(0, middles.Length)] + suffixes[Random.Range(0, suffixes.Length)] + " " + (Random.Range(0, 2) == 0 ? romanNumerals[Random.Range(0, romanNumerals.Length)] : greekLetters[Random.Range(0, greekLetters.Length)]);
    }

    public void setPosition(Vector3 newPosition)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            position = newPosition;
            particleObject.transform.position = newPosition;
        });
    }
}

public struct Planet
{
    public float Mass; // in Earth masses
    public Color color;

    public Planet(float mass, Color color)
    {
        Mass = mass;
        this.color = color;
    }
}

public struct Star
{
    public string Type;
    public float Mass; // in solar masses
    public float Temperature; // in Kelvin
    public Color color;

    public Star(string type, float mass, float temperature, Color color)
    {
        Type = type;
        Mass = mass;
        Temperature = temperature;
        this.color = color;
    }
}

public class Simulation : MonoBehaviour
{
    public GameObject particlePrefab;
    private ParticleEntity[] particles = new ParticleEntity[0];
    private readonly float G = 6.6743f;
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
    public AnimationClip mergeAnimation;
    public AnimatorController animatorController;
    private Vector3 _particleSize = new Vector3(0.1f, 0.1f, 0.1f);

    public TMP_Text showBloomText;
    public TMP_Text showOrbitLinesText;
    public TMP_Text showKineticEnergyText;
    public TMP_Text showVelocityColorText;

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

    private void CreateCluster(Scene currentScene, Vector3 position, int count = 20, bool hasBlackHole = false)
    {
        // Vector3 massCenter = position;
        // AddParticle(currentScene, massCenter, Vector3.zero, mass: 400);
        ParticleEntity[] newParticles = new ParticleEntity[count];
        // Create a cluster of particles
        for (int i = 0; i < count; i++)
        {
            float innerRadius = 6f; // Inner radius of the ring
            float outerRadius = 14f; // Outer radius of the ring
            float angle = i * 2.0f * Mathf.PI / count; // Distribute particles evenly around the circle

            // Randomize radius within the ring bounds
            float radius = Random.Range(innerRadius, outerRadius);
            // Calculate x, y, z coordinates for a ring-shaped galaxy
            float x = position.x + radius * Mathf.Cos(angle);
            float y = position.y + radius * Mathf.Sin(angle);
            float z = position.z; // Assuming you want the ring to be horizontal, keep z constant
            Vector3 newPosition = new Vector3(x, y, z);
            // Add particle with the new combined velocity
            Vector3 velocity = Quaternion.Euler(0, 0, i * 360f / count) * (Random.insideUnitSphere * 10);
            newParticles[i] = AddParticle(currentScene, newPosition, velocity);
        }
        particles = particles.Concat(newParticles).ToArray();
    }

    private Vector3 GetMassCenter()
    {
        Vector3 massCenter = Vector3.zero;
        foreach (ParticleEntity particle in particles)
        {
            massCenter += particle.position;
        }
        return massCenter / particles.Length;
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

    GameObject CreateParticle(Vector3 size, Color color, float? x = null, float? y = null, float? z = null, bool isBlackHole = false, bool isStellar = true)
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
            particle.transform.position = UnityEngine.Random.insideUnitSphere * 20;
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

    bool CheckCollision(ParticleEntity body1, ParticleEntity body2)
    {
        float radiusSum = body1.size.magnitude + body2.size.magnitude;
        float distance = Vector3.Distance(body1.position, body2.position);
        return distance < radiusSum / 2;
    }

    private ParticleEntity AddParticle(Scene currentScene, Vector3? position = null, Vector3? velocity = null, float mass = 1)
    {
        Star star = GenerateRandomStar();
        star.Mass = mass;
        Color color = GetStarColor(star.Temperature);
        GameObject particleObject = CreateParticle(_particleSize, color, position?.x, position?.y, position?.z); 

        SceneManager.MoveGameObjectToScene(particleObject, currentScene);
        return new(_particleSize, velocity ?? Vector3.zero, star.Mass, star.Temperature, star.Type, particleObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        objectInfoObject.SetActive(false);
        particlesCountText.text = "Objects: " + particles.Length.ToString();
        iterationsPerSecText.text = "0it/s";
        yearPassedText.text = yearPassed.ToString() + " Y";
        // newStarVelocityText.text = "v " + _starVelocity.ToString();
        Vector3 centerOfCamera = Camera.main.transform.position;
        Vector3 targetPosition = new Vector3(centerOfCamera.x, centerOfCamera.y, centerOfCamera.z + 1);
        // AddParticle(SceneManager.GetActiveScene(), targetPosition);

        showBloomText.color = showBloom ? Color.green : Color.white;
        showOrbitLinesText.color = showOrbitLines ? Color.green : Color.white;
        showKineticEnergyText.color = showKineticEnergy ? Color.green : Color.white;
        showVelocityColorText.color = showVelocityColor ? Color.green : Color.white;
    }

    ObjectInfoModel GetObjectInfoModel(ParticleEntity particle)
    {
        ObjectInfoModel objectInfoModel = new ObjectInfoModel
        {
            Name = particle.name,
            Type = particle.type,
            Temperature = particle.temperature,
            Mass = particle.mass
        };
        return objectInfoModel;
    }

    // Update is called once per frame
    void Update()
    {
        yearPassedText.text = yearPassed.ToString() + " Y";

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
            Scene scene = SceneManager.GetActiveScene();
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = Camera.main.nearClipPlane + 1;
            Vector3 newParticlePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            if (particles.Length == 0)
            {
                ParticleEntity newParticle1 = AddParticle(scene, newParticlePosition);
                ArrayUtility.Add(ref particles, newParticle1);
                return;
            }

            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 velocity = cameraForward.normalized;
            velocity *= _starVelocity / 10;
            ParticleEntity newParticle2 = AddParticle(scene, newParticlePosition, velocity);
            ArrayUtility.Add(ref particles, newParticle2);
        }

        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i] == null) continue;
                    if (particles[i].particleObject == hit.collider.gameObject)
                    {
                        ParticleEntity particle = particles[i];
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
            }
            else
            {
                lockedParticle = null;
                objectInfoObject.SetActive(false);
            }
        }

        if (lockedParticle != null)
        {
            Camera.main.transform.LookAt(lockedParticle.position);
        }

        _deltaTime = Time.deltaTime / 50;
        SimulateGravity();

        particlesCountText.text = "Objects: " + particles.Length.ToString();
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            showBloom = !showBloom;
            foreach (ParticleEntity particle in particles)
            {
                if (!showBloom)
                    particle.particleObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
                else
                    particle.particleObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
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

        //Cluster settings
        if (Input.GetKeyDown(KeyCode.I))
        {

        }

        if(Input.GetKeyDown(KeyCode.V))
        {
            showVelocityColor = !showVelocityColor;
            showVelocityColorText.color = showVelocityColor ? Color.green : Color.white;
        }
    }

    float CalculateKineticEnergy(ParticleEntity particle)
    {
        return 0.5f * particle.mass * particle.velocity.sqrMagnitude;
    }

    // Simulate gravity
    private void SimulateGravity()
    {
        Parallel.For(0, particles.Length, i =>
        {
            try
            {
                if (i >= particles.Length) return; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                ParticleEntity currentEntity = particles[i];
                int startFrom = particles.Length < 3 ? 0 : i + 1;
                for (int j = startFrom; j < particles.Length; j++) // Inizia da j = i + 1 per evitare calcoli duplicati e autointerazioni
                {
                    if (j == i) continue; // Evita calcoli duplicati e autointerazioni
                    if (j >= particles.Length) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                    if (i >= particles.Length) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
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
                        if (showKineticEnergy)
                        {
                            currentEntity.kineticEnergy = CalculateKineticEnergy(currentEntity);
                        }
                    }
                    else if (distance == 0)
                    {
                        continue;
                    }
                    // else
                    // {
                    //     MergeParticle(currentEntity, nextEntity);
                    // }
                }
            }
            catch(System.Exception e)
            {
                Debug.Log(e);
            }
        });

        Parallel.ForEach(particles, particle =>
        {
            if (particle == null) return;
            particle.velocity += particle.acceleration * _deltaTime;

            particle.setPosition(particle.position + particle.velocity * _deltaTime);

            particle.acceleration = Vector3.zero;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                TrailRenderer trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
                trailRenderer.emitting = showOrbitLines;
                Renderer particleRenderer = particle.particleObject.GetComponent<Renderer>();
                if (showKineticEnergy)
                {
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
            });
        });

        yearPassed = (int)Time.realtimeSinceStartup * 10;

        if (Time.time > nextUpdate)
        {
            nextUpdate = Time.time + 1;
            iterationsPerSec = 1 / _deltaTime;
            iterationsPerSecText.text = iterationsPerSec.ToString("F0") + "it/s";
        }
    }

    void RunMergeAnimation(ParticleEntity particle)
    {
        Animator animator = particle.particleObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = particle.particleObject.AddComponent<Animator>();
        }

        // Create a new AnimationClip and assign it to the Animator
        if (mergeAnimation != null)
        {
            // Ensure there is an AnimatorController
            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = animatorController;
                // execute the merge animation
                animator.Play(mergeAnimation.name);
            }
        }
    }

    Color GetKineticEnergyColor(float kineticEnergy)
    {
        return Color.Lerp(Color.blue, Color.red, kineticEnergy / 1000);
    }

    Color GetVelocityColor(Vector3 velocity)
    {
        float speed = velocity.magnitude;
        return Color.Lerp(Color.blue, Color.red, speed / 100);
    }

    void MergeParticle(ParticleEntity currentEntity, ParticleEntity nextEntity)
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
            ParticleEntity mergedParticle = new(newSize, newVelocity, newMass, newTemperature, type, newParticleObject)
            {
                isBlackHole = isBlackHole
            };
            particles[particles.Length] = mergedParticle;
            RunMergeAnimation(mergedParticle);
            if (lockedParticle != null)
            {
                ObjectInfoModel objectInfoModel = GetObjectInfoModel(mergedParticle);
                objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
                lockedParticle = mergedParticle;
            }
        });
        particles = particles.Where(p => p != currentEntity && p != nextEntity).ToArray();
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (currentEntity.particleObject != null && !currentEntity.particleObject.IsDestroyed())
                Destroy(currentEntity.particleObject);
            if (nextEntity.particleObject != null && !nextEntity.particleObject.IsDestroyed())
                Destroy(nextEntity.particleObject);
        });
    }

    void OnApplicationQuit()
    {
        foreach (ParticleEntity particle in particles)
        {
            if (particle != null && particle.particleObject != null && !particle.particleObject.IsDestroyed())
                Destroy(particle.particleObject);
        }

        particles = null;
    }
}