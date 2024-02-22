using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using PimDeWitte.UnityMainThreadDispatcher;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEditor.Animations;

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
    private List<ParticleEntity> particles = new List<ParticleEntity>();
    private readonly float G = 6.67430f;
    private float _deltaTime = 0;
    public TMP_Text particlesCountText;
    public TMP_Text iterationsPerSecText;
    public TMP_Text yearPassedText;
    public TMP_Text newStarVelocityText;
    public bool showOrbitLines = false;
    public bool showBloom = false;
    private double iterationsPerSec = 0;
    private int yearPassed = 0;
    private double nextUpdate = 1;
    public GameObject objectInfoObject;
    private int _starVelocity = 10;
    private ParticleEntity lockedParticle;
    private string[] STAR_TYPE = { "Brown Dwarf", "Red Dwarf", "Orange Dwarf", "Yellow Dwarf", "Yellow-White Dwarf", "White Star", "Blue-White Star", "Blue Star" };
    private float BLACK_HOLE_MASS = 1000;
    private bool showKineticEnergy = false;
    public GameObject legendPanel;
    public AnimationClip mergeAnimation;
    public AnimatorController animatorController;

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
        // Create a cluster of particles
        for (int i = 0; i < count; i++)
        {
            float? x = null;
            float? y = null;
            float? z = null;
            Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
            Star star = GenerateRandomStar();
            x ??= Random.Range(position.x - 10, position.x + 10);
            y ??= Random.Range(position.y - 10, position.y + 10);
            z ??= Random.Range(position.z - 10, position.z + 10);

            Color color = GetStarColor(star.Temperature);

            GameObject particleObject = CreateParticle(size, color, x, y, z);

            SceneManager.MoveGameObjectToScene(particleObject, currentScene);
            Vector3 randomVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            ParticleEntity particle = new(size, randomVelocity, star.Mass, star.Temperature, star.Type, particleObject);
            particles.Add(particle);
        }

        if (hasBlackHole)
        {
            Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
            Star star = new Star("Black Hole", BLACK_HOLE_MASS, 1000, Color.black);
            Vector3 massCenter = GetMassCenter();

            Color color = GetStarColor(star.Temperature);

            GameObject particleObject = CreateParticle(size, color, massCenter.x, massCenter.y, massCenter.z, true, true);

            SceneManager.MoveGameObjectToScene(particleObject, currentScene);
            ParticleEntity particle = new(size, Vector3.zero, star.Mass, star.Temperature, star.Type, particleObject);
            particles.Add(particle);
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
        float[,] massRanges = { { 0.01f, 0.08f }, { 0.08f, 0.45f }, { 0.45f, 0.8f }, { 0.8f, 1.04f }, { 1.04f, 1.4f }, { 1.4f, 2.1f }, { 2.1f, 16f }, { 16f, 50f } };
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
        float[,] massRanges = { { 0.01f, 0.08f }, { 0.08f, 0.45f }, { 0.45f, 0.8f }, { 0.8f, 1.04f }, { 1.04f, 1.4f }, { 1.4f, 2.1f }, { 2.1f, 16f }, { 16f, 50f } };
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
        trailRenderer.startWidth = 0.06f;
        trailRenderer.endWidth = 0.03f;
        trailRenderer.time = 6f;
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

    bool CheckCollision(ParticleEntity body1, ParticleEntity body2)
    {
        float radiusSum = body1.size.magnitude + body2.size.magnitude;
        float distance = Vector3.Distance(body1.position, body2.position);
        return distance < radiusSum / 2;
    }

    private void AddParticle(Scene currentScene, Vector3? position = null, Vector3? velocity = null)
    {
        Star star = GenerateRandomStar();
        Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
        Color color = GetStarColor(star.Temperature);
        GameObject particleObject = CreateParticle(size, color, position?.x, position?.y, position?.z);

        SceneManager.MoveGameObjectToScene(particleObject, currentScene);
        ParticleEntity particle = new(size, velocity ?? Vector3.zero, star.Mass, star.Temperature, star.Type, particleObject);
        particles.Add(particle);
    }

    // Start is called before the first frame update
    void Start()
    {
        objectInfoObject.SetActive(false);
        particlesCountText.text = "Objects: " + particles.Count.ToString();
        iterationsPerSecText.text = "0it/s";
        yearPassedText.text = yearPassed.ToString() + " Y";
        newStarVelocityText.text = "v " + _starVelocity.ToString();
        Vector3 centerOfCamera = Camera.main.transform.position;
        Vector3 targetPosition = new Vector3(centerOfCamera.x, centerOfCamera.y, centerOfCamera.z + 5);
        AddParticle(SceneManager.GetActiveScene(), targetPosition);
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
            if (particles.Count == 0)
            {
                AddParticle(scene, newParticlePosition);
                return;
            }

            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 velocity = cameraForward.normalized;
            velocity *= _starVelocity / 10;
            AddParticle(scene, newParticlePosition, velocity);
        }

        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                ParticleEntity particle = particles.Find(p => p.particleObject == hit.collider.gameObject);
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

        if (lockedParticle != null)
        {
            Camera.main.transform.LookAt(lockedParticle.position);
        }

        _deltaTime = Time.deltaTime / 50;
        SimulateGravity();

        particlesCountText.text = "Objects: " + particles.Count.ToString();
        // 100 particles
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Scene currentScene = SceneManager.GetActiveScene();
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            CreateCluster(currentScene, currentPosition, 100, true);
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
            CreateCluster(currentScene, currentPosition, 200, true);
        }
        // 400 particles
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Vector3 currentPosition = Camera.main.transform.position;
            currentPosition.z += 10;
            Scene currentScene = SceneManager.GetActiveScene();
            CreateCluster(currentScene, currentPosition, 400, true);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            showOrbitLines = !showOrbitLines;
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            if (_starVelocity < 20)
                _starVelocity += 1;
            newStarVelocityText.text = "v " + _starVelocity.ToString();
        }
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            if (_starVelocity > 1)
                _starVelocity -= 1;
            newStarVelocityText.text = "v " + _starVelocity.ToString();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // reset the scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        if (Input.GetKeyDown(KeyCode.K))
        {
            showKineticEnergy = !showKineticEnergy;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            legendPanel.SetActive(!legendPanel.activeSelf);
        }

        //Cluster settings
        if (Input.GetKeyDown(KeyCode.I))
        {

        }
    }

    float CalculateKineticEnergy(ParticleEntity particle)
    {
        return 0.5f * particle.mass * particle.velocity.sqrMagnitude;
    }

    // Simulate gravity
    private void SimulateGravity()
    {
        Parallel.For(0, particles.Count, i =>
        {
            if (i >= particles.Count) return; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
            ParticleEntity currentEntity = particles[i];
            int startFrom = particles.Count < 3 ? 0 : i + 1;
            for (int j = startFrom; j < particles.Count; j++) // Inizia da j = i + 1 per evitare calcoli duplicati e autointerazioni
            {
                if (j == i) continue; // Evita calcoli duplicati e autointerazioni
                if (j >= particles.Count) break; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                if (i >= particles.Count) break; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                ParticleEntity nextEntity = particles[j];
                if (nextEntity == null) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                if (currentEntity == null) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
                Vector3 distanceVector = nextEntity.position - currentEntity.position;
                float distance = distanceVector.magnitude;
                Vector3 forceDirection = distanceVector.normalized;

                if (distance > 100) // Evita calcoli inutili (particelle troppo lontane tra loro)
                {
                    continue;
                }

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
                else
                {
                    MergeParticle(currentEntity, nextEntity);
                }
            }
        });

        Parallel.ForEach(particles, particle =>
        {
            particle.velocity += particle.acceleration * _deltaTime;

            particle.setPosition(particle.position + particle.velocity * _deltaTime);

            particle.acceleration = Vector3.zero;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                TrailRenderer trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
                trailRenderer.emitting = showOrbitLines;
                if (showKineticEnergy)
                {
                    particle.particleObject.GetComponent<Renderer>().material.color = GetKineticEnergyColor(particle.kineticEnergy);
                }
                else
                {
                    particle.particleObject.GetComponent<Renderer>().material.color = GetStarColor(particle.temperature);
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

    void MergeParticle(ParticleEntity currentEntity, ParticleEntity nextEntity)
    {
        float newMass = currentEntity.mass + nextEntity.mass;
        Vector3 newPosition = (currentEntity.position * currentEntity.mass + nextEntity.position * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
        Vector3 newVelocity = (currentEntity.velocity * currentEntity.mass + nextEntity.velocity * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
        Vector3 newSize = (currentEntity.size * currentEntity.mass + nextEntity.size * nextEntity.mass) / (currentEntity.mass + nextEntity.mass);
        // Vector3 newSize = new Vector3(0.1f, 0.1f, 0.1f);
        float newTemperature = (currentEntity.temperature + nextEntity.temperature) / 2;
        bool isBlackHole = newMass >= BLACK_HOLE_MASS;
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
            particles.Add(mergedParticle);
            RunMergeAnimation(mergedParticle);
            if (lockedParticle != null)
            {
                ObjectInfoModel objectInfoModel = GetObjectInfoModel(mergedParticle);
                objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
                lockedParticle = mergedParticle;
            }
        });
        particles.Remove(currentEntity);
        particles.Remove(nextEntity);
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Destroy(currentEntity.particleObject);
            Destroy(nextEntity.particleObject);
        });
    }

    void OnApplicationQuit()
    {
        foreach (ParticleEntity particle in particles)
        {
            Destroy(particle.particleObject);
        }

        particles.Clear();
    }

    public void CreateSolarSystem(Vector3 position)
    {
        // Sun
        Star sun = new("Yellow Dwarf", 1, 5778, Color.yellow);
        Vector3 sunSize = new Vector3(1f, 1f, 1f);
        GameObject sunObject = CreateParticle(sunSize, sun.color, position.x, position.y, position.z);
        ParticleEntity sunParticle = new(sunSize, Vector3.zero, sun.Mass, sun.Temperature, sun.Type, sunObject);
        particles.Add(sunParticle);

        // Mercury
        Planet mercury = new(0.000000166f, Color.gray);
        Vector3 mercurySize = new Vector3(0.2f, 0.2f, 0.2f);
        GameObject mercuryObject = CreateParticle(mercurySize, mercury.color, position.x + 10f, position.y, position.z, isStellar: false);
        ParticleEntity mercuryParticle = new(mercurySize, new Vector3(0, 0.9f, 0), mercury.Mass, 0, "Planet", mercuryObject, "Mercury");
        particles.Add(mercuryParticle);

        // Venus
        Planet venus = new(0.000002447f, Color.yellow);
        Vector3 venusSize = new Vector3(0.3f, 0.3f, 0.3f);
        GameObject venusObject = CreateParticle(venusSize, venus.color, position.x + 40f, position.y, position.z, isStellar: false);
        ParticleEntity venusParticle = new(venusSize, new Vector3(0, 0.8f, 0), venus.Mass, 0, "Planet", venusObject, "Venus");
        particles.Add(venusParticle);

        // // Earth
        // Planet earth = new(0.000003003f, Color.blue);
        // Vector3 earthSize = new Vector3(0.3f, 0.3f, 0.3f);
        // GameObject earthObject = CreateParticle(earthSize, earth.color, position.x + 20f, position.y, position.z, isStar: false);
        // ParticleEntity earthParticle = new(earthSize, new Vector3(0, 0, 0.3f), earth.Mass, 0, "Planet", earthObject);
        // particles.Add(earthParticle);
    }
}