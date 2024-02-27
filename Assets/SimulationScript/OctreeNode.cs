using System.Collections.Generic;
using UnityEngine;

public class OctreeNode
{
    public Vector3 center; // Centro della cella
    public float size; // Lunghezza del lato della cella
    public Vector3 centerOfMass; // Centro di massa delle particelle all'interno della cella
    public float totalMass; // Massa totale delle particelle all'interno della cella
    public OctreeNode[] children; // Figli di questo nodo nell'octree
    public List<ParticleEntity> particles; // Particelle all'interno di questa cella
    private float softeningSquared = 0.01f; // Softening per evitare forze infinite
    public float G; // Costante gravitazionale
    private int maxParticlesPerNode = 4; // Numero massimo di particelle per nodo

    // Costruttore
    public OctreeNode(Vector3 center, float size)
    {
        this.center = center;
        this.size = size;
        centerOfMass = Vector3.zero;
        totalMass = 0f;
        children = new OctreeNode[8];
        particles = new List<ParticleEntity>();
        G = Utility.G;
    }

    // Metodo per aggiungere una particella a questo nodo (o ai suoi figli)
    public void AddParticle(ParticleEntity particle)
    {
        // Se il nodo ha figli, aggiungi la particella a uno dei figli
        if (children[0] != null)
        {
            int index = GetChildIndexForParticle(particle.position);
            children[index].AddParticle(particle);
        }
        else
        {
            // Altrimenti, aggiungi la particella a questo nodo
            particles.Add(particle);

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
                }
                // Pulisci la lista delle particelle dal nodo corrente
                particles.Clear();
            }
        }

        // Aggiorna il centro di massa e la massa totale
        UpdateMassDistribution(particle);
    }

    // Metodo per suddividere questo nodo creando otto nuovi figli
    private void Subdivide()
    {
        for (int i = 0; i < 8; i++)
        {
            // Calcola il centro per ogni nuovo figlio
            // Attenzione: la logica qui presuppone che il punto (0,0,0) sia al centro della cella corrente.
            // Se il tuo sistema di coordinate è diverso, potresti dover adattare.
            Vector3 childCenter = center + new Vector3(
                (i % 2 == 0 ? -size : size) / 4,  // Cambia per l'asse X
                (i / 4 == 0 ? -size : size) / 4,  // Cambia per l'asse Y
                (i / 2 % 2 == 0 ? -size : size) / 4); // Cambia per l'asse Z
            children[i] = new OctreeNode(childCenter, size / 2);
        }
    }

    // Metodo per determinare in quale figlio dovrebbe andare una particella data la sua posizione
    private int GetChildIndexForParticle(Vector3 position)
    {
        int index = 0;
        if (position.x >= center.x)
        {
            index += 1;
        }
        if (position.y >= center.y)
        {
            index += 4;
        }
        if (position.z >= center.z)
        {
            index += 2;
        }
        return index;
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
        if (node == null || particle == null)
        {
            return force; // Ritorna forza zero se il nodo o la particella sono nulli
        }

        // Se il nodo è una foglia (non ha figli) e contiene una particella
        if (node.particles.Count == 1 && node.particles[0] != particle)
        {
            // Calcola la forza diretta tra la particella e la particella nel nodo
            Vector3 direction = node.particles[0].position - particle.position;
            float distanceSquared = direction.sqrMagnitude + softeningSquared;
            float forceMagnitude = G * particle.mass * node.particles[0].mass / distanceSquared;
            force = direction.normalized * forceMagnitude;
        }
        else if (node.size / Vector3.Distance(particle.position, node.centerOfMass) < theta)
        {
            // Se il nodo è sufficientemente lontano, trattalo come un singolo corpo
            Vector3 direction = node.centerOfMass - particle.position;
            float distanceSquared = direction.sqrMagnitude + softeningSquared;
            float forceMagnitude = G * particle.mass * node.totalMass / distanceSquared;
            force = direction.normalized * forceMagnitude;
        }
        else
        {
            // Altrimenti, se il nodo non è sufficientemente lontano, calcola ricorsivamente la forza dai figli
            foreach (var child in node.children)
            {
                force += CalculateForceBarnesHut(particle, child, theta);
            }
        }
        return force;
    }
}