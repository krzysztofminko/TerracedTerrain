using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerracedTerrain
{
	[RequireComponent(typeof(MeshFilter))]
	public class Terrain : MonoBehaviour
	{
		[System.Serializable]
		public struct Layer
		{
			//TODO: ...
			public Material material;
			//[MinMaxSlider(0.0f, 1.0f)]
			//public Vector2 range;
		}
		public List<Layer> layers;
		
		public IEnumerator GenerateCoroutine(int height, float[,] map)
		{
			Vector3Int mapSize = new Vector3Int(map.GetLength(0), height, map.GetLength(1));

			//Generate heightmap mesh
			List<Vector3> hmpVertices = new List<Vector3>();
			List<int> hmpTriangles = new List<int>();
			for (int z = 0; z < mapSize.z; z++)
				for (int x = 0; x < mapSize.x; x++)
				{
					hmpVertices.Add(new Vector3(x * transform.localScale.x, map[x,z] * mapSize.y, z * transform.localScale.z));
					if (x < mapSize.x - 1 && z < mapSize.z - 1)
					{
						hmpTriangles.Add(hmpVertices.Count - 1);
						hmpTriangles.Add(hmpVertices.Count - 1 + mapSize.x);
						hmpTriangles.Add(hmpVertices.Count - 1 + 1);
						hmpTriangles.Add(hmpVertices.Count - 1 + mapSize.x);
						hmpTriangles.Add(hmpVertices.Count - 1 + mapSize.x + 1);
						hmpTriangles.Add(hmpVertices.Count - 1 + 1);
					}
				}
			yield return null;

			//Generate Terraces
			for (int h = 0; h < mapSize.y; h++)
			{
				if (layers[h].material)
				{
					GameObject go = transform.Find(h.ToString())?.gameObject;
					if (!go)
					{
						go = new GameObject(h.ToString(), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
						go.transform.parent = transform;
						go.transform.localPosition = Vector3.zero;
					}
					go.GetComponent<MeshRenderer>().sharedMaterial = h < layers.Count ? layers[h].material : null;

					Mesh mesh = new Mesh();
					mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

					List<Vector3> vertices = new List<Vector3>();
					List<int> triangles = new List<int>();
					int v = 0;

					for (int t = 0; t < hmpTriangles.Count / 3; t++)
					{
						Vector3 v1 = hmpVertices[hmpTriangles[t * 3]];
						Vector3 v2 = hmpVertices[hmpTriangles[t * 3 + 1]];
						Vector3 v3 = hmpVertices[hmpTriangles[t * 3 + 2]];

						float h1 = v1.y;
						float h2 = v2.y;
						float h3 = v3.y;

						int hMin = Mathf.FloorToInt(Mathf.Min(h1, h2, h3));
						int hMax = Mathf.FloorToInt(Mathf.Max(h1, h2, h3));

						//for (int h = hMin; h <= hMax; h++)
						if (h >= hMin && h <= hMax)
						{
							int above = 0;

							if (h1 < h)
							{
								if (h2 < h)
								{
									if (h3 < h)
									{
										//all below
									}
									else
									{
										above = 1;
									}
								}
								else
								{
									if (h3 < h)
									{
										above = 1;
										Vector3 oldv1 = v1;
										Vector3 oldv2 = v2;
										Vector3 oldv3 = v3;
										v1 = oldv3;
										v2 = oldv1;
										v3 = oldv2;
									}
									else
									{
										above = 2;
										Vector3 oldv1 = v1;
										Vector3 oldv2 = v2;
										Vector3 oldv3 = v3;
										v1 = oldv2;
										v2 = oldv3;
										v3 = oldv1;
									}
								}
							}
							else
							{
								if (h2 < h)
								{
									if (h3 < h)
									{
										above = 1;
										Vector3 oldv1 = v1;
										Vector3 oldv2 = v2;
										Vector3 oldv3 = v3;
										v1 = oldv2;
										v2 = oldv3;
										v3 = oldv1;
									}
									else
									{
										above = 2;
										Vector3 oldv1 = v1;
										Vector3 oldv2 = v2;
										Vector3 oldv3 = v3;
										v1 = oldv3;
										v2 = oldv1;
										v3 = oldv2;
									}
								}
								else
								{
									if (h3 < h)
									{
										above = 2;
									}
									else
									{
										above = 3;
									}
								}
							}

							h1 = v1.y;
							h2 = v2.y;
							h3 = v3.y;

							float yScale = transform.localScale.y;

							Vector3 v1_c = new Vector3(v1.x, h * yScale, v1.z);
							Vector3 v2_c = new Vector3(v2.x, h * yScale, v2.z);
							Vector3 v3_c = new Vector3(v3.x, h * yScale, v3.z);

							Vector3 v1_b = new Vector3(v1.x, (h - 1) * yScale, v1.z);
							Vector3 v2_b = new Vector3(v2.x, (h - 1) * yScale, v2.z);
							Vector3 v3_b = new Vector3(v3.x, (h - 1) * yScale, v3.z);

							if (above == 3)
							{
								vertices.Add(v1_c);
								vertices.Add(v2_c);
								vertices.Add(v3_c);
								triangles.Add(v);
								triangles.Add(v + 1);
								triangles.Add(v + 2);
								v += 3;
							}
							else
							{
								float t1 = (h1 - h) / (h1 - h3);
								Vector3 v1_c_n = Vector3.Lerp(v1_c, v3_c, t1);
								Vector3 v1_b_n = Vector3.Lerp(v1_b, v3_b, t1);

								float t2 = (h2 - h) / (h2 - h3);
								Vector3 v2_c_n = Vector3.Lerp(v2_c, v3_c, t2);
								Vector3 v2_b_n = Vector3.Lerp(v2_b, v3_b, t2);

								if (above == 2)
								{
									vertices.Add(v1_c);
									vertices.Add(v2_c);
									vertices.Add(v2_c_n);
									vertices.Add(v1_c_n);
									triangles.Add(v);
									triangles.Add(v + 1);
									triangles.Add(v + 2);
									triangles.Add(v + 2);
									triangles.Add(v + 3);
									triangles.Add(v);
									v += 4;

									vertices.Add(v1_c_n);
									vertices.Add(v2_c_n);
									vertices.Add(v2_b_n);
									vertices.Add(v1_b_n);
									triangles.Add(v);
									triangles.Add(v + 1);
									triangles.Add(v + 2);
									triangles.Add(v);
									triangles.Add(v + 2);
									triangles.Add(v + 3);
									v += 4;
								}
								else if (above == 1)
								{
									vertices.Add(v3_c);
									vertices.Add(v1_c_n);
									vertices.Add(v2_c_n);
									triangles.Add(v);
									triangles.Add(v + 1);
									triangles.Add(v + 2);
									v += 3;

									vertices.Add(v2_c_n);
									vertices.Add(v1_c_n);
									vertices.Add(v1_b_n);
									vertices.Add(v2_b_n);
									triangles.Add(v);
									triangles.Add(v + 1);
									triangles.Add(v + 3);
									triangles.Add(v + 1);
									triangles.Add(v + 2);
									triangles.Add(v + 3);
									v += 4;
								}
							}
						}
					}
					mesh.vertices = vertices.ToArray();
					mesh.triangles = triangles.ToArray();
					mesh.RecalculateNormals();
					go.GetComponent<MeshFilter>().mesh = mesh;
					go.GetComponent<MeshCollider>().sharedMesh = mesh;
					//yield return null;
				}
			}
			for (int i = transform.childCount - 1; i >= mapSize.y; i--)
				DestroyImmediate(transform.GetChild(i).gameObject);
		}
	}
}
