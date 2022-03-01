using System.Collections;
using System.Collections.Generic;
using Maps;
using Maps.Features;
using UnityEngine;

public class RoadVehicle : MonoBehaviour
{
    public HighwayNetwork network;
    public HighwayNetwork.HighwayElement currentNode;
    public float speed = 3.0F;
    private List<HighwayNetwork.HighwayElement> visitedNodes = new List<HighwayNetwork.HighwayElement>();
    private int retries = 0;
    void Start()
    {
        currentNode = network.highwayNetwork[0];
        foreach (HighwayNetwork.HighwayElement element in network.highwayNetwork)
        {
            if (Vector3.Distance(element.node.position,transform.position) < Vector3.Distance(currentNode.node.position,transform.position)) currentNode = element;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentNode.node.position - transform.position), Time.deltaTime * 2.0F);
        transform.position = Vector3.MoveTowards(transform.position, currentNode.node.position, speed * Time.deltaTime);
        if (transform.position == currentNode.node.position)
        {
            bool found = false;
            foreach (HighwayNetwork.HighwayElement element in currentNode.connections)
            {
                if (!visitedNodes.Contains(element))
                {
                    currentNode = element;
                    found = true;
                }
            }
            if (!found)
            {
                currentNode = currentNode.connections[MapFeature.ClampListIndex(retries+=3,currentNode.connections.Count)];
            }
            visitedNodes.Add(currentNode);
        }
    }
}
