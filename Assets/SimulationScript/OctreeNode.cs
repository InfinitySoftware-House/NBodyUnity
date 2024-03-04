using System.Collections.Generic;
using Vector3 = UnityEngine.Vector3;

public class OctreeNode
{
    public Vector3 Center; // Centro della cella
    public float Size; // Lunghezza del lato della cella
    public Vector3 CenterOfMass; // Centro di massa delle particelle all'interno della cella
    public float TotalMass; // Massa totale delle particelle all'interno della cella
    public OctreeNode[] Children; // Figli di questo nodo nell'octree
    public List<ParticleEntity> Particles; // Particelle all'interno di questo nodo
    private readonly float SofteningSquared = 0.001f; // Softening per evitare forze infinite
    private readonly int MaxParticlesPerNode = 120; // Numero massimo di particelle per nodo
    private bool Populated = false; // Flag per indicare se il nodo è stato popolato

    // Costruttore
    public OctreeNode(Vector3 center, float size)
    {
        Center = center;
        Size = size;
        CenterOfMass = Vector3.zero;
        TotalMass = 0f;
        Children = new OctreeNode[8];
        Particles = new List<ParticleEntity>();
    }

    // Metodo per aggiungere una particella a questo nodo (o ai suoi figli)
    public void AddParticle(ParticleEntity particle)
    {
        // Se il nodo ha figli, aggiungi la particella a uno dei figli
        if (Children[0] != null)
        {
            int index = GetChildIndexForParticle(particle.position);
            Children[index].AddParticle(particle);
            Children[index].Populated = true;
        }
        else
        {
            // Altrimenti, aggiungi la particella a questo nodo
            Particles.Add(particle);
            Populated = true;

            // Se dopo l'aggiunta la cella supera un certo limite di particelle,
            // dividila creando nuovi nodi figli e redistribuendo le particelle
            if (Particles.Count > MaxParticlesPerNode) // Soglia arbitraria
            {
                Subdivide();
                // Dopo la suddivisione, riposiziona le particelle esistenti nei nuovi figli
                foreach (var existingParticle in Particles)
                {
                    int index = GetChildIndexForParticle(existingParticle.position);
                    Children[index].AddParticle(existingParticle);
                    Children[index].Populated = true;
                }
                // Pulisci la lista delle particelle dal nodo corrente
                Particles.Clear();
            }
            else if (Particles.Count == 0)
            {
                Populated = false;
            }
        }

        // Aggiorna il centro di massa e la massa totale
        UpdateMassDistribution(particle);
    }

    // Metodo per suddividere questo nodo creando otto nuovi figli
    private void Subdivide()
    {
        Vector3 halfSize = new(Size / 8, Size / 8, Size / 8);
        for (int i = 0; i < 8; i++)
        {
            Vector3 childCenter = Center + new Vector3(
                (i % 2 == 0 ? -1 : 1) * halfSize.x,
                (i / 4 == 0 ? -1 : 1) * halfSize.y,
                (i / 2 % 2 == 0 ? -1 : 1) * halfSize.z);
            Children[i] = new OctreeNode(childCenter, Size / 2);
        }
    }

    // Metodo per determinare in quale figlio dovrebbe andare una particella data la sua posizione
    private int GetChildIndexForParticle(Vector3 position)
    {
        return (position.x >= Center.x ? 1 : 0) +
               (position.y >= Center.y ? 4 : 0) +
               (position.z >= Center.z ? 2 : 0);
    }


    // Metodo per aggiornare la massa totale e il centro di massa del nodo
    private void UpdateMassDistribution(ParticleEntity particle)
    {
        TotalMass += particle.mass;
        CenterOfMass = (CenterOfMass * (TotalMass - particle.mass) + particle.position * particle.mass) / TotalMass;
    }

    public Vector3 CalculateForceBarnesHut(ParticleEntity particle, OctreeNode node, float theta)
    {
        Vector3 force = Vector3.zero;

        if (node == null || particle == null || !node.Populated) return force; // Return zero force if the node or particle is null

        // If the node is a leaf (has no children) and contains a particle
        if (node.Particles.Count == 1 && node.Particles[0] != particle)
        {
            // Calculate the direct force between the particle and the particle in the node
            Vector3 direction = node.Particles[0].position - particle.position;
            float forceMagnitude = Utility.G * particle.mass * node.Particles[0].mass / direction.sqrMagnitude + SofteningSquared;
            force = direction.normalized * forceMagnitude;
        }
        else if (node.Size / Vector3.Distance(particle.position, node.CenterOfMass) < theta)
        {
            // If the node is far enough, treat it as a single body
            Vector3 direction = node.CenterOfMass - particle.position;
            float forceMagnitude = Utility.G * particle.mass * node.TotalMass / direction.sqrMagnitude + SofteningSquared;
            force = direction.normalized * forceMagnitude;
        }
        else
        {
            // Otherwise, if the node is not far enough, recursively calculate the force from the children
            for (int i = 0; i < node.Children.Length; i++)
            {
                force += CalculateForceBarnesHut(particle, node.Children[i], theta);
            }
        }

        return force;
    }
}