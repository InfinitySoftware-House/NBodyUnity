using UnityEngine;

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
