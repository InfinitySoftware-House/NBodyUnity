using TMPro;
using UnityEngine;

public class ObjectInfoModel 
{
    public string Name { get; set; }
    public string Type { get; set; }
    public float Temperature { get; set; }
    public float Mass { get; set; }
    public Vector3 Position { get; set; }
}

public class ObjectInfo : MonoBehaviour
{
    public GameObject panel;
    public bool show = false;
    public ObjectInfoModel objectInfoModel;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text temperatureText;
    public TMP_Text massText;
    public TMP_Text particlePositionText;

    // Update is called once per frame
    void Update()
    {
        if (show)
        {
            nameText.text = objectInfoModel.Name;
            typeText.text = objectInfoModel.Type;
            temperatureText.text = objectInfoModel.Temperature.ToString("N0").Replace(",", ".") + " K";
            massText.text = objectInfoModel.Mass.ToString("F3") + " M";
            // particlePositionText.text = "X: " + objectInfoModel.Position.x.ToString("F0") + " Y: " + objectInfoModel.Position.y.ToString("F0") + " Z: " + objectInfoModel.Position.z.ToString("F0");
        }
    }

    public void ShowInfo(ObjectInfoModel objectInfoModel)
    {
        this.objectInfoModel = objectInfoModel;
        show = true;
    }
}
