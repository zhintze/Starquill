using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class MapRenderer : MonoBehaviour
{
    GameData gameData;

	Vector3 triangleScale;
	public GameObject tileRendererPrefab;


	//player and camera vars
	[HideInInspector] public GameObject playerObject;
	public CinemachineVirtualCamera playerFollowCamera;
	public GameObject FirstPersonPrefab;


	
	[HideInInspector] public int xMapSize;
	[HideInInspector] public int zMapSize;
	public int renderWidth = 5;
	//public InventoryUI inventoryMenu;

	
	private MapData map;
	private GameObject[,] tileObjectArray;

	private int halfRenderWidth;



	Vector2 player_currentTile;
	Vector3 player_currentPosInTile;



	private int mapScale;
	private int xValue; // for cycling through tileObject Arrays 
	private int zValue; // for cycling through tileObject Arrays 
	private int xMapLocation;
	private int zMapLocation;

	private int xNegative;
	private int zNegative;
	private int xPrevTruePOS;
	private	int zPrevTruePOS;
	//represents tile as it spawns locally
	private	int xTileTruePOS = 0; 
	private	int zTileTruePOS = 0;
	//represents actual tile's position on flat globe
	private	int xTileLocation = 0; 
	private	int zTileLocation = 0;


	public Animator encounterAnim;



	void Start () {
		
		Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        

		gameData = GameData.localData;
		
		
		RenderMap(gameData.mapArray[gameData.currentMap]);

		gameData.isControlRestricted = true;
		encounterAnim.gameObject.SetActive(true);
		StartCoroutine("EnterWorldAnim");

	}



	void RenderMap(MapData dataMap) {
		//Create New MapData
		xMapSize = dataMap.xMapSize;
		zMapSize = dataMap.zMapSize;
		map = dataMap;
		
		triangleScale = map.triangleScale;


		// create object array with the same size as the data
		tileObjectArray = new GameObject[xMapSize,zMapSize];

		
		mapScale = (int)triangleScale.x*9;

		//force renderWidth to an Odd Number
		if (renderWidth%2 == 0) {
			renderWidth += 1;
		}
		halfRenderWidth = (renderWidth - 1) / 2;

		//init tile rendering variables for InitRender and Update functions
		xMapLocation = zMapLocation = xPrevTruePOS = zPrevTruePOS = xTileTruePOS = zTileTruePOS = xTileLocation = zTileLocation = 0;

		InitRender();
	}






	void SpawnPlayer() {

		playerObject = Instantiate(FirstPersonPrefab,gameData.mapArray[gameData.currentMap].entryPosWithinTile,Quaternion.identity);
		playerFollowCamera.Follow = playerObject.transform.GetChild(0);

		//setup weapons
		//GameObject weaponObject = player.transform.GetChild(0).GetChild(0).gameObject;
        //weaponObject.GetComponent<WeaponController>().UpdateWeapons();

	}







	void SpawnLocation(GameObject parent, TileData tile,Transform tilePos) {
		
		//set location position within tile
		Vector3 LocationPos;
		if (tile.locationData.isHeightSet == true) {
			LocationPos = new Vector3(tile.locationData.posInTile.x + tilePos.position.x, tile.locationData.height, tile.locationData.posInTile.z + tilePos.position.z);
		} else {
			LocationPos = new Vector3(tile.locationData.posInTile.x + tilePos.position.x, 100, tile.locationData.posInTile.z + tilePos.position.z);
		}
		 

		//create locationObject
		GameObject newLocation = Instantiate(gameData.locationObjectPrefabs[0]);
		newLocation.transform.SetParent(parent.transform,true);
		newLocation.GetComponent<LocationObject>().locationData = tile.locationData;
		newLocation.transform.position = LocationPos;


		//drop the ball
		if (tile.locationData.isHeightSet == false) {
			GameObject heightCheckObject = Instantiate(gameData.heightCheckObject);
			heightCheckObject.GetComponent<HeightCheck>().SetLocation(tile.locationData,newLocation);
			heightCheckObject.transform.position = newLocation.transform.position;

		}
		
	}


	





	void BuildZone(ZoneData currentZone, int xArrayLocation, int zArrayLocation) {

		if (currentZone.zoneType == "Town") {
			/*for (int i = 0; i < map.dataTileArray[xArrayLocation,zArrayLocation].objectPositionArray.Count; i++) {
				Vector3 objectVector = new Vector3(map.dataTileArray[xArrayLocation,zArrayLocation].objectPositionArray[i].x + tileObjectArray[xArrayLocation,zArrayLocation].transform.position.x, 100,
																map.dataTileArray[xArrayLocation,zArrayLocation].objectPositionArray[i].z + tileObjectArray[xArrayLocation,zArrayLocation].transform.position.z);
						
				GameObject zoneObject = Instantiate(plainsObjectSet[map.dataTileArray[xArrayLocation,zArrayLocation].objectTypeArray[i]], objectVector, Quaternion.identity); //vector3 position from array

				//shoot raycast to detect height
				RaycastHit heightHit;
				Physics.Raycast(zoneObject.transform.position, Vector3.down, out heightHit, 300);
				zoneObject.transform.position = new Vector3(objectVector.x, heightHit.point.y, objectVector.z);

				//set parent as tileObject so it gets deleted properly
				zoneObject.transform.parent = tileObjectArray[xArrayLocation,zArrayLocation].transform;
				zoneObject.transform.localScale = new Vector3(.05f,.05f,.05f);
			}*/

		} else if (currentZone.zoneType == "Plains") {
					/*for (int i = 0; i < map.dataTileArray[xArrayLocation,zArrayLocation].objectPositionArray.Count; i++) {

						Vector3 objectVector = new Vector3(map.dataTileArray[xArrayLocation,zArrayLocation].objectPositionArray[i].x + tileObjectArray[xArrayLocation,zArrayLocation].transform.position.x, 100,
																map.dataTileArray[xArrayLocation,zArrayLocation].objectPositionArray[i].z + tileObjectArray[xArrayLocation,zArrayLocation].transform.position.z);
						
						GameObject zoneObject = Instantiate(plainsObjectSet[map.dataTileArray[xArrayLocation,zArrayLocation].objectTypeArray[i]],objectVector, Quaternion.identity); //vector3 position from array

						//shoot raycast to detect height
						RaycastHit heightHit;
						Physics.Raycast(zoneObject.transform.position, Vector3.down, out heightHit, 300);
						zoneObject.transform.position = new Vector3(objectVector.x, heightHit.point.y, objectVector.z);

						//set parent as tileObject so it gets deleted properly
						zoneObject.transform.parent = tileObjectArray[xArrayLocation,zArrayLocation].transform;
						zoneObject.transform.localScale = new Vector3(.05f,.05f,.05f);


					}*/
		}
	}


	IEnumerator EnterWorldAnim() {
		yield return new WaitForSeconds(4);
        encounterAnim.SetTrigger("EncounterReverse");
		yield return new WaitForSeconds(.6f);
		gameData.isControlRestricted = false;
		encounterAnim.gameObject.SetActive(false);

	}



	/*
		Update Function
		================
		Determines Player's current Tile, then renders and removes surrounding tiles in order to keep a 9x9 square around the Player
	*/
	void Update() {
		//collect position in tile each iteration
		player_currentPosInTile = new Vector3(playerObject.transform.position.x / (int)triangleScale.x, 
												playerObject.transform.position.y / (int)triangleScale.y, 
												playerObject.transform.position.z / (int)triangleScale.z);

		//represents tile as it spawns locally in Unity Space
		xTileTruePOS = 0; 
		zTileTruePOS = 0;
		//represents actual tile's position on Globe
		xTileLocation = (int)map.entryPosition.x; 
		zTileLocation = (int)map.entryPosition.y;


		// Find Current Player's Tile
		// X POS > 9
		while (player_currentPosInTile.x > 9) {
			player_currentPosInTile.x -= 9;
			xTileTruePOS += 1;
			xTileLocation += 1;
			if (xTileLocation >= xMapSize) {
				xTileLocation = xTileLocation - xMapSize;
			}
		}
		// X POS < 0
		while (player_currentPosInTile.x < 0) {
			player_currentPosInTile.x += 9;
			xTileTruePOS -= 1;
			xTileLocation -= 1;
			if (xTileLocation < 0) {
				xTileLocation = xTileLocation + xMapSize;
			}
			
			
		}
		// Z POS > 9
		while (player_currentPosInTile.z > 9) {
			player_currentPosInTile.z -= 9;
			zTileTruePOS += 1;
			zTileLocation += 1;
			if (zTileLocation >= zMapSize) {
				zTileLocation = zTileLocation - zMapSize;
			}
		}
		// Z POS < 0
		while (player_currentPosInTile.z < 0) {
			player_currentPosInTile.z += 9;
			zTileTruePOS -= 1;
			zTileLocation -= 1;
			if (zTileLocation < 0) {
				zTileLocation = zTileLocation + zMapSize;
				;

			}
		}
		// END Find Current gameData's Tile


		//X Axis  Movement From Tile Detected
		if (xTileLocation != player_currentTile.x) {
			
			player_currentTile.x = xTileLocation; 
			
			//render and destroy tiles based on prevTile
			if (xPrevTruePOS < xTileTruePOS) {
				DirectionalTileRender("positiveX");
			} else if (xPrevTruePOS > xTileTruePOS) {
				DirectionalTileRender("negativeX");
			}

			//store current POS for next Update() iteration
			xPrevTruePOS = xTileTruePOS;
		}
		
		//Z Axis Movement From Tile Detected
		if (zTileLocation != player_currentTile.y) {
			
			player_currentTile.y = zTileLocation;
			
			//render and destroy tiles based on prevTile
			if (zPrevTruePOS < zTileTruePOS) {
				DirectionalTileRender("positiveZ");
			} else if (zPrevTruePOS > zTileTruePOS) {
				DirectionalTileRender("negativeZ");
			}
			//store current POS for next Update() iteration
			zPrevTruePOS = zTileTruePOS;
		}



	}



	/*
		DirectionalTileRender
		=========================
		Used in Update().
		Renders and removes tiles based on the direction the Player has moved.
	*/
	void DirectionalTileRender(string direction) {


		int xTempTruePos = xTileTruePOS;
		int zTempTruePos = zTileTruePOS;

		int xArrayPos;
		int zArrayPos;

		//Store true POS (unity location) of tiles to be rendered.
		int[] xPositiveTruePOS = new int[halfRenderWidth];
		int[] xNegativeTruePOS = new int[halfRenderWidth];
		for (int i = 0; i < halfRenderWidth; i++) {
			xPositiveTruePOS[i] = xTileTruePOS + 1 + i;
		}
		for (int i = 0; i < halfRenderWidth; i++) {
			xNegativeTruePOS[i] = xTileTruePOS - 1 - i;
		}
		int[] zPositiveTruePOS = new int[halfRenderWidth];
		int[] zNegativeTruePOS = new int[halfRenderWidth];
		for (int i = 0; i < halfRenderWidth; i++) {
			zPositiveTruePOS[i] = zTileTruePOS + 1 + i;
		}
		for (int i = 0; i < halfRenderWidth; i++) {
			zNegativeTruePOS[i] = zTileTruePOS - 1 - i;
		}
		
		//Store array POS of tiles to be rendered.
		int[] xPositiveArrayPOS = new int[halfRenderWidth];
		int[] xNegativeArrayPOS = new int[halfRenderWidth];

		for (int i = 0; i < halfRenderWidth; i++) {
			xPositiveArrayPOS[i] = xTileLocation + 1 + i;
			if (xPositiveArrayPOS[i] >= xMapSize) {
			xPositiveArrayPOS[i] = xPositiveArrayPOS[i] - xMapSize;
			}
		}

		for (int i = 0; i < halfRenderWidth; i++) {
			xNegativeArrayPOS[i] = xTileLocation - 1 - i;
			if (xNegativeArrayPOS[i] < 0) {
			xNegativeArrayPOS[i] =  xMapSize + xNegativeArrayPOS[i];
			}
		}


		int[] zPositiveArrayPOS = new int[halfRenderWidth];
		int[] zNegativeArrayPOS = new int[halfRenderWidth];

		for (int i = 0; i < halfRenderWidth; i++) {
			zPositiveArrayPOS[i] = zTileLocation + 1 + i;
			if (zPositiveArrayPOS[i] >= zMapSize) {
			zPositiveArrayPOS[i] = zPositiveArrayPOS[i] - zMapSize;
			}
		}

		for (int i = 0; i < halfRenderWidth; i++) {
			zNegativeArrayPOS[i] = zTileLocation - 1 - i;
			if (zNegativeArrayPOS[i] < 0) {
			zNegativeArrayPOS[i] =  zMapSize + zNegativeArrayPOS[i];
			}
		}


		if (direction == "positiveX") {
			// X+ init POS Variables
			xTempTruePos = xTileTruePOS + halfRenderWidth;
			xArrayPos = xTileLocation + halfRenderWidth;
			zArrayPos = zTileLocation;
			
			// X+ Correct TruePosition to align with TileArray[x,z]
			if (xArrayPos > xMapSize - 1){
				xArrayPos = xArrayPos - xMapSize;
			}
			if (xArrayPos < 0){
				xArrayPos = xMapSize + xArrayPos;
			}

			// X+ upper tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xTempTruePos , zPositiveTruePOS[i], xArrayPos, zPositiveArrayPOS[i]);
			}

			// X+ center tile
			RenderTile(xTempTruePos ,zTempTruePos, xArrayPos, zArrayPos);
			
			// X+ lower tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xTempTruePos, zNegativeTruePOS[i], xArrayPos, zNegativeArrayPOS[i]);
			}


			// X+ Delete Tiles Outside Render Range 
			int xArrayDeletionPos = xTileLocation - halfRenderWidth - 1;
			
			while (xArrayDeletionPos < 0) {
				xArrayDeletionPos = xArrayDeletionPos + xMapSize;
			}

			// X+ Tile Deletion Loops
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xArrayDeletionPos,zPositiveArrayPOS[i]);
			}
			RemoveTile(xArrayDeletionPos,zArrayPos);
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xArrayDeletionPos, zNegativeArrayPOS[i]);
			}


		} else if (direction == "negativeX") {

			// X- init POS Variables
			xTempTruePos = xTileTruePOS - halfRenderWidth;
			xArrayPos = xTileLocation - halfRenderWidth;
			zArrayPos = zTileLocation;
			
			// X- Correct TruePosition to align with TileArray[x,z]
			if (xArrayPos < 0){
				xArrayPos = xMapSize + xArrayPos;
			}
			if (xArrayPos > xMapSize - 1){
				xArrayPos = xArrayPos - xMapSize;
			}
			
			// X- upper tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xTempTruePos , zPositiveTruePOS[i], xArrayPos, zPositiveArrayPOS[i]);
			}

			// X- center tile
			RenderTile(xTempTruePos ,zTempTruePos, xArrayPos, zArrayPos);
			
			// X- lower tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xTempTruePos, zNegativeTruePOS[i], xArrayPos, zNegativeArrayPOS[i]);
			}

			// X- Delete Tiles Outside Render Range 
			int xArrayDeletionPos = xTileLocation + halfRenderWidth + 1;

			while (xArrayDeletionPos > xMapSize - 1) {
				xArrayDeletionPos = xArrayDeletionPos - xMapSize;
			}

			// X- Tile Deletion Loops
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xArrayDeletionPos,zPositiveArrayPOS[i]);
			}
			RemoveTile(xArrayDeletionPos,zArrayPos);
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xArrayDeletionPos, zNegativeArrayPOS[i]);
			}
		
		} else if (direction == "positiveZ") {
			// Z+ init POS Variables
			zTempTruePos = zTileTruePOS + halfRenderWidth;
			zArrayPos = zTileLocation + halfRenderWidth;
			xArrayPos = xTileLocation;
			
			// Z+ Correct TruePosition to align with TileArray[x,z]
			if (zArrayPos > zMapSize - 1){
				zArrayPos = zArrayPos - zMapSize;
			}
			if (zArrayPos < 0){
				zArrayPos = zMapSize + zArrayPos;
			}
			
			// Z+ upper tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xPositiveTruePOS[i] , zTempTruePos, xPositiveArrayPOS[i], zArrayPos);
			}

			// Z+ center tile
			RenderTile(xTempTruePos ,zTempTruePos, xArrayPos, zArrayPos);
			
			// Z+ lower tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xNegativeTruePOS[i] , zTempTruePos, xNegativeArrayPOS[i], zArrayPos);
			}

			// Z+ Delete Tiles Outside Render Range 
			int zArrayDeletionPos = zTileLocation - halfRenderWidth - 1;
			while (zArrayDeletionPos < 0) {
				zArrayDeletionPos = zArrayDeletionPos + zMapSize;
			}

			// Z+ Tile Deletion Loops
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xPositiveArrayPOS[i],zArrayDeletionPos);
			}
			RemoveTile(xArrayPos,zArrayDeletionPos);
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xNegativeArrayPOS[i],zArrayDeletionPos);
			}

		} else if (direction == "negativeZ") {

			// Z- init POS Variables
			zTempTruePos = zTileTruePOS - halfRenderWidth;
			zArrayPos = zTileLocation - halfRenderWidth;
			xArrayPos = xTileLocation;
			
			// Z- Correct TruePosition to align with TileArray[x,z]
			if (zArrayPos < 0){
				zArrayPos = zMapSize + zArrayPos;
			}
			if (zArrayPos > zMapSize - 1){
				zArrayPos = zArrayPos - zMapSize;
			}


			// Z- upper tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile( xPositiveTruePOS[i], zTempTruePos, xPositiveArrayPOS[i], zArrayPos);
			}

			// Z- center tile
			RenderTile(xTempTruePos ,zTempTruePos, xArrayPos, zArrayPos);
			
			// Z- lower tiles
			for (int i = 0; i < halfRenderWidth; i++) {
				RenderTile(xNegativeTruePOS[i], zTempTruePos, xNegativeArrayPOS[i], zArrayPos);
			}
			

			// Z- Delete Tiles Outside Render Range 
			int zArrayDeletionPos = zTileLocation + halfRenderWidth + 1;

			while (zArrayDeletionPos > zMapSize - 1) {
				zArrayDeletionPos = zArrayDeletionPos - zMapSize;
			}

			// Z- Tile Deletion Loops
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xPositiveArrayPOS[i],zArrayDeletionPos);
			}
			RemoveTile(xArrayPos,zArrayDeletionPos);
			for (int i = 0; i < halfRenderWidth; i++) {
				RemoveTile(xNegativeArrayPOS[i],zArrayDeletionPos);
			}

		}


	}





	void RenderTile(int xNewLocation, int zNewLocation, int xArrayLocation, int zArrayLocation) {

		if (map.dataTileArray[xArrayLocation,zArrayLocation].isCurrentlyRendered == false) {
			
			map.dataTileArray[xArrayLocation,zArrayLocation].isCurrentlyRendered = true;

			//spawn a GameObject representing the Tile in Array Location [x,z]
			tileObjectArray[xArrayLocation,zArrayLocation] = Instantiate(tileRendererPrefab,new Vector3(xNewLocation*mapScale,0,zNewLocation*mapScale),Quaternion.identity);
			tileObjectArray[xArrayLocation,zArrayLocation].transform.parent = gameObject.transform;

			tileObjectArray[xArrayLocation,zArrayLocation].name = "Tile["+xArrayLocation+"]["+zArrayLocation+"]";

			TileRenderer renderedTile = tileObjectArray[xArrayLocation,zArrayLocation].GetComponent<TileRenderer>();
			renderedTile.RenderTile(map.dataTileArray[xArrayLocation,zArrayLocation], triangleScale);


			if (map.dataTileArray[xArrayLocation,zArrayLocation].parentZone != null) {
				
				BuildZone(map.dataTileArray[xArrayLocation,zArrayLocation].parentZone,xValue,zValue);
			}
			
			if (map.dataTileArray[xArrayLocation,zArrayLocation].locationData != null) {
				SpawnLocation(tileObjectArray[xArrayLocation,zArrayLocation],map.dataTileArray[xArrayLocation,zArrayLocation],renderedTile.transform);
			}

		}
	}

	

	void RemoveTile(int xArrayLocation, int zArrayLocation) {
		if (map.dataTileArray[xArrayLocation,zArrayLocation].isCurrentlyRendered == true) { 
			map.dataTileArray[xArrayLocation,zArrayLocation].isCurrentlyRendered = false;
			Destroy(tileObjectArray[xArrayLocation,zArrayLocation]);
			
		}
	}






    
