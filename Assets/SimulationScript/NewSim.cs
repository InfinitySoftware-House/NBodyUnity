using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class NewSim : MonoBehaviour
{
    public ComputeShader computeShader;
    public GameObject particlePrefab;
    public int particleCount = 10000;
    public float softening = 0.01f;

    private int barnesHutKernel;
    private int updateParticlesKernel;
    private ComputeBuffer particlesBuffer;
    private ComputeBuffer nodesBuffer;

    private Particle[] particles;
    private Node[] nodes;

    private float gravitationalConstant = 0.000000000667f;
    private float deltaTime = 0.01f;
    private Vector3 spaceOrigin = Vector3.zero;
    private Vector3 spaceSize = new Vector3(100f, 100f, 100f);
    private int MAX_NODES = 8;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
    }

    struct Node
    {
        public Vector3 centerOfMass;
        public float totalMass;
        public Vector3 minBounds;
        public Vector3 maxBounds;
        public int firstChild;
        public int nextNode;
        public bool isLeaf;
    }

    void Start()
    {
        InitializeParticles();
        ConstructOctree();
        InitializeComputeShader();
    }

    void Update()
    {
        SimulateParticles();
        RenderParticles();
    }

    void InitializeParticles()
    {
        particles = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            particles[i].position = new Vector3(Random.Range(-spaceSize.x / 2f, spaceSize.x / 2f),
                                                 Random.Range(-spaceSize.y / 2f, spaceSize.y / 2f),
                                                 Random.Range(-spaceSize.z / 2f, spaceSize.z / 2f)) + spaceOrigin;
            particles[i].velocity = Vector3.zero;
            particles[i].mass = Random.Range(1f, 10f);
        }

        particlesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 7);
        particlesBuffer.SetData(particles);
    }

    void InitializeComputeShader()
    {
        barnesHutKernel = computeShader.FindKernel("BarnesHutKernel");
        updateParticlesKernel = computeShader.FindKernel("UpdateParticlesKernel");

        computeShader.SetBuffer(barnesHutKernel, "particlesBuffer", particlesBuffer);
        computeShader.SetBuffer(barnesHutKernel, "nodesBuffer", nodesBuffer);
        computeShader.SetFloat("deltaTime", deltaTime);
        computeShader.SetFloat("gravitationalConstant", gravitationalConstant);
        computeShader.SetFloat("softening", softening);
        computeShader.SetVector("spaceOrigin", spaceOrigin);
        computeShader.SetVector("spaceSize", spaceSize);
    }

    void ConstructOctree()
    {
        nodes = new Node[MAX_NODES];
        int rootNodeIndex = 0;

        // Initialize the root node
        nodes[rootNodeIndex].minBounds = spaceOrigin;
        nodes[rootNodeIndex].maxBounds = spaceOrigin + spaceSize;
        nodes[rootNodeIndex].firstChild = -1; // No children initially
        nodes[rootNodeIndex].nextNode = -1; // No next node initially
        nodes[rootNodeIndex].isLeaf = true;

        // Recursively insert particles into the octree
        int nextFreeNode = 1; // Start from 1 because 0 is the root node
        for (int i = 0; i < particleCount; i++)
        {
            InsertParticleIntoOctree(rootNodeIndex, particles[i], ref nextFreeNode);
        }

        // Create the nodes buffer and upload the octree data
        nodesBuffer = new ComputeBuffer(nodes.Length, sizeof(float) * 10 + sizeof(int) * 2 + sizeof(int));
        nodesBuffer.SetData(nodes);

        computeShader.SetBuffer(barnesHutKernel, "nodesBuffer", nodesBuffer);
    }

    void InsertParticleIntoOctree(int nodeIndex, Particle particle, ref int nextFreeNode)
    {
        if (nodeIndex < 0 || nodeIndex >= MAX_NODES) return; // Prevent out-of-bounds access

        Node node = nodes[nodeIndex];

        // Base case: If the node is a leaf but doesn't contain a particle, insert the particle here.
        if (node.firstChild == -1 && node.isLeaf)
        {
            // We assume you handle the particle storage in your node
            // Update the node as necessary, e.g., updating center of mass, storing particle data, etc.
            node.isLeaf = false; // Now contains a particle
            nodes[nodeIndex] = node; // Save changes to the node
            return;
        }

        // If the node is a leaf and already contains a particle, it should be subdivided and the particles distributed.
        // This condition was missing in your original implementation.
        if (node.firstChild == -1 && !node.isLeaf)
        {
            SubdivideNode(nodeIndex, ref nextFreeNode);
            // After subdivision, we need to re-insert the original particle and the new one into the correct children.
            // This involves a logic similar to below but for the original particle stored in this node.
        }

        // Find the appropriate child index and recursively insert.
        int childIndex = FindChildIndexForParticle(particle, node); // You should define this method based on your child node logic.
        if (node.firstChild >= 0 && node.firstChild + childIndex < MAX_NODES)
        {
            InsertParticleIntoOctree(node.firstChild + childIndex, particle, ref nextFreeNode);
        }
    }

    int FindChildIndexForParticle(Particle particle, Node node)
    {
        int index = 0;

        // Calculate the center point of the node
        Vector3 center = (node.minBounds + node.maxBounds) * 0.5f;

        // Determine the octant the particle belongs to
        if (particle.position.x >= center.x) index |= 1; // Bit 0
        if (particle.position.y >= center.y) index |= 2; // Bit 1
        if (particle.position.z >= center.z) index |= 4; // Bit 2

        return index;
    }


    void SubdivideNode(int nodeIndex, ref int nextFreeNode)
    {
        if (nextFreeNode + 8 > MAX_NODES) return; // Ensure there's enough space for new nodes

        Node node = nodes[nodeIndex];
        Vector3 childSize = (node.maxBounds - node.minBounds) / 2f;

        // Indicate that the current node is no longer a leaf after subdivision
        nodes[nodeIndex].isLeaf = false;

        // Create the child nodes
        nodes[nodeIndex].firstChild = nextFreeNode;
        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = new Vector3(
                (i & 1) * childSize.x,
                (i & 2) * childSize.y,
                (i & 4) * childSize.z
            );

            nodes[nextFreeNode].minBounds = node.minBounds + offset;
            nodes[nextFreeNode].maxBounds = nodes[nextFreeNode].minBounds + childSize;
            nodes[nextFreeNode].firstChild = -1; // New child nodes initially have no children
            nodes[nextFreeNode].nextNode = (i < 7) ? nextFreeNode + 1 : -1; // Link to the next node or end the list
            nodes[nextFreeNode].isLeaf = true; // New child nodes start as leaves

            nextFreeNode++;
        }
    }

    void SimulateParticles()
    {
        computeShader.Dispatch(barnesHutKernel, Mathf.CeilToInt(particleCount / 256f), 1, 1);
        computeShader.Dispatch(updateParticlesKernel, Mathf.CeilToInt(particleCount / 256f), 1, 1);
    }

    void RenderParticles()
    {
        particlesBuffer.GetData(particles);
        Mesh particleMesh = particlePrefab.GetComponent<MeshFilter>().sharedMesh;
        Material particleMaterial = particlePrefab.GetComponent<MeshRenderer>().sharedMaterial;
        Matrix4x4[] matrices = new Matrix4x4[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            // Render a sphere or sprite at the particle's position
            // You can use Unity's built-in rendering or a custom rendering method

            // Example: render a sphere
            matrices[i] = Matrix4x4.TRS(particles[i].position, Quaternion.identity, Vector3.one * 0.1f);
        }

        Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matrices, particleCount);
    }

    void OnDestroy()
    {
        particlesBuffer.Release();
        nodesBuffer.Release();
    }
}