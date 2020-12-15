using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	[System.Serializable]
	public class PerlinLayer
	{
		public bool active;
		[Range(0.00001f, 1.0f)]
		public float scale;
		public Vector2 offset;
		[Range(0.0f, 1.0f)]
		public float weight;
		[Range(-1.0f, 1.0f)]
		public float contrast;
	}

	public bool generateInEditMode;
	public Vector3Int mapSize = new Vector3Int(100, 10, 100);
	[Range(-1.0f, 1.0f)]
	public float contrast;
	[SerializeField]
	private Texture2D loadTexture;
	[PreviewField(200, ObjectFieldAlignment.Center)][ShowInInspector][ReadOnly]
	private Texture2D mapTexture;
	public AnimationCurve fallOff;
	public PerlinLayer fallOffNoise;
	public float scale = 1;
	public List<PerlinLayer> cliffs = new List<PerlinLayer>();
	public Transform treePrefab;
	public List<PerlinLayer> trees = new List<PerlinLayer>();

	private float[,] map;


	private void OnValidate()
	{
		mapSize = Vector3Int.Max(Vector3Int.one * 1, mapSize);
		if (loadTexture)
			mapTexture = loadTexture;
		if (gameObject.activeInHierarchy)
		{
			if (generateInEditMode)
				StartCoroutine(GenerateCoroutine(mapSize));
			else
				DestroyChildren();
		}
	}

	private void Start()
	{
		StartCoroutine(GenerateCoroutine(mapSize));
	}	


	public IEnumerator GenerateCoroutine(Vector3Int mapSize)
	{
		float[,] map;
		float groundHeight = 1.5f * (1.0f / mapSize.y);

		//Generate heightmap
		if (!loadTexture)
		{
			mapTexture = new Texture2D(mapSize.x, mapSize.z);
			map = new float[mapSize.x, mapSize.z];

			//Cliffs
			for (int z = 0; z < mapSize.z; z++)
				for (int x = 0; x < mapSize.x; x++)
				{
					//Land height
					float h = 0;
					float weightSum = Mathf.Max(1.0f, cliffs.FindAll(l => l.active).Sum(l => l.weight));
					for (int i = 0; i < cliffs.Count; i++)
						if (cliffs[i].active)
						{
							float p = Mathf.PerlinNoise(x * cliffs[i].scale * scale + cliffs[i].offset.x, z * cliffs[i].scale * scale + cliffs[i].offset.y);
							float f = (cliffs[i].contrast + 1.0f) / (1.0f - cliffs[i].contrast);
							p = Mathf.Clamp((f * (p - 0.5f) + 0.5f) * (cliffs[i].weight / weightSum), 0.0f, 0.99f);
							h += p;
						}
					float factor = (contrast + 1.0f) / (1.0f - contrast);
					h = Mathf.Clamp(factor * (h - 0.5f) + 0.5f, 0.0f, 0.99f);
					//h *= fallOff.Evaluate((new Vector2(mapSize.x, mapSize.z) * 0.5f - new Vector2(x, z)).magnitude / (new Vector2(mapSize.x, mapSize.z) * 0.5f).magnitude);
					//h *= fallOff.Evaluate(Distance.Manhattan2D(new Vector2(mapSize.x, mapSize.z) * 0.5f, new Vector2(x, z), Distance.Axis.z) / Distance.Manhattan2D(Vector2.zero, new Vector2(mapSize.x, mapSize.z) * 0.5f, Distance.Axis.z));
					float horizontal = (Mathf.Abs(mapSize.x * 0.5f - x) / (mapSize.x * 0.5f)) * (1.0f - fallOffNoise.weight) + Mathf.PerlinNoise(z * fallOffNoise.scale + fallOffNoise.offset.y, 0.0f) * fallOffNoise.weight;
					float vertical = (Mathf.Abs(mapSize.z * 0.5f - z) / (mapSize.z * 0.5f)) * (1.0f - fallOffNoise.weight) + Mathf.PerlinNoise(x * fallOffNoise.scale + fallOffNoise.offset.x, 0.0f) * fallOffNoise.weight;
					float distance = horizontal > vertical ? horizontal : vertical;
					h *= fallOff.Evaluate(distance);// * (fallOffNoise.active? Mathf.PerlinNoise(horizontal > vertical ? (z / mapSize.z) : (x / mapSize.x), 0.0f) : 1.0f));
					h *= 1.0f - groundHeight;
					h += groundHeight;
					map[x, z] = Mathf.Clamp(h,0.0f,0.99f);
					mapTexture.SetPixel(x, z, new Color(h, h, h));
					/*
					if(h == groundHeight)
					{
						float t = 0;
						for (int i = 0; i < trees.Count; i++)
							if (trees[i].active)
							{
								float p = Mathf.PerlinNoise(x * trees[i].scale + trees[i].offset.x, z * trees[i].scale + trees[i].offset.y);
								float f = (trees[i].contrast + 1.0f) / (1.0f - trees[i].contrast);
								p = Mathf.Clamp((f * (p - 0.5f) + 0.5f) * (trees[i].weight / weightSum), 0.0f, 0.99f);
								t += p;
							}
						if(t < 0.5f)
						{
							t = 0;
						}
						else
						{
							Instantiate(treePrefab, transform.position + new Vector3(x * transform.localScale.x, h* transform.localScale.y, z * transform.localScale.z), Quaternion.AngleAxis(Random.Range(0.0f,360.0f), Vector3.up));
						}
					}*/
				}

			//Rivers
			/*Vector2Int start = new Vector2Int(20, 20);
			Vector2 mainDir = Vector2.one;
			float sectionLength = 5;
			for (int i = 0; i < 5; i++)
			{
				Vector2 d = (mainDir + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f))).normalized * sectionLength;
				DrawLine(map, start, start + new Vector2Int((int)d.x, (int)d.y), (1.0f / mapSize.y) * 0.5f, 1, 10);
				start += new Vector2Int((int)d.x, (int)d.y);
			}*/
			/*Vector2 point = new Vector2(20, 20);
			Vector2 dir = new Vector2(0.5f, 0.5f);
			for (int i = 0; i < 50; i++)
			{
				DrawPoint(map, new Vector2Int((int)point.x, (int)point.y), groundHeight * 0.5f, groundHeight, 5);
				point += dir;
			}*/

			//Lakes
			

			mapTexture.Apply();
		}
		else
		{
			mapSize = new Vector3Int(loadTexture.width, mapSize.y, loadTexture.height);
			map = new float[mapSize.x, mapSize.z];
			for (int z = 0; z < mapSize.z; z++)
				for (int x = 0; x < mapSize.x; x++)
					map[x, z] = loadTexture.GetPixel(x, z).r;
		}
		this.map = map;
		this.mapSize = mapSize;

		//Generate meshes
		yield return StartCoroutine(GetComponent<TerracedTerrain.Terrain>().GenerateCoroutine(mapSize.y, map));
	}

	private void DestroyChildren()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
			DestroyImmediate(transform.GetChild(i).gameObject);
	}

	private void DrawLine(float[,] map, Vector2Int p0, Vector2Int p1, float value, float falloffValue = 0, int radius = 1)
	{
		int dx = p1.x - p0.x;
		int dy = p1.y - p0.y;
		var sign_x = dx > 0 ? 1 : -1;
		var sign_y = dy > 0 ? 1 : -1;
		dx = Mathf.Abs(dx);
		dy = Mathf.Abs(dy);

		Vector2Int p = new Vector2Int(p0.x, p0.y);
		DrawPoint(map, p, value, falloffValue, radius);
		float ix = 0;
		float iy = 0;
		while (ix < dx || iy < dy)
		{
			if ((0.5 + ix) / dx < (0.5 + iy) / dy)
			{
				p.x += sign_x;
				ix++;
			}
			else
			{
				p.y += sign_y;
				iy++;
			}
			DrawPoint(map, p, value, falloffValue, radius);
		}
	}

	private void DrawPoint(float [,] map, Vector2Int p, float value, float falloffValue = 0, int radius = 5)
	{
		if (radius < 0)
			Debug.LogError("radius must be positive number.", this);

		for (int x = -radius; x <= radius; x++)
			for (int y = -radius; y <= radius; y++)
			{
				float d = (new Vector2Int(x, y) - Vector2Int.zero).magnitude;
				if (d <= radius)
				{
					float v = falloffValue - (falloffValue - value) * (1.0f - d / radius);
					if (v < map[p.x + x, p.y + y])
					{
						map[p.x + x, p.y + y] = v;
						mapTexture.SetPixel(p.x + x, p.y + y, new Color(v, v, v));
					}
				}
			}
	}
}
