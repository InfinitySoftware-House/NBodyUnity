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

public class Simulation : MonoBehaviour
{
    public GameObject particlePrefab;
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
    private Vector3 _particleSize = new(0.2f, 0.2f, 0.2f);
    public TMP_Text showBloomText;
    public TMP_Text showOrbitLinesText;
    public TMP_Text showKineticEnergyText;
    public TMP_Text showVelocityColorText;
    public OctreeNode octree { get; private set; }
    private bool runSimulation = false;
    private bool showMass = false;
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
    Mesh pointMesh;
    Material particleMaterial;
    // Added caching field for transformation matrices
    private Matrix4x4[] matricesCache;

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

    // Replaced Parallel.ForEach with a simple for-loop for determinism and lower overhead.
    private int GetUniverseSize()
    {
        int universeSize = 0;
        for (int i = 0; i < particles.Count; i++)
        {
            int dist = (int)Vector3.Distance(particles[i].position, Vector3.zero);
            if (dist > universeSize)
                universeSize = dist;
        }
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

    // Removed per-particle GameObject creation to avoid spawning N objects.

    private bool CheckCollision(ParticleEntity body1, ParticleEntity body2)
    {
        float radiusSum = body1.size.magnitude + body2.size.magnitude;
        float distance = Vector3.Distance(body1.position, body2.position);
        return distance < radiusSum / 2;
    }

    private ParticleEntity AddParticle(Star star, Scene currentScene, Vector3? position = null, Vector3? velocity = null, float mass = 0.5f)
    {
        bool isBlackHole = mass >= 1000;
        Vector3 spawnPosition = position ?? (Vector3)(Random.insideUnitSphere * 20);
        ParticleEntity particleEntity = new(_particleSize, velocity ?? Vector3.zero, star.Mass, star.Temperature, star.Type, spawnPosition, star.color)
        {
            isBlackHole = isBlackHole
        };
        return particleEntity;
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

        pointMesh = particlePrefab.GetComponent<MeshFilter>().sharedMesh;
        particleMaterial = particlePrefab.GetComponent<MeshRenderer>().sharedMaterial;
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

    #region Input Handling
    private void ProcessInput() {
        if (Input.GetKeyDown(KeyCode.H)) {
            showRedshift = !showRedshift;
            Debug.Log("Redshift: " + (showRedshift ? "ON" : "OFF"));
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            runSimulation = !runSimulation;
        }
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            // Screen-space picking without per-particle GameObjects
            float pickRadius = 12f; // pixels
            float bestDist = float.MaxValue;
            ParticleEntity best = null;
            for (int i = 0; i < particles.Count; i++) {
                Vector3 worldPos = (particlesGPU != null && particlesGPU.Length == particles.Count) ? particlesGPU[i].position : particles[i].position;
                Vector3 sp = Camera.main.WorldToScreenPoint(worldPos);
                if (sp.z < 0) continue; // behind camera
                float dist = Vector2.Distance(new Vector2(sp.x, sp.y), (Vector2)Input.mousePosition);
                if (dist < pickRadius && dist < bestDist) {
                    bestDist = dist;
                    best = particles[i];
                }
            }
            if (best != null) {
                lockedParticle = best;
                ObjectInfoModel objectInfoModel = GetObjectInfoModel(best);
                objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
                objectInfoObject.SetActive(true);
            } else {
                lockedParticle = null;
                objectInfoObject.SetActive(false);
            }
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            showOrbitLines = !showOrbitLines;
            showOrbitLinesText.color = showOrbitLines ? Color.green : Color.white;
            propertyChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.Period)) {
            if (_starVelocity < 20)
                _starVelocity += 1;
        }
        if (Input.GetKeyDown(KeyCode.Comma)) {
            if (_starVelocity > 1)
                _starVelocity -= 1;
        }
        if (Input.GetKeyDown(KeyCode.R)) {
            particles = new List<ParticleEntity>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Input.GetKeyDown(KeyCode.B)) {
            showBloom = !showBloom;
            showBloomText.color = showBloom ? Color.green : Color.white;
            propertyChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.N)) {
            simulationMode = simulationMode == SimulationMode.BigBang ? SimulationMode.Random : SimulationMode.BigBang;
            galaxyModePanel.SetActive(simulationMode == SimulationMode.Galaxy);
            bigBangModePanel.SetActive(simulationMode == SimulationMode.BigBang);
        }
        if (Input.GetKeyDown(KeyCode.K)) {
            showKineticEnergy = !showKineticEnergy;
            showKineticEnergyText.color = showKineticEnergy ? Color.green : Color.white;
            propertyChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            showHUD = !showHUD;
            hud.SetActive(!hud.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.V)) {
            showVelocityColor = !showVelocityColor;
            showVelocityColorText.color = showVelocityColor ? Color.green : Color.white;
            propertyChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.M)) {
            showMass = !showMass;
            propertyChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.G)) {
            simulationMode = simulationMode == SimulationMode.Galaxy ? SimulationMode.Random : SimulationMode.Galaxy;
            galaxyModePanel.SetActive(simulationMode == SimulationMode.Galaxy);
            bigBangModePanel.SetActive(simulationMode == SimulationMode.BigBang);
        }
        if (Input.GetKeyDown(KeyCode.P)) {
            startCameraRotation = !startCameraRotation;
        }
    }
    #endregion

    #region Simulation Update
    private void UpdateSimulationLogic() {
        _deltaTime = Time.deltaTime / 50;
        if (runSimulation) {
            if (particlesGPU != null && particlesGPU.Length > 0) {
                SimulateGPU();
            }
            yearPassed += 1;
        } else {
            iterationsPerSecText.text = "Simulation paused";
        }
        int particleCount = particles.Count;
        if(particleCount > 0) {
            UpdateParticlesPositions(particleCount);
        }
        particlesCountText.text = "Objects: " + particles.Count.ToString();
        
        if(startCameraRotation && particles.Count > 0){
            Vector3 universeCenter = GetMassCenter();
            RotateTheCameraAround(universeCenter, 14);
        }
    }
    #endregion

    #region Locked Particle Update
    private void UpdateLockedParticle() {
        if (lockedParticle != null) {
            ObjectInfoModel objectInfoModel = GetObjectInfoModel(lockedParticle);
            objectInfoObject.GetComponent<ObjectInfo>().ShowInfo(objectInfoModel);
            Camera.main.transform.LookAt(lockedParticle.position);
        }
    }
    #endregion

    // Modify Update method to use the extracted methods
    private void Update()
    {
        yearPassedText.text = yearPassed.ToString() + " Y";
        iterationsPerSecText.color = runSimulation ? Color.white : Color.red;
        
        ProcessInput();
        UpdateLockedParticle();
        UpdateSimulationLogic();
    }

    private void RotateTheCameraAround(Vector3 position, float speed)
    {
        Camera.main.transform.RotateAround(position, Vector3.up, speed * Time.deltaTime);
    }

    public struct ParticleGPU
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
    }

    private const int ThreadGroupSize = 256;
    private const int StrideSize = sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float);
    private int kernelHandle;

    private void InitializeComputeShader()
    {
        kernelHandle = computeShader.FindKernel("NBodySimulation");
        particleBuffer = new ComputeBuffer(particles.Count, StrideSize);
    }

    void SimulateGPU()
    {
        particleBuffer.SetData(particlesGPU);
        computeShader.SetBuffer(kernelHandle, "particles", particleBuffer);
        computeShader.SetInt("particlesCount", particles.Count);
        computeShader.SetFloat("deltaTime", _deltaTime);

        // Use CeilToInt to cover cases where particles.Count is not a multiple of ThreadGroupSize.
        int groups = Mathf.CeilToInt((float)particles.Count / ThreadGroupSize);
        computeShader.Dispatch(kernelHandle, groups, 1, 1);

        particleBuffer.GetData(particlesGPU);

        UpdatePerformanceMetrics();
    }

    void CleanupComputeShader()
    {
        if(particleBuffer != null){
            particleBuffer.Release();
        }
    }

    // Reuse a cached matrices array to avoid per-frame allocations
    private void UpdateParticlesPositions(int particleCount)
    {
        if (matricesCache == null || matricesCache.Length != particleCount)
        {
            matricesCache = new Matrix4x4[particleCount];
        }
        RenderParams rp = new RenderParams(particleMaterial)
        {
            layer = particlePrefab.layer
        };

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = particlesGPU[i].position;
            matricesCache[i] = Matrix4x4.TRS(pos, Quaternion.identity, _particleSize);
            // Keep CPU-side particle positions in sync for UI and picking
            particles[i].position = pos;
        }
        Graphics.RenderMeshInstanced(rp, pointMesh, 0, matricesCache, particleCount);
    }

    private void UpdateParticleColor(ParticleEntity particle, bool hasChanged)
    {
        // No-op: per-instance colors are not applied without per-instance properties.
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
            ParticleEntity mergedParticle = new ParticleEntity(newSize, newVelocity, newMass, newTemperature, type, newPosition, newMergedObject.color)
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
        });
    }

    private void OnApplicationQuit()
    {
        CleanupComputeShader();

        particles = null;
    }

    public void ClickButtonsAddCluster(int count)
    {
        int countParticles = (int)Math.Pow(2, count);
        float zPositionConstant = 50;
        Vector3 currentPosition = Camera.main.transform.position;
        currentPosition.z += zPositionConstant;
        Scene currentScene = SceneManager.GetActiveScene();
        CreateCluster(currentScene, currentPosition, countParticles);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = SystemInfo.processorCount
        };

        particlesGPU = new ParticleGPU[countParticles];

        Parallel.ForEach(particles, parallelOptions, (particle, state, i) =>
        {
            particlesGPU[i] = new ParticleGPU
            {
                position = particle.position,
                velocity = particle.velocity,
                mass = particle.mass
            };
        });

        simulationMode = SimulationMode.Random;
        galaxyModePanel.SetActive(false);
        bigBangModePanel.SetActive(false);

        InitializeComputeShader();
    }
}