void InitRender() {
		//--> CHUNK RENDERER <--//
		RemovePreviousRenderBools();

		xValue = (int)map.entryPosition.x;
		zValue = (int)map.entryPosition.y;
		xMapLocation = 0;
		zMapLocation = 0;

		RenderInitialTile();

		for (int z = 0; z < halfRenderWidth; z++) {
				zValue = zValue + 1;
				//xMapLocation = xMapLocation;
				zMapLocation = zMapLocation + 1;
				CalculateMapWrap("positiveZ");
				RenderInitialTile();	
		}
		
		xValue = (int)map.entryPosition.x;
		zValue = (int)map.entryPosition.y;
		xMapLocation = 0;
		zMapLocation = 0;
		for (int z = 0; z < halfRenderWidth; z++) {
				zValue = zValue - 1;
				//xMapLocation = xMapLocation;
				zMapLocation = zMapLocation - 1;
				CalculateMapWrap("negativeZ");
				RenderInitialTile();	
		}


		xValue = (int)map.entryPosition.x;
		zValue = (int)map.entryPosition.y;
		xMapLocation = 0;
		zMapLocation = 0;
		for (int z = 0; z <= halfRenderWidth; z++) {
			xValue = (int)map.entryPosition.x;
			xMapLocation = 0;
			for (int x = 0; x < halfRenderWidth; x++) {
					xValue = xValue + 1;
					xMapLocation = xMapLocation + 1;
					//zMapLocation = zMapLocation;
					CalculateMapWrap("positiveX");
					RenderInitialTile();	
			}
			zValue = zValue + 1;
			zMapLocation = zMapLocation + 1;
		}

		xValue = (int)map.entryPosition.x;
		zValue = (int)map.entryPosition.y;
		xMapLocation = 0;
		zMapLocation = 0;
		for (int z = 0; z <= halfRenderWidth; z++) {
			xValue = (int)map.entryPosition.x;
			xMapLocation = 0;
			for (int x = 0; x < halfRenderWidth; x++) {
					xValue = xValue + 1;
					xMapLocation = xMapLocation + 1;
					//zMapLocation = zMapLocation;
					CalculateMapWrap("positiveX");
					RenderInitialTile();	
			}
			zValue = zValue - 1;
			zMapLocation = zMapLocation - 1;
		}


		xValue = (int)map.entryPosition.x;
		zValue = (int)map.entryPosition.y;
		xMapLocation = 0;
		zMapLocation = 0;
		for (int z = 0; z <= halfRenderWidth; z++) {
			xValue = (int)map.entryPosition.x;
			xMapLocation = 0;
			for (int x = 0; x < halfRenderWidth; x++) {
					xValue = xValue - 1;
					xMapLocation = xMapLocation - 1;
					//zMapLocation = zMapLocation;
					CalculateMapWrap("negativeX");
					RenderInitialTile();	
			}
			zValue = zValue - 1;
			zMapLocation = zMapLocation - 1;
		}

		xValue = (int)map.entryPosition.x;
		zValue = (int)map.entryPosition.y;
		xMapLocation = 0;
		zMapLocation = 0;
		for (int z = 0; z <= halfRenderWidth; z++) {
			xValue = (int)map.entryPosition.x;
			xMapLocation = 0;
			for (int x = 0; x < halfRenderWidth; x++) {
					xValue = xValue - 1;
					xMapLocation = xMapLocation - 1;
					//zMapLocation = zMapLocation;
					CalculateMapWrap("negativeX");
					RenderInitialTile();	
			}
			zValue = zValue + 1;
			zMapLocation = zMapLocation + 1;
		}

		

		SpawnPlayer();
	}



