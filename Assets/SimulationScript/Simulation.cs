using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using PimDeWitte.UnityMainThreadDispatcher;
using Unity.VisualScripting;
using System.Linq;
using Random = UnityEngine.Random;
using System;

public class Simulation : MonoBehaviour
{
    public GameObject particlePrefab;
    public GameObject blackHolePrefab;
    private List<ParticleEntity> particles = new List<ParticleEntity>();
    private float _deltaTime = 0;
    public TMP_Text particlesCountText;
    public TMP_Text iterationsPerSecText;
    public TMP_Text yearPassedText;
    public bool showOrbitLines = false;
    public bool showBloom = true;
    private double iterationsPerSec = 0;
    private int yearPassed = 0;
    private double nextUpdate = 1;
    public GameObject objectInfoObject;
    private int _starVelocity = 4;
    private ParticleEntity lockedParticle;
    private bool showKineticEnergy = false;
    private bool showVelocityColor = false;
    public GameObject hud;
    private Vector3 _particleSize = new Vector3(0.08f, 0.08f, 0.08f);
    public TMP_Text showBloomText;
    public TMP_Text showOrbitLinesText;
    public TMP_Text showKineticEnergyText;
    public TMP_Text showVelocityColorText;
    public OctreeNode octree { get; private set; }
    private bool runSimulation = false;
    private bool isGalaxy = false;
    private bool showMass = false;
    public bool isMainMenu = false;
    public bool showRedshift;
    private bool startCameraRotation;
    private bool showHUD = true;
    public GameObject galaxyModePanel;

    private void CreateCluster(Scene currentScene, Vector3 position, int count = 20)
    {
        Vector3 massCenter = position;
        // if (isGalaxy){
        //     // massCenter.z += 10;
        //     Star blackHole = new Star("Black Hole", 100, 0, Color.black);
        //     particles.Add(AddParticle(blackHole, currentScene, massCenter, Vector3.zero));
        // }
        List<Star> stars = Utility.GenerateStars(count);
        // Create a cluster of particles
        for (int i = 0; i < count; i++)
        {
            Vector3 newPosition;
            if(isGalaxy){
                float innerRadius = 6f; // Inner radius of the ring
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
                float multiplier = Mathf.Max(20, count / 100); // Multiplier to increase the rectangular area
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
                // make the stars rotate around the center
                Vector3 direction = massCenter - newPosition;
                direction.z = 10;
                velocity = Vector3.Cross(direction, Vector3.forward);
            } else {
                // Randomize velocity
                velocity.x = Random.Range(-_starVelocity, _starVelocity);
                velocity.y = Random.Range(-_starVelocity, _starVelocity);
                velocity.z = Random.Range(-_starVelocity, _starVelocity);
            }
            particles.Add(AddParticle(stars[i], currentScene, newPosition, velocity));
        }
    }

    private int GetUniverseSize()
    {
        int universeSize = 0;
        Parallel.ForEach(particles, particle =>
        {
            float distance = Vector3.Distance(particle.position, Vector3.zero);
            if (distance > universeSize)
            {
                universeSize = (int)distance;
            }
        });
        return universeSize;
    }

