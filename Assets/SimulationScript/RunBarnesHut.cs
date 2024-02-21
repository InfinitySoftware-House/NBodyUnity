using UnityEngine;
using System.Collections.Generic;
using BHTree;
 
public class NBody : MonoBehaviour {
 
    public GameObject planetPrefab01;
    public double dt = 0.1;
    public double radius = 1000;
    public int N = 10;
    public double G = 1;
 
    private Body[] nBodies;
    private GameObject[] pBodies;
 
    void Start()
    {
        Initialize();
    }
 
    void FixedUpdate()
    {
        Quad quad = new Quad(0, 0, radius * 2);
        BarnesHutTree tree = new BarnesHutTree(quad);
        pBodies = GameObject.FindGameObjectsWithTag("Celestial Object");
 
        for(int i = 0; i < N; i++)
        {
            if(nBodies[i].inCheck(quad))
            {
                tree.insert(nBodies[i]);
            }
        }
 
        for(int i = 0; i < N; i++)
        {
            nBodies[i].resetForce();
            tree.updateForce(nBodies[i]);
            nBodies[i].update(pBodies[i].transform.position.x, pBodies[i].transform.position.y);
        }
 
        for(int i = 0; i < N; i++)
        {
            Vector2 force = new Vector2((float)nBodies[i].fx, (float)nBodies[i].fy);
            pBodies[i].GetComponent<Rigidbody>().AddForce(force);
        }
    }
 
    void Initialize()
    {
        List<int> places = new List<int>();
        for (int i = 0; i < N; i++)
        {
            bool check = true;
            while (check)
            {
                int x = Random.Range(0,(int)radius);
                int y = Random.Range(0,(int)radius);
                if(!places.Contains(x + y))
                {
                    places.Add(x + y);
                    Instantiate(planetPrefab01, new Vector3(x, y) , Quaternion.identity);
                    check = false;
                }
            }
        }
 
        pBodies = GameObject.FindGameObjectsWithTag("Celestial Object");
        nBodies = new Body[N];
 
        for(int i = 0; i < N; i++)
        {
            double px = pBodies[i].transform.position.x;
            double py = pBodies[i].transform.position.y;
            double mass = pBodies[i].GetComponent<Rigidbody>().mass;
            nBodies[i] = new Body(px, py, mass, G);
        }
    }
}
 