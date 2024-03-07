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
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;

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
    private bool showMass = false;
    public bool isMainMenu = false;
    public bool showRedshift;
    private bool startCameraRotation;
    private bool showHUD = true;
    public GameObject galaxyModePanel;
    public GameObject bigBangModePanel;
    bool propertyChanged = false;
    private SimulationMode simulationMode = SimulationMode.Random;
    public ComputeShader computeShader;
    ComputeBuffer particleBuffer;
    ParticleGPU[] particlesGPU;

    private void CreateCluster(Scene currentScene, Vector3 position, int count = 20)
    {
        System.Random random = new();
        Vector3 massCenter = position;
        // Create a cluster of particles
        for (int i = 0; i < count; i++)
        {
            double roll = random.NextDouble() * 100;
            Star star = Utility.GenerateStars(roll);
            Vector3 newPosition = Vector3.zero;
            Vector3 velocity = Vector3.zero;

            switch(simulationMode)
            {
                case SimulationMode.Galaxy:
                    float innerRadius = 10f; // Inner radius of the ring
                    float outerRadius = 100f; // Outer radius of the ring
                    float angle = i * 2.0f * Mathf.PI / count; // Distribute particles evenly around the circle

                    // Randomize radius within the ring bounds
                    float radius = Random.Range(innerRadius, outerRadius);
                    // Calculate x, y, z coordinates for a ring-shaped galaxy
                    float x = position.x + radius * Mathf.Cos(angle);
                    float y = position.y + radius * Mathf.Sin(angle);
                    float z = position.z + Random.Range(-5, 5);
                    newPosition = new Vector3(x, y, z);
                    // make the stars rotate around the center
                    Vector3 direction = massCenter - newPosition;
                    direction.z = 10;
                    // velocity = Vector3.Cross(direction, Vector3.forward);
                break;
                case SimulationMode.Random:
                    // Randomize position within a rectangular area
                    float multiplier = Mathf.Max(20, count / 1000); // Multiplier to increase the rectangular area
                    float minX = position.x - multiplier; // Minimum x coordinate of the rectangular area
                    float maxX = position.x + multiplier; // Maximum x coordinate of the rectangular area
                    float minY = position.y - multiplier; // Minimum y coordinate of the rectangular area
                    float maxY = position.y + multiplier; // Maximum y coordinate of the rectangular area
                    float minZ = position.z - multiplier; // Minimum z coordinate of the rectangular area
                    float maxZ = position.z + multiplier; // Maximum z coordinate of the rectangular area

                    x = Random.Range(minX, maxX);
                    y = Random.Range(minY, maxY);
                    z = Random.Range(minZ, maxZ);
                    newPosition = new Vector3(x, y, z);
                    // Randomize velocity
                    velocity.x = Random.Range(-_starVelocity, _starVelocity);
                    velocity.y = Random.Range(-_starVelocity, _starVelocity);
                    velocity.z = Random.Range(-_starVelocity, _starVelocity);
                break;
                case SimulationMode.BigBang:
                    newPosition = position + Random.insideUnitSphere * 10;
                break;
            }
            particles.Add(AddParticle(star, currentScene, newPosition, velocity));
        }
    }

    private int GetUniverseSize()
    {
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = SystemInfo.processorCount
        };
        int universeSize = 0;
        Parallel.ForEach(particles, parallelOptions, particle =>
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
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Vector3 massCenterForSim = GetMassCenter();
        int universeSize = GetUniverseSize();
        octree = new OctreeNode(massCenterForSim, universeSize);
        foreach (ParticleEntity particle in particles)
        {
            octree.AddParticle(particle);
        }
        stopwatch.Stop();
        Debug.Log("Octree creation time: " + stopwatch.ElapsedMilliseconds + "ms");
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
        GameObject particle = new GameObject();

        particle.transform.localScale = size;

        if (x != null && y != null && z != null)
        {
            particle.transform.position = new Vector3((float)x, (float)y, (float)z);
        }
        else
        {
            particle.transform.position = Random.insideUnitSphere * 20;
        }

        // Color particleColor = !isBlackHole ? color : Color.black;
        // // Set the color
        // Renderer particleRenderer = particle.GetComponent<Renderer>();
        // if (!isBlackHole && isStellar)
        // {
        //     particleRenderer.material.color = particleColor; // Apply color based on mass
        //     particleRenderer.material.EnableKeyword("_EMISSION"); // Enable emission
        //     particleRenderer.material.SetColor("_EmissionColor", particleColor); // Set emission color
        // }
        // else if (!isStellar)
        // {
        //     particleRenderer.material.DisableKeyword("_EMISSION");
        //     particleRenderer.material.color = particleColor;
        // }

        // if (isBlackHole)
        // {
        //     particleRenderer.material.color = particleColor;
        //     particleRenderer.material.EnableKeyword("_EMISSION");
        //     particleRenderer.material.SetColor("_EmissionColor", particleColor);
        // }

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

        // SceneManager.MoveGameObjectToScene(particleObject, currentScene);
        return new(_particleSize, velocity ?? Vector3.zero, star.Mass, star.Temperature, star.Type, particleObject, star.color);
    }

    // Start is called before the first frame update
    private void Start()
    {
        Utility.computeShader = computeShader;
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
            GameObject gameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g == lockedParticle.particleObject);
            Camera.main.transform.LookAt(gameObject.transform);
        }

        _deltaTime = Time.deltaTime / 50;

        if (runSimulation)
        {
            // if(particles.Count > 0)
            // {
            //     CreateOctree();
            // }
            // SimulateBarnesHut();
            SimulateGPU();
            yearPassed += 1;
        }
        else
        {
            iterationsPerSecText.text = "Simulation paused";
        }

        int particleCount = particles.Count;
        if(particleCount > 0)
        {
            UpdateParticlesPositions(particleCount);
        }

        particlesCountText.text = "Objects: " + particles.Count.ToString();

        if (Input.GetKeyDown(KeyCode.O))
        {
            showOrbitLines = !showOrbitLines;
            showOrbitLinesText.color = showOrbitLines ? Color.green : Color.white;
            propertyChanged = true;
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

        // Reset the simulation
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
            showBloomText.color = showBloom ? Color.green : Color.white;
            propertyChanged = true;
        }

        // Big Bang mode
        if (Input.GetKeyDown(KeyCode.N))
        {
            if(simulationMode == SimulationMode.BigBang)
            {
                simulationMode = SimulationMode.Random;
            }
            else
            {
                simulationMode = SimulationMode.BigBang;
            }
            galaxyModePanel.SetActive(simulationMode == SimulationMode.Galaxy);
            bigBangModePanel.SetActive(simulationMode == SimulationMode.BigBang);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            showKineticEnergy = !showKineticEnergy;
            showKineticEnergyText.color = showKineticEnergy ? Color.green : Color.white;
            propertyChanged = true;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            showHUD = !showHUD;
            hud.SetActive(!hud.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            showVelocityColor = !showVelocityColor;
            showVelocityColorText.color = showVelocityColor ? Color.green : Color.white;
            propertyChanged = true;
        }

        if(Input.GetKeyDown(KeyCode.M))
        {
            showMass = !showMass;
            propertyChanged = true;
        }

        if(Input.GetKeyDown(KeyCode.G)){
            if(simulationMode == SimulationMode.Galaxy)
            {
                simulationMode = SimulationMode.Random;
            }
            else
            {
                simulationMode = SimulationMode.Galaxy;
            }
            galaxyModePanel.SetActive(simulationMode == SimulationMode.Galaxy);
            bigBangModePanel.SetActive(simulationMode == SimulationMode.BigBang);
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
    
        // keep camera looking at the center of the universe
        // if (particles.Count > 0)
        // {
        //     Vector3 universeCenter = GetMassCenter();
        //     Camera.main.transform.LookAt(universeCenter);
        // }
    }

    private void RotateTheCameraAround(Vector3 position, float speed)
    {
        Camera.main.transform.RotateAround(position, Vector3.up, speed * Time.deltaTime);
    }
    private float CalculateKineticEnergy(ParticleEntity particle)
    {
        return 0.5f * particle.mass * particle.velocity.sqrMagnitude;
    }

    // Simulate gravity using the Barnes-Hut algorithm (O(n log n) complexity)
    void SimulateBarnesHut()
    {
        Stopwatch stopwatch = new();
        try
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = SystemInfo.processorCount
            };
            stopwatch.Start();
            Parallel.ForEach(particles, parallelOptions, particle =>
            {
                particle.acceleration = octree.CalculateForceBarnesHut(particle, octree, 1.5f);
                particle.velocity += particle.acceleration * _deltaTime;
                particle.acceleration = Vector3.zero;
            });

            stopwatch.Stop();
            Debug.Log("Barnes-Hut simulation time: " + stopwatch.ElapsedMilliseconds + "ms");

            for (int i = 0; i < particles.Count; i++)
            {
                ParticleEntity particle = particles[i];
                particle.SetPosition(particle.position + particle.velocity * _deltaTime);
                // Update the color of the particle based on its properties only when a property is changed
                UpdateParticleColor(particle, propertyChanged);
            }
            UpdatePerformanceMetrics();
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
    }

    public struct ParticleGPU
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
    }
    
    void RemoveGameObjects()
    {
        foreach (ParticleEntity particle in particles)
        {
            if (particle != null && particle.particleObject != null && !particle.particleObject.IsDestroyed())
                Destroy(particle.particleObject);
        }
    }

    void SimulateGPU()
    {
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = SystemInfo.processorCount
        };
        int kernelHandle = computeShader.FindKernel("NBodySimulation");
        int particleCount = particles.Count;

        // Calculate the stride size manually
        int strideSize = sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float);

        // Create the ComputeBuffer with the optimized stride size
        particleBuffer = new ComputeBuffer(particleCount, strideSize);

        // Create a temporary array to hold the particle data
        particlesGPU = new ParticleGPU[particleCount];

        Parallel.For(0, particleCount, parallelOptions, i =>
        {
            particlesGPU[i] = new ParticleGPU
            {
                position = particles[i].position,
                velocity = particles[i].velocity,
                mass = particles[i].mass
            };
        });

        // Set the particle data in the ComputeBuffer
        particleBuffer.SetData(particlesGPU);

        // Set the ComputeBuffer and other parameters for the compute shader
        computeShader.SetBuffer(kernelHandle, "particles", particleBuffer);
        computeShader.SetInt("particlesCount", particleCount);
        computeShader.SetFloat("deltaTime", _deltaTime);

        // Dispatch the compute shader with a smaller thread group size
        int threadGroupSize = Mathf.CeilToInt((float)particleCount / 64);
        computeShader.Dispatch(kernelHandle, threadGroupSize, 1, 1);

        // Get the updated particle data from the ComputeBuffer
        particleBuffer.GetData(particlesGPU);

        // Release the ComputeBuffer
        particleBuffer.Release();

        // Update the particles with the updated data
        Parallel.For(0, particleCount, parallelOptions, i =>
        {
            particles[i].velocity = particlesGPU[i].velocity;
            particles[i].acceleration = Vector3.zero;
            particles[i].position = particlesGPU[i].position;
        });

        UpdatePerformanceMetrics();
    }

    private void UpdateParticlesPositions(int particleCount)
    {
        Matrix4x4[] matrices = new Matrix4x4[particleCount];
        // Update the particles with the updated data
        var particleMesh = particlePrefab.GetComponent<MeshFilter>().sharedMesh;
        var particleMaterial = particlePrefab.GetComponent<MeshRenderer>().sharedMaterial;
        RenderParams rp = new(particleMaterial){
            layer = particlePrefab.layer
        };

        for (int i = 0; i < particleCount; i++)
        {
            matrices[i] = Matrix4x4.TRS(particles[i].position, Quaternion.identity, particles[i].size);
        }
        Graphics.RenderMeshInstanced(rp, particleMesh, 0, matrices, particleCount);
    }

    private void UpdateParticleColor(ParticleEntity particle, bool hasChanged)
    {
        Renderer particleRenderer = particle.particleObject.GetComponent<Renderer>();
        if(particleRenderer == null || !hasChanged)
        {
            return;
        }
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
        else if (!showBloom)
        {
            particle.particleObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
            particle.particleObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
        }
        else if (showBloom)
        {
            particle.particleObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            particle.particleObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", particle.color);
        }
        else
        {
            particleRenderer.material.color = Utility.GetStarColor(particle.temperature);
            if (!particleRenderer.material.IsKeywordEnabled("_EMISSION"))
                particleRenderer.material.EnableKeyword("_EMISSION");
        }
    }

    void UpdatePerformanceMetrics()
    {
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

    public void ClickButtonsAddCluster(int count)
    {
        float zPositionConstant = 50;
        Vector3 currentPosition = Camera.main.transform.position;
        currentPosition.z += zPositionConstant;
        Scene currentScene = SceneManager.GetActiveScene();
        CreateCluster(currentScene, currentPosition, count);
        particlesGPU = new ParticleGPU[particles.Count];
        simulationMode = SimulationMode.Random;
        galaxyModePanel.SetActive(false);
        bigBangModePanel.SetActive(false);
    }
}
