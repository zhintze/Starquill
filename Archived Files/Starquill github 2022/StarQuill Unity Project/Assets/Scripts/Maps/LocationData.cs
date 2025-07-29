using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationData
{
    GameData gameData;
    public bool isHostile;
    public string name;
    public int image;
    public List<LocationData> EmbeddedLocations;
    public List<NPCData> NPCInhabitants;
    public StepData stepData;
    public Color color;


    public Vector2 tile;
    public Vector3 posInTile;


    

    public bool isHeightSet;
    public float height;

    //int saturationLimit = 2;
    //int saturationDistance = 2; // 2 = 5x5 square with currentTile as center


    public LocationData(bool isLocHostile, Vector2 tileSpawned, int mapSize) {
        gameData = GameData.localData;
        tile = tileSpawned;
        isHostile = isLocHostile;
        NPCInhabitants = new List<NPCData>();

        //posInTile = new Vector3(Random.Range(0, mapSize*9+1),0,Random.Range(0, mapSize*9+1));
        //avoid edges version
        posInTile = new Vector3(Random.Range(60, mapSize*9-60),0,Random.Range(60, mapSize*9-60));

        name = GenerateRandomName();
        if (isHostile == false) {
            image = Random.Range(0,2);

            //if (Random.Range(0,2) == 0) {
                NPCData questGiver = new NPCData(); 
                questGiver.questGiverData = new QuestGiverData(questGiver,this,false);
                NPCInhabitants.Add(questGiver);

                NPCData nonQuest = new NPCData();
                NPCInhabitants.Add(nonQuest);
            //} else {
                //add interactable object for location quest
            //}


        } else {
            //image = Random.Range(1,gameData.locationImages.Length);


        }
        
    }


    public string GenerateRandomName() { 
        string[] LocArray = gameData.csvCityNames.text.Split(char.Parse("\n"));

        string chosenName = LocArray[Random.Range(0,LocArray.Length)];
        return chosenName;
    }







    /*public void PlaceLocationDataBasedOnTile(int currentTileX, int currentTileY) {
        GameStateData gameData = gameData;
        MapData overworld = gameData.mapArray[0];


        
        int saturationCount = 0;


        float [,] percentArray = new float[overworld.mapSize,overworld.mapSize];

        //amount of times the loop must be performed to cover the entire map
        int radiusLoops = overworld.mapSize/4;

        List<Vector2> selectedTiles = new List<Vector2>();
        List<float> tilePercentage = new List<float>();
        Debug.Log("currentX: "+gameData.currentTileX+" currentY: "+gameData.currentTileY);

        //-- loop checks saturationCount within saturationLimit inside of each radius to determine if tile can be randomly selected to spawn a town
        for (int radius = 1; radius < 4; radius++) {
            
            for (int x = gameData.currentTileX-saturationDistance*radius; x <= gameData.currentTileX+saturationDistance*radius; x++) {
                if ((radius != 1 && x > gameData.currentTileX-saturationDistance*radius + saturationDistance && x < gameData.currentTileX+saturationDistance*radius - saturationDistance) ||
                    (x < 0 || x >= overworld.mapSize)) {
                    //not within current radius
                } else {
                    for (int y = gameData.currentTileY-saturationDistance*radius; y <= gameData.currentTileY+saturationDistance*radius; y++) {
                        if ((radius != 1 && y > gameData.currentTileY-saturationDistance*radius + saturationDistance && y < gameData.currentTileY+saturationDistance*radius - saturationDistance) ||
                                (y < 0 || y >= overworld.mapSize)) {
                            //not within current radius
                        } else {
                            //if locationData is null, check saturationCount of tile
                            //if saturationCount < Limit then apply percentage change for spawning to tile
                            
                            if (overworld.tileArray[x][y].locationData == null) {
                                for (int a = x-saturationDistance; a <= x+saturationDistance; a++) {
                                    for (int b = y-saturationDistance; b <= y+saturationDistance; b++) {
                                        
                                        if (a < 0 || a >= overworld.mapSize || b < 0 || b >= overworld.mapSize) {
                                            //do nothing
                                        } else {
                                            if (overworld.tileArray[a][b].locationData != null) {
                                                saturationCount++;
                                            }
                                        }
                                        
                                    }
                                }

                                if (saturationCount < saturationLimit) {
                                    //tiledata is valid for placing based on radius
                                    
                                    selectedTiles.Add(new Vector2(x,y));
                                    float percentChance = 0;
                                    if (radius == 0) {
                                        percentChance = 100;
                                    } else {
                                        percentChance = 40.8f - 10*radius;
                                        if (percentChance < 0) {
                                            percentChance = 0;
                                        }
                                    }
                                    tilePercentage.Add(percentChance);
                                }
                                saturationCount = 0;
                            }
                        }
                    }
                }
            }
        }

        Vector2 selectedTile;
        //cycle through selectedTiles based on percentage and select one
        int randomTile; 

        bool isTileSelected = false;
        int infLoopProtection = 0;
        //if tile percentage is less than chosen percentage use selected tile.
        do {
            randomTile = Random.Range(0,selectedTiles.Count-1);
            Debug.Log("randomTile num: "+randomTile);
            if (tilePercentage[randomTile] <= Random.Range(0,100)) {
                selectedTile = selectedTiles[randomTile];
                overworld.tileArray[(int)selectedTile.x][(int)selectedTile.y].locationData = this;
                overworld.tileArray[(int)selectedTile.x][(int)selectedTile.y].isGateway = true;

                AssignColor(overworld.tileArray[(int)selectedTile.x][(int)selectedTile.y]);

                isTileSelected = true;
                Debug.Log("town x: "+selectedTile.x+" y: "+selectedTile.y);
            }
            infLoopProtection++;
            if (infLoopProtection >= 10000) {
                Debug.Log("infLoopProtection triggered");
                isTileSelected = true;
            }
        } while(isTileSelected == false);
        



    }*/







    /*public bool DiscoveryLocationDataBasedOnTile(int currentTileX, int currentTileY) {
        GameStateData gameData = gameData;
        MapData overworld = gameData.mapArray[0];


        int saturationCount = 0;


        float [,] percentArray = new float[overworld.mapSize,overworld.mapSize];

        //amount of times the loop must be performed to cover the entire map
        int radiusLoops = overworld.mapSize/4;

        List<Vector2> selectedTiles = new List<Vector2>();
        Debug.Log("currentX: "+gameData.currentTileX+" currentY: "+gameData.currentTileY);

        //-- loop checks saturationCount within saturationLimit inside of each radius to determine if tile can be randomly selected to spawn a town
            
            for (int x = gameData.currentTileX-saturationDistance; x <= gameData.currentTileX+saturationDistance; x++) {
                if ((x > gameData.currentTileX-saturationDistance + saturationDistance && x < gameData.currentTileX+saturationDistance - saturationDistance) ||
                    (x < 0 || x >= overworld.mapSize)) {
                    //not within current radius
                } else {
                    for (int y = gameData.currentTileY-saturationDistance; y <= gameData.currentTileY+saturationDistance; y++) {
                        if ((y > gameData.currentTileY-saturationDistance + saturationDistance && y < gameData.currentTileY+saturationDistance - saturationDistance) ||
                                (y < 0 || y >= overworld.mapSize) || 
                                (gameData.currentTileX == x && gameData.currentTileY == y)) {
                            //not within current radius
                        } else {
                            //if locationData is null, check saturationCount of tile
                            //if saturationCount < Limit then apply percentage change for spawning to tile
                            
                            if (overworld.tileArray[x][y].locationData == null) {
                                for (int a = x-saturationDistance; a <= x+saturationDistance; a++) {
                                    for (int b = y-saturationDistance; b <= y+saturationDistance; b++) {
                                        
                                        if (a < 0 || a >= overworld.mapSize || b < 0 || b >= overworld.mapSize) {
                                            //do nothing
                                        } else {
                                            if (overworld.tileArray[a][b].locationData != null) {
                                                saturationCount++;
                                            }
                                        }
                                        
                                    }
                                }

                                if (saturationCount < saturationLimit) {
                                    //tiledata is valid for placing based on radius
                                    
                                    selectedTiles.Add(new Vector2(x,y));
                                    
                                }
                                saturationCount = 0;
                            }
                        }
                    }
                }
            }


        if (selectedTiles.Count > 0) {
            Vector2 selectedTile;
            //cycle through selectedTiles based on percentage and select one
            int randomTile; 

        
            randomTile = Random.Range(0,selectedTiles.Count-1);
            selectedTile = selectedTiles[randomTile];
            overworld.tileArray[(int)selectedTile.x][(int)selectedTile.y].locationData = this;
            overworld.tileArray[(int)selectedTile.x][(int)selectedTile.y].isGateway = true;
            AssignColor(overworld.tileArray[(int)selectedTile.x][(int)selectedTile.y]);
            
            Debug.Log("town x: "+selectedTile.x+" y: "+selectedTile.y);
            return true;
        } else {
            return false;
        }
        
    }*/



    


    public void AssignColor(TileData tileData) {
        if (tileData.terrain == 0) {
            color = HexColorConvert.Parse(gameData.colorCodes.terrainColorsArray[tileData.terrain][Random.Range(0,gameData.colorCodes.terrainColorsArray[tileData.terrain].Length)]);
        }
    }
}
