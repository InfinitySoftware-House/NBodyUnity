using System.Collections.Generic;
using Vector3 = UnityEngine.Vector3;

public class OctreeNode
{
    public Vector3 center; // Centro della cella
    public float size; // Lunghezza del lato della cella
    public Vector3 centerOfMass; // Centro di massa delle particelle all'interno della cella
    public float totalMass; // Massa totale delle particelle all'interno della cella
    public OctreeNode[] children; // Figli di questo nodo nell'octree
    public List<ParticleEntity> particles; // Particelle all'interno di questo nodo
    private readonly float softeningSquared = 0.001f; // Softening per evitare forze infinite
    private readonly int maxParticlesPerNode = 6; // Numero massimo di particelle per nodo
    private bool populated = false; // Flag per indicare se il nodo è stato popolato

    // Costruttore
    public OctreeNode(Vector3 center, float size)
    {
        this.center = center;
        this.size = size;
        centerOfMass = Vector3.zero;
        totalMass = 0f;
        children = new OctreeNode[8];
        particles = new List<ParticleEntity>();
    }

    // Metodo per aggiungere una particella a questo nodo (o ai suoi figli)
    public void AddParticle(ParticleEntity particle)
    {
        // Se il nodo ha figli, aggiungi la particella a uno dei figli
        if (children[0] != null)
        {
            int index = GetChildIndexForParticle(particle.position);
            children[index].AddParticle(particle);
            children[index].populated = true;
        }
        else
        {
            // Altrimenti, aggiungi la particella a questo nodo
            particles.Add(particle);
            populated = true;

            // Se dopo l'aggiunta la cella supera un certo limite di particelle,
            // dividila creando nuovi nodi figli e redistribuendo le particelle
            if (particles.Count > maxParticlesPerNode) // Soglia arbitraria
            {
                Subdivide();
                // Dopo la suddivisione, riposiziona le particelle esistenti nei nuovi figli
                foreach (var existingParticle in particles)
                {
                    int index = GetChildIndexForParticle(existingParticle.position);
                    children[index].AddParticle(existingParticle);
                    children[index].populated = true;
                }
                // Pulisci la lista delle particelle dal nodo corrente
                particles.Clear();
            }
            else if (particles.Count == 0)
            {
                populated = false;
            }
        }

        // Aggiorna il centro di massa e la massa totale
        UpdateMassDistribution(particle);
    }

    // Metodo per suddividere questo nodo creando otto nuovi figli
    private void Subdivide()
    {
        Vector3 halfSize = new Vector3(size / 8, size / 8, size / 8);
        for (int i = 0; i < 8; i++)
        {
            Vector3 childCenter = center + new Vector3(
                (i % 2 == 0 ? -1 : 1) * halfSize.x,
                (i / 4 == 0 ? -1 : 1) * halfSize.y,
                (i / 2 % 2 == 0 ? -1 : 1) * halfSize.z);
            children[i] = new OctreeNode(childCenter, size / 2);
        }
    }

    // Metodo per determinare in quale figlio dovrebbe andare una particella data la sua posizione
    private int GetChildIndexForParticle(Vector3 position)
    {
        return (position.x >= center.x ? 1 : 0) +
               (position.y >= center.y ? 4 : 0) +
               (position.z >= center.z ? 2 : 0);
    }


    // Metodo per aggiornare la massa totale e il centro di massa del nodo
    private void UpdateMassDistribution(ParticleEntity particle)
    {
        totalMass += particle.mass;
        centerOfMass = (centerOfMass * (totalMass - particle.mass) + particle.position * particle.mass) / totalMass;
    }

    public Vector3 CalculateForceBarnesHut(ParticleEntity particle, OctreeNode node, float theta)
    {
        Vector3 force = Vector3.zero;

        if (node == null || particle == null || !node.populated) return force; // Return zero force if the node or particle is null

        // If the node is a leaf (has no children) and contains a particle
        if (node.particles.Count == 1 && node.particles[0] != particle)
        {
            // Calculate the direct force between the particle and the particle in the node
            Vector3 direction = node.particles[0].position - particle.position;
            float distanceSquared = direction.sqrMagnitude + softeningSquared;
            float forceMagnitude = Utility.G * particle.mass * node.particles[0].mass / distanceSquared;
            force = direction.normalized * forceMagnitude;
        }
        else if (node.size / Vector3.Distance(particle.position, node.centerOfMass) < theta)
        {
            // If the node is far enough, treat it as a single body
            Vector3 direction = node.centerOfMass - particle.position;
            float distanceSquared = direction.sqrMagnitude + softeningSquared;
            float forceMagnitude = Utility.G * particle.mass * node.totalMass / distanceSquared;
            force = direction.normalized * forceMagnitude;
        }
        else
        {
            // Otherwise, if the node is not far enough, recursively calculate the force from the children
            foreach (var child in node.children)
            {
                force += CalculateForceBarnesHut(particle, child, theta);
            }
        }

        return force;
    }
}