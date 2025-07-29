using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public MapData parentMap;
	public VerticeData[,] verticeData;

	[HideInInspector]public float heightMatchChance = 50f; //92f;
	[HideInInspector]public int xTileSize;
	[HideInInspector]public int zTileSize;
	[HideInInspector]public bool isCurrentlyRendered = false;


	public float heightFloor, heightCeiling;

	public int terrain;
	public ZoneData parentZone;

	//public List<int> objectTypeArray;
	//public List<Vector3> objectPositionArray; 

	public LocationData locationData;




    public TileData(MapData parent) {

		//objectTypeArray = new List<int>();
		//objectPositionArray = new List<Vector3>();
		
		parentZone = null;
		parentMap = parent;

		xTileSize = (int)parent.triangleScale.x / (int)parent.triangleScale.x * 10;
		zTileSize = (int)parent.triangleScale.z / (int)parent.triangleScale.z * 10;

		verticeData = new VerticeData[xTileSize,zTileSize];


		
		for (int x = 0; x < xTileSize; x++) {
			for (int z = 0; z < zTileSize; z++) {

				//generates the position of each vertice point and stores the data
				verticeData[x,z] = new VerticeData(this, new Vector3(x,0,z));
	
			}
		}
		

	}



    public void GenerateTileVertices(VerticeData[,] verticeDataArray, int heightFloor, int heightCeiling) {

		float randomNum = 0;
		float selectedNum = 0;
		int positionX = 0;
		int positionZ = 0;

		bool verticeMiddleLeft = false;
		bool verticeMiddleRight = false;
		bool verticeUpperLeft = false;
		bool verticeUpperMiddle = false;
		bool verticeUpperRight = false;
		bool verticeLowerLeft = false;
		bool verticeLowerMiddle = false;
		bool verticeLowerRight = false;

		for (int x = 0; x < xTileSize; x++) {
			for (int z = 0; z < zTileSize; z++) {

				selectedNum = UnityEngine.Random.Range(heightFloor, heightCeiling+1);
				randomNum = UnityEngine.Random.Range(0,101);

				//mark surrounding vertices as true if they already exist
				if (randomNum < heightMatchChance) {
				  //if (randomNum < 200) {
					//Lower Middle
					if (z-1 >= 0) {verticeLowerMiddle = true;}
					//Lower Left
					if (z-1 >= 0 && x-1 >= 0) {verticeLowerLeft = true;}
					//Lower Right
					if (z-1 >= 0 && x+1 < xTileSize) {verticeLowerRight = true;}
					//Upper Middle
					if (z+1 < zTileSize) {verticeUpperMiddle = true;}
					//Upper Left
					if (z+1 < zTileSize && x-1 >= 0) {verticeUpperLeft = true;}
					//Upper Right
					if (z+1 < zTileSize && x+1 < xTileSize) {verticeUpperRight = true;}
					//Middle Left
					if (x-1 >= 0) {verticeMiddleLeft = true;}
					//Middle Right
					if (x+1 < xTileSize) {verticeMiddleRight = true;}

					//loop through 8 possible surrounding vertices until one is chosen
					//only continue when chosen vertice exists
					bool isValueChosen = false;
					int randomInt = 0;
					do {
						randomInt = UnityEngine.Random.Range(0,8);
						if (randomInt == 0) {
							if (verticeLowerMiddle == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x,z-1].vector.y;
							}
						} else if (randomInt == 1) {
							if (verticeLowerLeft == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x-1,z-1].vector.y;
							}
						} else if (randomInt == 2) {
							if (verticeLowerRight == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x+1,z-1].vector.y;
							}
						} else if (randomInt == 3) {
							if (verticeMiddleLeft == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x-1,z].vector.y;
							}
						} else if (randomInt == 4) {
							if (verticeMiddleRight == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x+1,z].vector.y;
							} 
						} else if (randomInt == 5) {
							if (verticeUpperMiddle == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x,z+1].vector.y;
							}
						} else if (randomInt == 6) {
							if (verticeUpperLeft == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x-1,z+1].vector.y;
							}
						} else if (randomInt == 7) {
							if (verticeUpperRight == true) {
								isValueChosen = true;
								selectedNum = verticeDataArray[x+1,z+1].vector.y;
							}
						}
					} while (isValueChosen == false);


				}

				verticeDataArray[x,z].vector = new Vector3(x,selectedNum,z);
				positionZ += 1;

				//reset the surrounding vertice existence confirmation
				verticeMiddleLeft = false;
				verticeMiddleRight = false;
				verticeUpperLeft = false;
				verticeUpperMiddle = false;
				verticeUpperRight = false;
				verticeLowerLeft = false;
				verticeLowerMiddle = false;
				verticeLowerRight = false;



			}
			positionZ = 0;
			positionX += 1;
			
		}

	}


	/*
		GetVertices
		============
		Called within TileEdgeMatching() function of MapData.cs.
		returns a sub array based on the desired direction in order to match height.
	*/
	public Vector3[] GetVerticesPositiveX() {
		Vector3[] VertexArrayPositiveX = new Vector3[zTileSize];
		
		for (int i = 0; i < zTileSize; i++) {
			VertexArrayPositiveX[i] = verticeData[xTileSize - 1 ,i].vector;
			
		}
		return VertexArrayPositiveX;
	}

	public Vector3[] GetVerticesNegativeX() {
		Vector3[] VertexArrayNegativeX = new Vector3[zTileSize];
		
		for (int i = 0; i < zTileSize; i++) {
			VertexArrayNegativeX[i] = verticeData[0,i].vector;
			
		}
		return VertexArrayNegativeX;
	}

	public Vector3[] GetVerticesPositiveZ() {
		Vector3[] VertexArrayPositiveZ = new Vector3[xTileSize];
		
		for (int i = 0; i < xTileSize; i++) {
			VertexArrayPositiveZ[i] = verticeData[i,zTileSize - 1].vector;
			
		}
		return VertexArrayPositiveZ;
	}

	public Vector3[] GetVerticesNegativeZ() {
		Vector3[] VertexArrayNegativeZ = new Vector3[xTileSize];
		
		for (int i = 0; i < xTileSize; i++) {
			VertexArrayNegativeZ[i] = verticeData[i,0].vector;
			
		}
		return VertexArrayNegativeZ;
	}

	//---> GET CORNERS <---//
	public Vector3 GetVerticeLowerLeft() {
		Vector3 CornerVertice = new Vector3();
		CornerVertice = verticeData[0,0].vector;

		return CornerVertice;
	}
	public Vector3 GetVerticeLowerRight() {
		Vector3 CornerVertice = new Vector3();
		CornerVertice = verticeData[xTileSize - 1,0].vector;

		return CornerVertice;
	}
	public Vector3 GetVerticeUpperLeft() {
		Vector3 CornerVertice = new Vector3();
		CornerVertice = verticeData[0,zTileSize - 1].vector;

		return CornerVertice;
	}
	public Vector3 GetVerticeUpperRight() {
		Vector3 CornerVertice = new Vector3();
		CornerVertice = verticeData[xTileSize - 1,zTileSize - 1].vector;

		return CornerVertice;
	}




	/*
		AttachCurrentVertices
		======================
		Functions called in MapData.cs after GetVertice returns the desired arrays of vertices to match up.
	*/

	//Positive X attachment
	public void AttachCurrentVerticesToPositiveX(Vector3[] vectorArray) {

		//overwrite vertices2d
		for (int z = 0; z < zTileSize; z++) {
			vectorArray[z] = new Vector3(0,vectorArray[z].y,vectorArray[z].z);
			verticeData[0, z].vector = vectorArray[z];
		}
		
	}

	//Negative X attachment
	public void AttachCurrentVerticesToNegativeX(Vector3[] vectorArray) {

		//overwrite vertices2d
		for (int z = 0; z < zTileSize; z++) {
			vectorArray[z] = new Vector3(xTileSize - 1,vectorArray[z].y,vectorArray[z].z);
			verticeData[xTileSize - 1, z].vector = vectorArray[z];
		}
		
	}

	//Positive Z attachment
	public void AttachCurrentVerticesToPositiveZ(Vector3[] vectorArray) {

		//overwrite vertices2d
		for (int x = 0; x < xTileSize; x++) {
			vectorArray[x] = new Vector3(vectorArray[x].x,vectorArray[x].y,0);
			verticeData[x, 0].vector = vectorArray[x];
		}
		
	}

	//Negative Z attachment
	public void AttachCurrentVerticesToNegativeZ(Vector3[] vectorArray) {

		//overwrite vertices2d
		for (int x = 0; x < xTileSize; x++) {
			vectorArray[x] = new Vector3(vectorArray[x].x,vectorArray[x].y,zTileSize - 1);
			verticeData[x, zTileSize - 1].vector = vectorArray[x];
		}
		
	}



}
