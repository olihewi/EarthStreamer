using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Maps
{
  // All element types can have tags.
  public abstract class OSMType
  {
    public Dictionary<string, string> tags = new Dictionary<string, string>();
  }
  // Nodes are single points in space defined by latitude and longitude.
  // See: https://wiki.openstreetmap.org/wiki/Node
  public class Node : OSMType
  {
    public Node(XElement _element, Vector2 _origin)
    {
      id = long.Parse(_element.FirstAttribute.Value);
      // TODO: Take into account elevation data.
      position = new Vector3((float.Parse(_element.Attribute("lon").Value) - _origin.x) * 111319.444F, 0.0F /* TODO: Heightmap */, (float.Parse(_element.Attribute("lat").Value) - _origin.y) * 111319.444F);
      tags = new Dictionary<string, string>();
      foreach (XElement tag in _element.Elements("tag"))
      {
        string key = tag.Attribute("k").Value;
        string value = tag.Attribute("v").Value;
        tags.Add(key, value);
      }
    }
    public long id;
    public Vector3 position;
  }
  // Ways are a line, such as a road, walls or area.
  // See: https://wiki.openstreetmap.org/wiki/Way
  public class Way : OSMType
  {
    public Way(XElement _element, IReadOnlyDictionary<long, Node> _nodes)
    {
      id = long.Parse(_element.FirstAttribute.Value);
      XElement[] nodeReferences = _element.Elements("nd").ToArray();
      nodes = new Node[nodeReferences.Length];
      for (int i = 0; i < nodeReferences.Length; i++)
      {
        nodes[i] = _nodes[long.Parse(nodeReferences[i].FirstAttribute.Value)];
      }
      tags = new Dictionary<string, string>();
      foreach (XElement tag in _element.Elements("tag"))
      {
        string key = tag.Attribute("k").Value;
        string value = tag.Attribute("v").Value;
        tags.Add(key, value);
      }
    }
    public long id;
    public Node[] nodes;
  }
  // A Relation is an element with a group of members, used to define logical or geographical relationships between objects.
  // See: https://wiki.openstreetmap.org/wiki/Relation
  public class Relation : OSMType
  {
    // Constructor
    public Relation(XElement _element, IReadOnlyDictionary<long, Node> _nodes, IReadOnlyDictionary<long, Way> _ways)
    {
      id = long.Parse(_element.FirstAttribute.Value);
      
      List<XElement> nodeMembers = new List<XElement>();
      List<XElement> wayMembers = new List<XElement>();
      foreach (XElement member in _element.Elements("member"))
      {
        string type = member.FirstAttribute.Value;
        List<XElement> thisList = type == "way" ? wayMembers : nodeMembers;
        thisList.Add(member);
      }
      List<KeyValuePair<Node, string>> nodesList = new List<KeyValuePair<Node, string>>();
      foreach (XElement nodeMember in nodeMembers)
      {
        long nodeID = long.Parse(nodeMember.Attribute("ref").Value);
        if (!_nodes.ContainsKey(nodeID)) continue;
        nodesList.Add(new KeyValuePair<Node, string>(_nodes[nodeID], nodeMember.Attribute("role").Value));
      }
      nodes = nodesList.ToArray();
      
      List<KeyValuePair<Way, string>> waysList = new List<KeyValuePair<Way, string>>();
      foreach (XElement wayMember in wayMembers)
      {
        long wayID = long.Parse(wayMember.Attribute("ref").Value);
        if (!_nodes.ContainsKey(wayID)) continue;
        waysList.Add(new KeyValuePair<Way, string>(_ways[wayID], wayMember.Attribute("role").Value));
      }
      ways = waysList.ToArray();
      
      tags = new Dictionary<string, string>();
      foreach (XElement tag in _element.Elements("tag"))
      {
        string key = tag.Attribute("k").Value;
        string value = tag.Attribute("v").Value;
        tags.Add(key, value);
      }
    }
    // Member Variables
    public long id;
    public KeyValuePair<Node, string>[] nodes;
    public KeyValuePair<Way, string>[] ways;
  }
}
