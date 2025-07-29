using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneData
{
    //EACH ZONE MUST HAVE A UNIQUE ID 
	//SO DATACHUNKS CAN SAVE AND LOAD AND CALL FUNCTIONS STRAIGHT FROM THEIR PARENT ZONE

	///////////////////////////////////////////////
	////--> 	ZoneType Static Data 		<--////
	// (used with every Zone of the chosen type) //
	public string zoneType;
	public int id;
	//allowed min and max size of zoneType
	public int xMinTileSize;
	public int xMaxTileSize;
	public int zMinTileSize;
	public int zMaxTileSize;
	//available objects for chosen zoneType
	//public AuthorizedObject[] availableObjects;

	///////////////////////////////////
	//--> Zone Instance Variables <--//
	//where a particular Zone instance begins starting at bottom left
	public int xOriginTile;
	public int zOriginTile;
	//how many tiles
	public int xTilesWide;
	public int zTilesWide;

	public TileData[,] dataTileArray;


    
	public ZoneData(TileData[,] dataTiles ,int zoneID, string zoneTypeName, Vector2 startingTile) {
		id = zoneID;
		dataTileArray = dataTiles;

		xOriginTile = (int)startingTile.x;
		zOriginTile = (int)startingTile.y;

		zoneType = zoneTypeName;

		int xWorldSize = dataTileArray.GetLength(0);
		int zWorldSize = dataTileArray.GetLength(1);




        if (zoneType == "Test") {
			dataTiles[xOriginTile,zOriginTile].parentZone = this;
			//dataTiles[xOriginTile,zOriginTile].objectTypeArray.Add(0);
			//dataTiles[xOriginTile,zOriginTile].objectPositionArray.Add(new Vector3(Random.Range(0, xWorldSize*9+1),0,Random.Range(0, xWorldSize*9+1)));


		} else if (zoneType == "Flat") {
			int flatHeight = 0;

			xMinTileSize = 2;
			zMinTileSize = 2;

			xMaxTileSize = 2;
			zMaxTileSize = 2;

			xTilesWide = Random.Range(xMinTileSize,xMaxTileSize +1);
			zTilesWide = Random.Range(zMinTileSize,zMaxTileSize +1);

			for (int i = xOriginTile; i < xOriginTile + xTilesWide; i++) {
				int iWrapped = i;
				if (i > xWorldSize - 1) {
					iWrapped -= xWorldSize;
				}
				for (int j = zOriginTile; j < zOriginTile + zTilesWide; j++) {
					int jWrapped = j;
					if (j > zWorldSize - 1) {
					jWrapped -= zWorldSize;
					}

					//set parentZone of the coordinate to this Zone
					//dataTiles[iWrapped,jWrapped].parentZone = this; ????????
					
					//alter height of all vertices
					for (int a = 0; a < dataTiles[iWrapped,jWrapped].verticeData.GetLength(0); a++) {
						for (int b = 0; b < dataTiles[iWrapped,jWrapped].verticeData.GetLength(1); b++) {
							dataTiles[iWrapped,jWrapped].verticeData[a,b].vector.y = flatHeight;	
						}
					}

				}
			}


		} else if (zoneType == "Plains") {
			//set min size
			xMinTileSize = 1;
			zMinTileSize = 1;
			//set max size
			xMaxTileSize = 1;
			zMaxTileSize = 1;

			//int typesOfObjects = 4;
			//int amountOfObjects = 4;

			//objectTypeArray = new List<int>();
			//objectPositionArray = new List<Vector3>();

			//randomly choose size of zone
			xTilesWide = Random.Range(xMinTileSize,xMaxTileSize +1);
			zTilesWide = Random.Range(zMinTileSize,zMaxTileSize +1);

			//loop through all Tiles
			for (int i = xOriginTile; i < xOriginTile + xTilesWide; i++) {
				int iWrapped = i;
				if (i > xWorldSize - 1) {
					iWrapped -= xWorldSize;
				}
				for (int j = zOriginTile; j < zOriginTile + zTilesWide; j++) {
					int jWrapped = j;
					if (j > zWorldSize - 1) {
						jWrapped -= zWorldSize;
					}

					//dataTiles[iWrapped,jWrapped]
					//int randomAmount = Random.Range(amountOfObjects-2,amountOfObjects+1);
					//spawn randomAmount of Objects with random objectTypes to random objectPositions
					//for (int a = 0; a < randomAmount; a++) {
					//dataTiles[iWrapped,jWrapped].objectTypeArray.Add(0);
						// 451: magic number representing physical space a tile currently takes up
						//	replace with a tile size variable from parents
					//dataTiles[iWrapped,jWrapped].objectPositionArray.Add(new Vector3(Random.Range(0, xWorldSize*9+1),0,Random.Range(0, xWorldSize*9+1)));

					//}

				}
			}
			




		} else if (zoneType == "Mountainous") {
			xMinTileSize = 1;
			zMinTileSize = 1;

			xMaxTileSize = 4;
			zMaxTileSize = 4;

			xTilesWide = Random.Range(xMinTileSize,xMaxTileSize +1);
			zTilesWide = Random.Range(zMinTileSize,zMaxTileSize +1);
		
			//variables that change how the zone operates
			int mountainHeight = 2;
			int mountainDepth = 0;
			int tooHigh = 2;
			int peakChance = 3;


			for (int i = xOriginTile; i < xOriginTile + xTilesWide; i++) {
				int iWrapped = i;
				if (i > xWorldSize - 1) {
					iWrapped -= xWorldSize;
				}
				for (int j = zOriginTile; j < zOriginTile + zTilesWide; j++) {
					int jWrapped = j;
					if (j > zWorldSize - 1) {
					jWrapped -= zWorldSize;
					}

					for (int a = 0; a < dataTiles[iWrapped,jWrapped].verticeData.GetLength(0); a++) {
						for (int b = 0; b < dataTiles[iWrapped,jWrapped].verticeData.GetLength(1); b++) {
							int randomChoice;
							bool chosen = false;

							
							do {
									
								randomChoice = Random.Range(0,12);
								if (randomChoice == 0) {
									if (a != 0 && b != 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a-1,b-1].vector.y + mountainHeight;
										chosen = true;
									}

								} else if (randomChoice == 1) {
									if (a != 0 && b != 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a-1,b-1].vector.y - mountainDepth;
										chosen = true;
									}

								} else if (randomChoice == 2) {
									if (a != 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a-1,b].vector.y - mountainDepth;
										chosen = true;
									}

								} else if (randomChoice == 3) {
									if (a != 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a-1,b].vector.y + mountainHeight;
										chosen = true;
									}

								} else if (randomChoice == 4) {
									if (b != 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a,b-1].vector.y - mountainDepth;
										chosen = true;
									}

								} else if (randomChoice == 5) {
									if (b != 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a,b-1].vector.y + mountainHeight;
										chosen = true;
									}

								}  else if (randomChoice == 6) {
									if (a+1 < dataTiles[i,j].verticeData.GetLength(0) && b+1 < dataTiles[i,j].verticeData.GetLength(1)) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a+1,b+1].vector.y + mountainHeight;
										chosen = true;
									}

								}  else if (randomChoice == 7) {
									if (a+1 < dataTiles[i,j].verticeData.GetLength(0) && b+1 < dataTiles[i,j].verticeData.GetLength(1)) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a+1,b+1].vector.y - mountainDepth;
										chosen = true;
									}


								} else if (randomChoice == 8) {
									if (a+1 < dataTiles[i,j].verticeData.GetLength(0)) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a+1,b].vector.y + mountainHeight;
										chosen = true;
									}

								} else if (randomChoice == 9) {
									if (a+1 < dataTiles[i,j].verticeData.GetLength(0)) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a+1,b].vector.y - mountainDepth;
										chosen = true;
									}

								} else if (randomChoice == 10) {
									if (b+1 < dataTiles[i,j].verticeData.GetLength(1)) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a,b+1].vector.y + mountainHeight;
										chosen = true;
									}

								} else if (randomChoice == 11) {
									if (b+1 < dataTiles[i,j].verticeData.GetLength(1)) {
										dataTiles[i,j].verticeData[a,b].vector.y = dataTiles[i,j].verticeData[a,b+1].vector.y + mountainHeight;
										chosen = true;
									}

								}
								//reduce height if too high
								if (dataTiles[i,j].verticeData[a,b].vector.y > tooHigh) {
									randomChoice = Random.Range(0,peakChance);
									if (randomChoice == 0) {
										dataTiles[i,j].verticeData[a,b].vector.y = tooHigh;
									}
								}
							} while (chosen == false);
							
							//dataTiles[i,j].verticeData[a,b].vector.y = 1;
						
						}
						dataTiles[iWrapped,jWrapped].parentZone = this;
					}
				

				}
			}
		

		}

		//randomly assign size based on zoneType
		xTilesWide = Random.Range(xMinTileSize, xMaxTileSize + 1);
		zTilesWide = Random.Range(zMinTileSize, zMaxTileSize + 1);


		//check to see if it overlaps with an incompatible Zone
	
		
		//add ZoneType to all tiles between [xOriginTile, zOriginTileingTile] and [xTilesWide, zTilesWide]
		for (int i = xOriginTile; i < xTilesWide; i++) {
			for (int j = zOriginTile; j < zTilesWide; j++) {
				dataTileArray[i,j].parentZone = this;
			}
		}


	}



    
    
}
