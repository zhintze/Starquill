using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapData
{
    GameData gameData;

	public string name;
	public TileData[,] dataTileArray;
	
	public Vector3 triangleScale;
	public int xMapSize;
	public int zMapSize;

	public Vector2 entryPosition;
	public Vector3 entryPosWithinTile;


    public MapData(int xzSize, Vector3 mapTriangleScale) {

		gameData = GameData.localData;
        entryPosition = new Vector2(6, 6);
		entryPosWithinTile = new Vector3(2,100,10);
		triangleScale = mapTriangleScale;
		xMapSize = zMapSize = xzSize;


		//creates TileData for each tile in the map
		dataTileArray = new TileData[xMapSize,zMapSize];
		for (int x = 0; x < xMapSize; x++) {
			for (int z = 0; z < zMapSize; z++) {
				dataTileArray[x,z] = new TileData(this);
			}
		}

		//Generate Vertices for mesh and set other properties for each TileData
		for (int x = 0; x < xMapSize; x++) {
			for (int z = 0; z < zMapSize; z++) {

				// min and max height of each tile set here (not including zone modification)
				//maybe change to biom settings
				dataTileArray[x,z].GenerateTileVertices(dataTileArray[x,z].verticeData,-1,1);
				//dataTileArray[x,z].biom
				//dataTileArray[x,z].color


			}
		}


		//////////////////////////////////////////////
		//		Add Known Locations Here			//
		//////////////////////////////////////////////
		dataTileArray[6,7].locationData = new LocationData(false,new Vector2(6,7),xMapSize);




		//////////////////////////////////////////////
		//			Designate Zones Here			//
		//////////////////////////////////////////////
		int zoneID = -1;
		ZoneData zoneTest = new ZoneData(dataTileArray, zoneID++, "Flat", new Vector2(1,0));
		ZoneData zoneTest3 = new ZoneData(dataTileArray, zoneID++, "Mountainous", new Vector2(0,0));



		TileEdgeMatching();


	}








	

	/*
		Tile Edge Matching
		===================
		Matches height of each vertice at the edges of each tile.
		Prevents holes in map by making sure all vertices connect properly.
	*/
	void TileEdgeMatching() {
		
		//Connect all edges of each tile's height
		for (int x = 0; x < xMapSize; x++) {
			for (int z = 0; z < zMapSize; z++) {
				//if the adjacent tiles exist, call the proper attachment code
					Vector3[] tempVectorArray;

					//Middle Left
					if (x-1 >= 0) {
						tempVectorArray = dataTileArray[x-1,z].GetVerticesPositiveX();
						dataTileArray[x,z].AttachCurrentVerticesToPositiveX(tempVectorArray);
						
					}
					//Middle Right
					if (x+1 < xMapSize) {
						tempVectorArray = dataTileArray[x+1,z].GetVerticesNegativeX();
						dataTileArray[x,z].AttachCurrentVerticesToNegativeX(tempVectorArray);
					}

					//Lower Middle
					if (z-1 >= 0) {
						tempVectorArray = dataTileArray[x,z-1].GetVerticesPositiveZ();
						dataTileArray[x,z].AttachCurrentVerticesToPositiveZ(tempVectorArray);
					}
					
					//Upper Middle
					if (z+1 < zMapSize) {
						tempVectorArray = dataTileArray[x,z+1].GetVerticesNegativeZ();
						dataTileArray[x,z].AttachCurrentVerticesToNegativeZ(tempVectorArray);
					}

			}
		}

		//Globe Effect: Connect the ends[xMapSize,zMapSize] of the earth to the beginning[0,0].
		Vector3[] tempVectorArray2;

		for (int x = 0; x < xMapSize; x++) {
			tempVectorArray2 = dataTileArray[x,zMapSize-1].GetVerticesPositiveZ();
			dataTileArray[x,0].AttachCurrentVerticesToPositiveZ(tempVectorArray2);
		}
		for (int z = 0; z < zMapSize; z++) {
			tempVectorArray2 = dataTileArray[xMapSize-1,z].GetVerticesPositiveX();
			dataTileArray[0,z].AttachCurrentVerticesToPositiveX(tempVectorArray2);
		}



		//Corners of 4 vertices must be matched to one of the edge's heights
		VerticeData chosen4CornerVertice;
		int randomNum;
		for (int x = 0; x+1 < xMapSize; x++) {
			for (int z = 0; z+1 < zMapSize; z++) {

				//Randomly choose one vertice of the four corners and match the rest to the chosen one's height.
				int sizeX = dataTileArray[x,z].xTileSize;
				int sizeZ = dataTileArray[x,z].zTileSize;
				VerticeData cornerBottomLeft = dataTileArray[x,z].verticeData[sizeX-1,sizeZ-1];
				sizeX = dataTileArray[x,z+1].xTileSize;
				VerticeData cornerBottomRight = dataTileArray[x,z+1].verticeData[sizeX-1,0];
				sizeZ = dataTileArray[x+1,z].zTileSize;
				VerticeData cornerTopLeft = dataTileArray[x+1,z].verticeData[0,sizeZ-1];
				VerticeData cornerTopRight = dataTileArray[x+1,z+1].verticeData[0,0];



				randomNum = UnityEngine.Random.Range(0,4);
				if (randomNum == 0) {
					chosen4CornerVertice = cornerBottomLeft;

				} else if (randomNum == 1) {
					chosen4CornerVertice = cornerTopLeft;

				} else if (randomNum == 2) {
					chosen4CornerVertice = cornerBottomRight;

				} else if (randomNum == 3) {
					chosen4CornerVertice = cornerTopRight;
					
				} else {
					return;
				}
				cornerBottomLeft.vector.y = chosen4CornerVertice.vector.y;
				cornerTopLeft.vector.y = chosen4CornerVertice.vector.y;
				cornerBottomRight.vector.y = chosen4CornerVertice.vector.y;
				cornerTopRight.vector.y = chosen4CornerVertice.vector.y;

			}
			
		}
	}

    
}
