using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;
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

    public void SetPosition(Vector3 newPosition)
    {
        position = newPosition;
        particleObject.transform.position = newPosition;
    }
}
