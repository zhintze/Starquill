using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticeData {

	/*
		VerticeData
		===========
		Each Vertice is a point on the map tile mesh.
		triangles are formed between the vertices to make a square surface to represent the world's ground.
	*/

	public TileData parentTile;
	public Vector3 vector; //holds Vector3 position
	public bool hasHeightMatched;



	public VerticeData(TileData parent) {
		parentTile = parent;
		vector = new Vector3();
		hasHeightMatched = false;
	}

	public VerticeData(TileData parent, Vector3 vectorInput) {
		parentTile = parent;
		vector = vectorInput;
		hasHeightMatched = false;
	}
	


}