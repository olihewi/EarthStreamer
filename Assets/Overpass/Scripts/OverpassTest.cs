using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Maps.Features
{
    public class OverpassTest : MonoBehaviour
    {
        [Header("Overpass API Query")]
        public string filePath;
        public Rect boundingBox;
        
        [TextArea(1,8)] public string request;
        
        private Dictionary<long, Vector3> nodes = new Dictionary<long, Vector3>();
    
        // Way: 916667470

        [ContextMenu("GenerateMesh")]
        public void GenerateMesh()
        {
            MapFeature.RegisterFeatureGenerators();
            /* Clear Children */
            while (transform.childCount != 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            
            XElement response = LoadFromFile().Element("osm");
            
            /* Load all Nodes */
            nodes = new Dictionary<long, Vector3>();
            foreach (XElement node in response.Elements("node"))
            {
                long id = long.Parse(node.Attribute("id").Value);
                if (nodes.ContainsKey(id)) continue;
                nodes.Add(id,new Vector3((float.Parse(node.Attribute("lon").Value) + 2.58F) * 40000.0F,0.0F,(float.Parse(node.Attribute("lat").Value) - 51.5F) * 40000.0F));
            }
            
            /* Generate the meshes for each way. */
            Dictionary<MapFeature, MapFeature.FeatureMeshData> featureTypes = new Dictionary<MapFeature, MapFeature.FeatureMeshData>();
            foreach (XElement way in response.Elements("way"))
            {
                List<Vector3> nodeRefs = new List<Vector3>();
                MapFeature generator = MapFeature.GetFeatureGenerator(way);
                if (generator == null) continue;
                if (!featureTypes.ContainsKey(generator)) featureTypes.Add(generator, new MapFeature.FeatureMeshData());
                MapFeature.FeatureMeshData meshData = featureTypes[generator];
                foreach (XElement node in way.Elements("nd"))
                {
                    nodeRefs.Add(nodes[long.Parse(node.Attribute("ref").Value)]);
                }
                MapFeature.FeatureMeshData newData = generator.GetMesh(way, nodeRefs, meshData.triOffset);
                meshData.vertices.AddRange(newData.vertices);
                meshData.triangles.AddRange(newData.triangles);
                meshData.triOffset = meshData.vertices.Count;
            }

            foreach (KeyValuePair<MapFeature, MapFeature.FeatureMeshData> featurePair in featureTypes)
            {
                // TODO: See if using prefabs for this is more efficient.
                MeshFilter meshFilter = new GameObject(featurePair.Key.name,new [] {typeof(MeshFilter), typeof(MeshRenderer)}).GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = featurePair.Key.materials;
                meshFilter.transform.parent = transform;
                /* Creating the new mesh */
                Mesh mesh = new Mesh();
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.vertices = featurePair.Value.vertices.ToArray();
                mesh.triangles = featurePair.Value.triangles.ToArray();
                mesh.RecalculateNormals();
                meshFilter.sharedMesh = mesh;
            }
        }
    
        private Vector3 LatLongToMercator(float _latitude, float _longitude, Vector3 _offset)
        {
            float mapSize = 40000.0F;
            float x = (_longitude + 180.0F + _offset.x) * (mapSize / 360.0F);
            float latRad = (_latitude + _offset.y) * Mathf.PI / 180.0F;
            float mercN = Mathf.Log(Mathf.Tan((Mathf.PI / 4.0F) + (latRad / 2.0F)));
            float y = (mapSize / 2.0F) - (mapSize * mercN / (2.0F * Mathf.PI));
            return new Vector3(x,0.0F,y);
        }
        
        [ContextMenu("Send Request")]
        public async void SendRequest()
        {
            SaveToFile(await GetAsync($"http://overpass-api.de/api/interpreter?data=[out:xml];(node({boundingBox.y - boundingBox.height},{boundingBox.x},{boundingBox.y},{boundingBox.x + boundingBox.width});way({boundingBox.y - boundingBox.height},{boundingBox.x},{boundingBox.y},{boundingBox.x + boundingBox.width});relation({boundingBox.y - boundingBox.height},{boundingBox.x},{boundingBox.y},{boundingBox.x + boundingBox.width}););out body;>;out skel qt;"));
        }
        
        private async Task<XDocument> GetAsync(string uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
    
            HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            return XDocument.Parse(await reader.ReadToEndAsync());
        }
    
        private void SaveToFile(XDocument _doc)
        {
            _doc.Save(filePath);
            Debug.Log($"Saved query to {filePath}");
        }
    
        private XDocument LoadFromFile()
        {
            return XDocument.Load(filePath);
        }
    }
}

