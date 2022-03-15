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
    void Start()
    {
        currentNode = network.highwayNetwork[0];
        foreach (HighwayNetwork.HighwayElement element in network.highwayNetwork)
        {
            if (Vector3.Distance(element.node.chunkPos,transform.position) < Vector3.Distance(currentNode.node.chunkPos,transform.position)) currentNode = element;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentNode.node.chunkPos - transform.position), Time.deltaTime * 2.0F);
        transform.position = Vector3.MoveTowards(transform.position, currentNode.node.chunkPos, speed * Time.deltaTime);
        if (transform.position == currentNode.node.chunkPos)
        {
            bool found = false;
            foreach (HighwayNetwork.HighwayConnection connection in currentNode.connections)
            {
                HighwayNetwork.HighwayElement element = connection.element;
                if (!visitedNodes.Contains(element))
                {
                    currentNode = element;
                    found = true;
                }
            }
            if (!found)
            {
                currentNode = currentNode.connections[0].element;
            }
            visitedNodes.Add(currentNode);
        }
    }
}
