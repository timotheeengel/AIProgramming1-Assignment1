﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInstantiator : MonoBehaviour
{
    [SerializeField] private GameObject tile_prefab_ = null;
    [SerializeField] private GameObject sheep_prefab_ = null;
    [SerializeField] private GameObject wolf_prefab_ = null;
    
    // https://unity3d.com/learn/tutorials/modules/intermediate/scripting/lists-and-dictionaries
    // https://docs.unity3d.com/ScriptReference/Hashtable.html
    // https://forum.unity.com/threads/solved-question-about-dictionary-any-vs-dictionary-containskey.589939/
    
    private int grid_width_ = 10;
    private int grid_height_ = 10;

    private int amount_of_sheep_ = 1;
    private int amount_of_wolves_ = 0;

    private int ratio_of_grass_to_dirt_ = 20; // percent of the field covered in grass

    private GameObject field_;
    private GameObject herd_;
    
    private void Awake()
    {
        CreateField();
    }

    void Start()
    {
        SpawnSheep();
        // TODO: implement SpawnWolves()
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateField()
    {
        field_ = new GameObject("Field");
        field_.transform.position = Vector3.zero;
        
        for (int i = 0; i < grid_width_; i++)
        {
            for (int j = 0; j < grid_height_; j++)
            {
                Vector3 current_grid_pos = new Vector3(i, j, 0);
                GameObject temp_tile = Instantiate(tile_prefab_, current_grid_pos, Quaternion.identity, field_.transform);
                
                // TODO: Investigate why it isn't "isTileSeeded < ration_of_grass_to_dirt_"
                int isTileSeeded = Random.Range(0, 100);
                if(isTileSeeded < ratio_of_grass_to_dirt_) temp_tile.GetComponent<GroundTile>().PlantSeed();
            }
        }
        
        // note: move field_ further into the background so sheep & wolves are "closer" to the camera and thus can be drawn on top of it
        field_.transform.Translate(Vector3.forward);
    }

    void SpawnSheep()
    {
        herd_ = new GameObject("Herd");
        herd_.transform.position = Vector3.zero;
     
        int spawned_sheep = 0;
        
        while (spawned_sheep < amount_of_sheep_)
        {
            Vector2 spawn_pos = new Vector2(Random.Range(0, grid_width_), Random.Range(0, grid_height_));
            if (GroundTile.TileDictionary.ContainsKey(spawn_pos) && !Sheep.sheep_db_.ContainsKey(spawn_pos))
            {
                GameObject tempRef = Instantiate(sheep_prefab_, new Vector3(spawn_pos.x, spawn_pos.y, 0), Quaternion.identity, herd_.transform);
                tempRef.GetComponent<Sheep>().BirthSheep(spawn_pos);
                spawned_sheep++;
            }
        }
        
        Sheep.SetSheepPrefab(sheep_prefab_);

        
        
    }
}