    private void CreateOctree()
    {
        Vector3 massCenterForSim = GetMassCenter();
        int universeSize = GetUniverseSize();
        octree = new OctreeNode(massCenterForSim, universeSize);

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

    private GameObject CreateParticle(Vector3 size, Color color, float? x = null, float? y = null, float? z = null, bool isBlackHole = false, bool isStellar = true)
    {
        GameObject particle;
        if (isBlackHole)
        {
            particle = Instantiate(blackHolePrefab);
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

    private ParticleEntity AddParticle(Star star, Scene currentScene, Vector3? position = null, Vector3? velocity = null, float mass = 0.5f)
    {
        bool isBlackHole = mass >= 1000;
        GameObject particleObject = CreateParticle(_particleSize, star.color, position?.x, position?.y, position?.z, isBlackHole, !isBlackHole);

        SceneManager.MoveGameObjectToScene(particleObject, currentScene);
        return new(_particleSize, velocity ?? Vector3.zero, star.Mass, star.Temperature, star.Type, particleObject, star.color);
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

        iterationsPerSecText.color = runSimulation ? Color.white : Color.red;

        if (Input.GetKeyDown(KeyCode.H))
        {
            showRedshift = !showRedshift;
            Debug.Log("Redshift: " + (showRedshift ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Show the main menu
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        if(Input.GetKeyDown(KeyCode.Space)){
            runSimulation = !runSimulation;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                ParticleEntity selectedParticle = particles.FirstOrDefault(p => p.name == hit.collider.gameObject.name);
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
            GameObject gameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == lockedParticle.name);
            Camera.main.transform.LookAt(gameObject.transform);
        }

        _deltaTime = Time.deltaTime / 20;

        if (runSimulation)
        {
            if(particles.Count > 0)
            {
                CreateOctree();
            }
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
                GameObject gameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == particle.name);
                if (particle != null && gameObject != null && !gameObject.IsDestroyed())
                    Destroy(gameObject);
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
                GameObject gameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == particle.name);
                if (!showBloom)
                {
                    gameObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
                    gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
                }
                else
                {
                    gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                    gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", particle.color);
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
            showHUD = !showHUD;
            // if (!showHUD)
            //     StartCoroutine(Utility.FadeOutCanvas(hud));
            // else
            //     StartCoroutine(Utility.FadeInCanvas(hud));

            hud.SetActive(!hud.activeSelf);
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
            galaxyModePanel.SetActive(isGalaxy);
        }

        // Garbage collect every 1000 iterations
        // if (yearPassed % 1000 == 0 && particles.Count > 1000)
        // {
        //     GC.Collect();
        // }

        if (Input.GetKeyDown(KeyCode.P))
        {
            startCameraRotation = !startCameraRotation;
        }

        if (startCameraRotation && particles.Count > 0)
        {
            Vector3 universeCenter = GetMassCenter();
            RotateTheCameraAround(universeCenter, 14);
        }   
    }

    private void RotateTheCameraAround(Vector3 position, float speed)
    {
        Camera.main.transform.RotateAround(position, Vector3.up, speed * Time.deltaTime);
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

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = SystemInfo.processorCount
            };

            Parallel.ForEach(particles, parallelOptions, particle =>
            {
                particle.acceleration = octree.CalculateForceBarnesHut(particle, octree, 1.0f);
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
                    else if (showRedshift)
                    {
                        particleColor = Color.Lerp(Color.red, Color.blue, Utility.CalculateRedshift(particle.position.magnitude, particle.velocity.magnitude));
                    }
                    else
                    {
                        particleColor = particle.color;
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

    // // Simulate gravity O(n^2) complexity
    // private void SimulateGravity()
    // {
    //     Parallel.For(0, particles.Count, i =>
    //     {
    //         try
    //         {
    //             if (i >= particles.Count) return; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
    //             ParticleEntity currentEntity = particles[i];
    //             int startFrom = particles.Count < 3 ? 0 : i + 1;
    //             for (int j = startFrom; j < particles.Count; j++) // Inizia da j = i + 1 per evitare calcoli duplicati e autointerazioni
    //             {
    //                 if (j == i) continue; // Evita calcoli duplicati e autointerazioni
    //                 if (j >= particles.Count) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
    //                 if (i >= particles.Count) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
    //                 ParticleEntity nextEntity = particles[j];
    //                 if (nextEntity == null) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
    //                 if (currentEntity == null) continue; // Evita l'indice fuori dai limiti (in caso di collisione e fusione di particelle
    //                 Vector3 distanceVector = nextEntity.position - currentEntity.position;
    //                 float distance = distanceVector.magnitude;
    //                 Vector3 forceDirection = distanceVector.normalized;

    //                 if (!CheckCollision(currentEntity, nextEntity))
    //                 {
    //                     float forceMagnitude = Utility.G * (currentEntity.mass * nextEntity.mass) / (distance * distance);
    //                     Vector3 force = forceDirection * forceMagnitude;

    //                     currentEntity.acceleration += force / currentEntity.mass;
    //                     nextEntity.acceleration -= force / nextEntity.mass;
    //                 }
    //                 else if (distance == 0)
    //                 {
    //                     // MergeParticle(currentEntity, nextEntity);
    //                     continue;
    //                 }
    //             }
    //         }
    //         catch (System.Exception e)
    //         {
    //             Debug.Log(e);
    //         }
    //     });

    //     Parallel.ForEach(particles, particle =>
    //     {
    //         if (particle == null) return;
    //         particle.velocity += particle.acceleration * _deltaTime;

    //         particle.SetPosition(particle.position + particle.velocity * _deltaTime);

    //         particle.acceleration = Vector3.zero;

    //         UnityMainThreadDispatcher.Instance().Enqueue(() =>
    //         {
    //             TrailRenderer trailRenderer = particle.particleObject.GetComponent<TrailRenderer>();
    //             trailRenderer.emitting = showOrbitLines;
    //             Renderer particleRenderer = particle.particleObject.GetComponent<Renderer>();

    //             // Aggiorna lo stato del TrailRenderer in base alla variabile 'showOrbitLines'
    //             if (trailRenderer != null)
    //             {
    //                 trailRenderer.emitting = showOrbitLines;
    //             }

    //             // Aggiorna il colore della particella in base alle sue proprietà
    //             if (particleRenderer != null)
    //             {
    //                 if (showKineticEnergy)
    //                 {
    //                     particle.kineticEnergy = CalculateKineticEnergy(particle);
    //                     particleRenderer.material.color = GetKineticEnergyColor(particle.kineticEnergy);
    //                     if (particleRenderer.material.IsKeywordEnabled("_EMISSION"))
    //                         particleRenderer.material.DisableKeyword("_EMISSION");
    //                 }
    //                 else if (showVelocityColor)
    //                 {
    //                     particleRenderer.material.color = GetVelocityColor(particle.velocity);
    //                     if (particleRenderer.material.IsKeywordEnabled("_EMISSION"))
    //                         particleRenderer.material.DisableKeyword("_EMISSION");
    //                 }
    //                 else
    //                 {
    //                     particleRenderer.material.color = Utility.GetStarColor(particle.temperature);
    //                     if (!particleRenderer.material.IsKeywordEnabled("_EMISSION"))
    //                         particleRenderer.material.EnableKeyword("_EMISSION");
    //                 }
    //             }
    //         });
    //     });

    //     if (Time.time > nextUpdate)
    //     {
    //         nextUpdate = Time.time + 1;
    //         iterationsPerSec = 1 / Time.deltaTime;
    //         iterationsPerSecText.text = iterationsPerSec.ToString("F0") + "it/s";
    //     }
    // }

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
        Star newMergedObject = new Star("Merged Star", newMass, newTemperature, Utility.GetStarColor(newTemperature));
        string type = isBlackHole ? "Black Hole" : newMergedObject.Type;
        newMergedObject.color = isBlackHole ? Color.black : Utility.GetStarColor(newTemperature);

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            GameObject newParticleObject = CreateParticle(newSize, newMergedObject.color, newPosition.x, newPosition.y, newPosition.z, isBlackHole);
            ParticleEntity mergedParticle = new ParticleEntity(newSize, newVelocity, newMass, newTemperature, type, newParticleObject, newMergedObject.color)
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

    public void ClickButtonsAddCluster(int type)
    {
        float zPositionConstant = 50;
        switch (type)
        {
            case 1:
                Vector3 currentPosition = Camera.main.transform.position;
                currentPosition.z += zPositionConstant;
                Scene currentScene = SceneManager.GetActiveScene();
                CreateCluster(currentScene, currentPosition);
                break;
            case 2:
                Scene currentScene2 = SceneManager.GetActiveScene();
                Vector3 currentPosition2 = Camera.main.transform.position;
                currentPosition2.z += zPositionConstant;
                CreateCluster(currentScene2, currentPosition2, 100);
                break;
            case 3:
                Scene currentScene3 = SceneManager.GetActiveScene();
                Vector3 currentPosition3 = Camera.main.transform.position;
                currentPosition3.z += zPositionConstant;
                CreateCluster(currentScene3, currentPosition3, 200);
                break;
            case 4:
                Scene currentScene4 = SceneManager.GetActiveScene();
                Vector3 currentPosition4 = Camera.main.transform.position;
                currentPosition4.z += zPositionConstant;
                CreateCluster(currentScene4, currentPosition4, 400);
                break;
            case 5:
                Scene currentScene5 = SceneManager.GetActiveScene();
                Vector3 currentPosition5 = Camera.main.transform.position;
                currentPosition5.z += zPositionConstant;
                CreateCluster(currentScene5, currentPosition5, 1000);
                break;
            case 6:
                Scene currentScene6 = SceneManager.GetActiveScene();
                Vector3 currentPosition6 = Camera.main.transform.position;
                currentPosition6.z += zPositionConstant;
                CreateCluster(currentScene6, currentPosition6, 10000);
                break;
        }
        isGalaxy = false;
        galaxyModePanel.SetActive(isGalaxy);
    }
}