//////////////--> Old InitRender <--/////////////
	

	void RenderInitialTile() {

		if (map.dataTileArray[xValue,zValue].isCurrentlyRendered == false) {

			map.dataTileArray[xValue,zValue].isCurrentlyRendered = true;
			tileObjectArray[xValue,zValue] = Instantiate(tileRendererPrefab,new Vector3(xMapLocation*mapScale,0,zMapLocation*mapScale),Quaternion.identity);
			tileObjectArray[xValue,zValue].transform.parent = gameObject.transform;
			TileRenderer renderedTile = tileObjectArray[xValue,zValue].GetComponent<TileRenderer>();
			renderedTile.RenderTile(map.dataTileArray[xValue,zValue], triangleScale);

			tileObjectArray[xValue,zValue].name = "Tile["+xValue+"]["+zValue+"]";

			//spawn any GameObjects that tagged in the Tile
			VerticeData[,] verticeDataArray = map.dataTileArray[xValue,zValue].verticeData;

			//BuildRegionSystem(xValue,zValue);
			//BuildCityAlpha(verticeDataArray,xValue,zValue);

			if (map.dataTileArray[xValue,zValue].parentZone != null) {
				BuildZone(map.dataTileArray[xValue,zValue].parentZone,xValue,zValue);
			}
			

			
			
			if (map.dataTileArray[xValue,zValue].locationData != null) {
				SpawnLocation(tileObjectArray[xValue,zValue],map.dataTileArray[xValue,zValue],renderedTile.transform);
			}
						
		}
	}



	void CalculateMapWrap(string direction) { 

		if (xValue < 0) {
			xValue = xMapSize + xValue;
		} else if (xValue >= xMapSize) {
			xValue = xValue - xMapSize;
		}

		if (zValue < 0) {
			zValue = zMapSize + zValue;
		} else if (zValue >= zMapSize) {
			zValue = zValue - zMapSize;
		}
	}


	void RemovePreviousRenderBools() {
		for (int x = 0; x < map.dataTileArray.GetLength(0); x++) {
			for (int y = 0; y < map.dataTileArray.GetLength(1); y++) {
				map.dataTileArray[x,y].isCurrentlyRendered = false;
			}
		}
	}



}
