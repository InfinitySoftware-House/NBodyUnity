using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Utility
{
    public static readonly float G = 6.67430f; // Gravitational constant
    public const float C = 299792458f; // Speed of light in m/s
    private static readonly System.Random random = new();

    public static Star GenerateStars(double roll)
    {
        Star star = new Star();
        if (roll < 0.0001) // Type O
        {
            float temperature = (float)(30000 + random.NextDouble() * 20000);
            float mass = (float)(16 + random.NextDouble() * 14);
            star = new Star("O", mass, temperature, GetStarColor(temperature));
        }
        else if (roll < 0.13) // Type B
        {
            float temperature = (float)(10000 + random.NextDouble() * 2000);
            float mass = (float)(2 + random.NextDouble() * 6);
            star = new Star("B", mass, temperature, GetStarColor(temperature));
        }
        else if (roll < 0.73) // Type A
        {
            float temperature = (float)(7500 + random.NextDouble() * 1500);
            float mass = (float)(1.4 + random.NextDouble() * 0.6);
            star = new Star("A", mass, temperature, GetStarColor(temperature));
        }
        else if (roll < 3.73) // Type F
        {
            float temperature = (float)(6000 + random.NextDouble() * 1500);
            float mass = (float)(1.15 + random.NextDouble() * 0.35);
            star = new Star("F", mass, temperature, GetStarColor(temperature));
        }
        else // Types G, K, M
        {
            // Assuming equal distribution among G, K, M for simplification
            double subRoll = random.NextDouble() * 3;
            if (subRoll < 1)
            {
                float temperature = (float)(5200 + random.NextDouble() * 1500);
                float mass = (float)(0.85 + random.NextDouble() * 0.15);
                star = new Star("G", mass, temperature, GetStarColor(temperature));
            }
            else if (subRoll < 2)
            {
                float temperature = (float)(3700 + random.NextDouble() * 1500);
                float mass = (float)(0.8 + random.NextDouble() * 0.2);
                star = new Star("K", mass, temperature, GetStarColor(temperature));
            }
            else
            {
                float temperature = (float)(2400 + random.NextDouble() * 1300);
                float mass = (float)(0.45 + random.NextDouble() * 0.35);
                star = new Star("M", mass, temperature, GetStarColor(temperature));
            }
        }
        // CreateStarTypeCountCSV(stars, "Assets/SimulationScript/StarTypeCount.csv");
        return star;
    }

    public static float CalculateRedshift(float distance, float speed)
    {
        return speed / C / (1 + (speed / C)) * distance;
    }

    public static Color GetStarColor(float temperature)
    {
        // Define temperature ranges for different star types
        const float BROWN_DWARF_MAX_TEMP = 2500f;
        const float RED_MAX_TEMP = 3700f;
        const float ORANGE_MAX_TEMP = 5200f;
        const float YELLOW_MAX_TEMP = 6000f;
        const float YELLOW_WHITE_MAX_TEMP = 7500f;
        const float WHITE_MAX_TEMP = 10000f;
        const float BLUE_WHITE_MAX_TEMP = 30000f;
        // No need for a max for Blue Star as they are the hottest considered

        // Define color values for different star types
        Color brownDwarfColor = new Color(0.6f, 0.3f, 0f); // Dark red-brown
        Color redStar = new Color(1f, 0.5f, 0.5f); // Deep red
        Color orangeStar = new Color(1f, 0.65f, 0.4f); // Orange
        Color yellowStar = new Color(1f, 1f, 0.5f); // Yellow
        Color yellowWhiteStar = new Color(1f, 1f, 0.9f); // Yellow-White
        Color whiteStar = new Color(0.9f, 0.9f, 1f); // White
        Color blueWhiteStar = new Color(0.7f, 0.7f, 1f); // Blue-White
        Color blueStar = new Color(0.5f, 0.5f, 1f); // Deep Blue

        // Determine star color based on temperature
        if (temperature <= BROWN_DWARF_MAX_TEMP)
        {
            return brownDwarfColor; // Fixed color for Brown Dwarfs
        }
        else if (temperature <= RED_MAX_TEMP)
        {
            return Color.Lerp(brownDwarfColor, redStar, (temperature - BROWN_DWARF_MAX_TEMP) / (RED_MAX_TEMP - BROWN_DWARF_MAX_TEMP));
        }
        else if (temperature <= ORANGE_MAX_TEMP)
        {
            return Color.Lerp(redStar, orangeStar, (temperature - RED_MAX_TEMP) / (ORANGE_MAX_TEMP - RED_MAX_TEMP));
        }
        else if (temperature <= YELLOW_MAX_TEMP)
        {
            return Color.Lerp(orangeStar, yellowStar, (temperature - ORANGE_MAX_TEMP) / (YELLOW_MAX_TEMP - ORANGE_MAX_TEMP));
        }
        else if (temperature <= YELLOW_WHITE_MAX_TEMP)
        {
            return Color.Lerp(yellowStar, yellowWhiteStar, (temperature - YELLOW_MAX_TEMP) / (YELLOW_WHITE_MAX_TEMP - YELLOW_MAX_TEMP));
        }
        else if (temperature <= WHITE_MAX_TEMP)
        {
            return Color.Lerp(yellowWhiteStar, whiteStar, (temperature - YELLOW_WHITE_MAX_TEMP) / (WHITE_MAX_TEMP - YELLOW_WHITE_MAX_TEMP));
        }
        else if (temperature <= BLUE_WHITE_MAX_TEMP)
        {
            return Color.Lerp(whiteStar, blueWhiteStar, (temperature - WHITE_MAX_TEMP) / (BLUE_WHITE_MAX_TEMP - WHITE_MAX_TEMP));
        }
        else // For the hottest stars
        {
            return Color.Lerp(blueWhiteStar, blueStar, (temperature - BLUE_WHITE_MAX_TEMP) / (50000 - BLUE_WHITE_MAX_TEMP)); // Assume an arbitrary upper limit for temperature
        }
    }

    public static void CreateStarTypeCountCSV(List<Star> stars, string filePath)
    {
        Dictionary<string, int> starTypeCount = new Dictionary<string, int>();

        foreach (Star star in stars)
        {
            if (starTypeCount.ContainsKey(star.Type))
            {
                starTypeCount[star.Type]++;
            }
            else
            {
                starTypeCount[star.Type] = 1;
            }
        }

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Star Type,Count");

            foreach (KeyValuePair<string, int> entry in starTypeCount)
            {
                writer.WriteLine($"{entry.Key},{entry.Value}");
            }
        }
    }

    public static IEnumerator FadeOutCanvas(GameObject canvas)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        canvasGroup.interactable = false;
        yield return null;
    }

    public static IEnumerator FadeInCanvas(GameObject canvas)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime;
            yield return null;
        }
        canvasGroup.interactable = false;
        yield return null;
    }
}