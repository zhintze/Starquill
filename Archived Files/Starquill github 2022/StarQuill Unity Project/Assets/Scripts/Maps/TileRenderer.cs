using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileRenderer : MonoBehaviour
{
    

	private List<GameObject> gameObjectList;
	private Mesh mesh;
	private VerticeData[,] vertices2d;
	private Vector3[] vertices;
	private int xSize;
	private int zSize;
	[HideInInspector] public Vector3 truePOS;

	private int triangleScale;



	

	
	public void RenderTile(TileData tile,Vector3 WorldTriangleScale) {


		transform.localScale = WorldTriangleScale;
		triangleScale = (int)WorldTriangleScale.x;

		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "Procedural Floor";

		vertices2d = tile.verticeData;
		xSize = tile.xTileSize;
		zSize = tile.zTileSize;
		vertices = new Vector3[(xSize+1)*(zSize+1)*triangleScale];


		SortMeshVertices();



	}


	private void SortMeshVertices() {

		int currentVertice = 0;

		for (int y = 0; y < zSize - 1; y++) {
			for (int x = 0; x < xSize - 1; x++) {
				vertices[currentVertice] = vertices2d[0+x,0+y].vector;
				currentVertice++;
				
				vertices[currentVertice] = vertices2d[0+x,1+y].vector;
				currentVertice++;

				vertices[currentVertice] = vertices2d[1+x,1+y].vector;
				currentVertice++;

				vertices[currentVertice] = vertices2d[0+x,0+y].vector;
				currentVertice++;

				vertices[currentVertice] = vertices2d[1+x,1+y].vector;
				currentVertice++;

				vertices[currentVertice] = vertices2d[1+x,0+y].vector;
				currentVertice++;
				
			}
			
		}

		mesh.vertices = vertices;
		mesh.uv = UvCalculator.CalculateUVs(vertices,1.0f);
		CreateTriangles();
	}


	private void CreateTriangles() {

		int[] triangles = new int[xSize * zSize * triangleScale*3];
		
		for (int i = 0; i < vertices.Length; i++) {
			triangles[i] = i;
		}
		
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		

		gameObject.AddComponent<MeshCollider>();

		
	}
	


}






/*
    UvCalculator
    =============
    Adds UV to Tile Mesh.
    This allows textures to properly display and repeat across the surface of the mesh.
*/
public class UvCalculator
 {
     private enum Facing { Up, Forward, Right };
     
     public static Vector2[] CalculateUVs(Vector3[] v/*vertices*/, float scale)
     {
         var uvs = new Vector2[v.Length];
         
         for (int i = 0 ; i < uvs.Length; i += 3)
         {
             int i0 = i;
             int i1 = i+1;
             int i2 = i+2;

            if(i == uvs.Length - 1) {
                i1 = 0;
                i2 = 1;
            }
            if(i == uvs.Length - 2) {
                i2 = 0;
            }
             
             Vector3 v0 = v[i0];
             Vector3 v1 = v[i1];
             Vector3 v2 = v[i2];
             
             Vector3 side1 = v1 - v0;
             Vector3 side2 = v2 - v0;
             var direction = Vector3.Cross(side1, side2);
             var facing = FacingDirection(direction);
             switch (facing)
             {
             case Facing.Forward:
                 uvs[i0] = ScaledUV(v0.x, v0.y, scale);
                 uvs[i1] = ScaledUV(v1.x, v1.y, scale);
                 uvs[i2] = ScaledUV(v2.x, v2.y, scale);
                 break;
             case Facing.Up:
                 uvs[i0] = ScaledUV(v0.x, v0.z, scale);
                 uvs[i1] = ScaledUV(v1.x, v1.z, scale);
                 uvs[i2] = ScaledUV(v2.x, v2.z, scale);
                 break;
             case Facing.Right:
                 uvs[i0] = ScaledUV(v0.y, v0.z, scale);
                 uvs[i1] = ScaledUV(v1.y, v1.z, scale);
                 uvs[i2] = ScaledUV(v2.y, v2.z, scale);
                 break;
             }
         }
         return uvs;
     }
     
     private static bool FacesThisWay(Vector3 v, Vector3 dir, Facing p, ref float maxDot, ref Facing ret)
     {
         float t = Vector3.Dot(v, dir);
         if (t > maxDot)
         {
             ret = p;
             maxDot = t;
             return true;
         }
         return false;
     }
     
     private static Facing FacingDirection(Vector3 v)
     {
         var ret = Facing.Up;
         float maxDot = Mathf.NegativeInfinity;
         
         if (!FacesThisWay(v, Vector3.right, Facing.Right, ref maxDot, ref ret))
             FacesThisWay(v, Vector3.left, Facing.Right, ref maxDot, ref ret);
         
         if (!FacesThisWay(v, Vector3.forward, Facing.Forward, ref maxDot, ref ret))
             FacesThisWay(v, Vector3.back, Facing.Forward, ref maxDot, ref ret);
         
         if (!FacesThisWay(v, Vector3.up, Facing.Up, ref maxDot, ref ret))
             FacesThisWay(v, Vector3.down, Facing.Up, ref maxDot, ref ret);
         
         return ret;
     }
     
     private static Vector2 ScaledUV(float uv1, float uv2, float scale)
     {
         return new Vector2(uv1 / scale, uv2 / scale);
     }
}
