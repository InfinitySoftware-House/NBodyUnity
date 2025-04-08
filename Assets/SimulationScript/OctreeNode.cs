using System.Collections.Generic;
using Vector3 = UnityEngine.Vector3;

public struct Particle
{
    public Vector3 acceleration;
    public Vector3 velocity;
    public Vector3 position;
    public float mass;
}

public class OctreeNode {
    public Vector3 Center { get; private set; } // Center of the cell
    public float Size { get; private set; } // Length of the cell's side
    public Vector3 CenterOfMass { get; private set; } // Center of mass of the particles within the cell
    public float TotalMass { get; private set; } // Total mass of the particles within the cell
    public OctreeNode[] Children { get; private set; } // Children of this node in the octree
    public List<ParticleEntity> Particles { get; private set; } // Particles inside this node
    private const float SofteningSquared = 0.001f; // Softening to avoid infinite forces
    private const int MaxParticlesPerNode = 120; // Maximum number of particles per node

    // Constructor
    public OctreeNode(Vector3 center, float size) {
        Center = center;
        Size = size;
        CenterOfMass = Vector3.zero;
        TotalMass = 0f;
        Children = new OctreeNode[8];
        Particles = new List<ParticleEntity>();
    }

    // Method to add a particle to this node (or its children)
    public void AddParticle(ParticleEntity particle) {
        if (Children[0] != null) {
            int index = GetChildIndexForParticle(particle.position);
            Children[index].AddParticle(particle);
        } else {
            Particles.Add(particle);
            if (Particles.Count > MaxParticlesPerNode) {
                Subdivide();
                RedistributeParticles();
                Particles.Clear(); // Clear the particles list from the current node after redistribution
            }
        }
        UpdateMassDistribution(particle); // Update the mass distribution whenever a particle is added
    }

    // Method for subdividing this node by creating eight new children
    private void Subdivide() {
        Vector3 halfSize = new Vector3(Size / 8, Size / 8, Size / 8); // Half of halfSize for correct child sizing
        for (int i = 0; i < 8; i++) {
            Vector3 childCenter = Center + new Vector3(
                i % 2 == 0 ? -halfSize.x : halfSize.x,
                i / 4 == 0 ? -halfSize.y : halfSize.y,
                i / 2 % 2 == 0 ? -halfSize.z : halfSize.z);
            Children[i] = new OctreeNode(childCenter, Size / 2);
        }
    }

    // Redistribute existing particles into new children after subdivision
    private void RedistributeParticles() {
        foreach (var particle in Particles) {
            int index = GetChildIndexForParticle(particle.position);
            Children[index].AddParticle(particle);
        }
    }

    // Method to determine in which child a particle should go based on its position
    private int GetChildIndexForParticle(Vector3 position) {
        return (position.x >= Center.x ? 1 : 0) +
               (position.y >= Center.y ? 4 : 0) +
               (position.z >= Center.z ? 2 : 0);
    }

    // Method to update the total mass and the center of mass of the node
    private void UpdateMassDistribution(ParticleEntity particle) {
        TotalMass += particle.mass;
        CenterOfMass = (CenterOfMass * (TotalMass - particle.mass) + particle.position * particle.mass) / TotalMass;
    }

    public Vector3 CalculateForceBarnesHut(ParticleEntity particle, OctreeNode node, float theta)
    {
        if (node == null || particle == null) return Vector3.zero;

        Vector3 particlePosition = particle.position;
        float particleMass = particle.mass;
        Vector3 nodeCenterOfMass = node.CenterOfMass;
        float softeningSquared = SofteningSquared;
        Vector3 force = Vector3.zero;

        if (node.Particles.Count == 1 && node.Particles[0] != particle)
        {
            Vector3 diff = node.Particles[0].position - particlePosition;
            float distanceSquared = diff.sqrMagnitude + softeningSquared;
            force = diff.normalized * (Utility.G * particleMass * node.Particles[0].mass / distanceSquared);
        }
        else
        {
            Vector3 diff = nodeCenterOfMass - particlePosition;
            float distSqr = diff.sqrMagnitude;
            // Replace expensive Vector3.Distance by comparing squared values
            if (node.Size * node.Size < theta * theta * distSqr)
            {
                float distanceSquared = distSqr + softeningSquared;
                force = diff.normalized * (Utility.G * particleMass * node.TotalMass / distanceSquared);
            }
            else
            {
                foreach (var childNode in node.Children)
                {
                    force += CalculateForceBarnesHut(particle, childNode, theta);
                }
            }
        }

        return force;
    }
}