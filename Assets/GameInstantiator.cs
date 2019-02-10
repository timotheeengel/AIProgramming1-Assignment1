using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class GameInstantiator : MonoBehaviour
{
    [SerializeField] private GameObject tile_prefab_ = null;
    [SerializeField] private GameObject sheep_prefab_ = null;
    [SerializeField] private GameObject wolf_prefab_ = null;
    
    // https://unity3d.com/learn/tutorials/modules/intermediate/scripting/lists-and-dictionaries
    // https://docs.unity3d.com/ScriptReference/Hashtable.html
    // https://forum.unity.com/threads/solved-question-about-dictionary-any-vs-dictionary-containskey.589939/
    
    private int grid_width_ = 36;
    private int grid_height_ = 20;

    private int amount_of_sheep_ = 80;
    private int amount_of_wolves_ = 8;

    private int ratio_of_grass_to_dirt_ = 30; // percent of the field covered in grass

    private GameObject field_;
    private GameObject herd_;
    private GameObject pack_;

    private List<GroundTile> active_tiles_ = new List<GroundTile>();
    private List<Sheep> active_sheep_ = new List<Sheep>();
    private List<Wolf> active_wolves_ = new List<Wolf>();
    
    
    private void Awake()
    {
        CreateField();
    }

    void Start()
    {
        Sheep.onSheepAddedToGame += AddSheepToActiveAgentsList;
        Sheep.onSheepDeletedFromGame += RemoveSheepFromActiveAgentsList;        
        SpawnSheep();
        // TODO: implement SpawnWolves()

        Wolf.onWolfAddedToGame += AddWolfToActiveAgentsList;
        Wolf.onWolfDeletedFromGame += RemoveWolfFromActiveAgentsList;
        SpawnWolves();
    }

    // Update is called once per frame
    void Update()
    {
        RunGrassBehaviour();
        RunSheepBehaviour();
        RunWolfBehaviour();
    }

    
    void RunGrassBehaviour()
    {
        foreach (var tile in active_tiles_)
        {
            tile.Tick();
        }
    }

    void RunSheepBehaviour()
    {
        List<Sheep> sheep_to_run_behaviour_on = new List<Sheep>(active_sheep_);
        
        foreach (var sheep in sheep_to_run_behaviour_on)
        {
            sheep.Tick();
        }   
    }

    void AddSheepToActiveAgentsList(Sheep sheep)
    {
        active_sheep_.Add(sheep);
    }

    void RemoveSheepFromActiveAgentsList(Sheep sheep)
    {
        active_sheep_.Remove(sheep);
    }
    
    void RunWolfBehaviour()
    {
        List<Wolf> wolves_to_run_behaviour_on = new List<Wolf>(active_wolves_);

        foreach (var wolf in wolves_to_run_behaviour_on)
        {
            wolf.Tick();
        }
    }
    
    void AddWolfToActiveAgentsList(Wolf wolf)
    {
        active_wolves_.Add(wolf);
    }

    void RemoveWolfFromActiveAgentsList(Wolf wolf)
    {
        active_wolves_.Remove(wolf);
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
                GameObject temp_tile_obj = Instantiate(tile_prefab_, current_grid_pos, Quaternion.identity, field_.transform);
                GroundTile temp_tile_ref = temp_tile_obj.GetComponent<GroundTile>();
                
                int isTileSeeded = Random.Range(0, 100);
                if(isTileSeeded < ratio_of_grass_to_dirt_) temp_tile_ref.PlantSeed();
                
                    active_tiles_.Add(temp_tile_ref);
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
        Vector2 spawn_pos;
        while (spawned_sheep < amount_of_sheep_)
        {
            spawn_pos = new Vector2(Random.Range(0, grid_width_), Random.Range(0, grid_height_));
            if (GroundTile.TileDictionary.ContainsKey(spawn_pos) && !Sheep.sheep_db_.ContainsKey(spawn_pos))
            {
                GameObject tempRef = Instantiate(sheep_prefab_, new Vector3(spawn_pos.x, spawn_pos.y, 0), Quaternion.identity, herd_.transform);
                tempRef.GetComponent<Sheep>().BirthSheep(spawn_pos);
                spawned_sheep++;
            }
        }
        Sheep.SetSheepPrefab(sheep_prefab_);
    }

    void SpawnWolves()
    {
        pack_ = new GameObject("Pack");
        pack_.transform.position = Vector3.zero;

        int spawned_wolves = 0;

        Vector2 spawn_pos;
        
        while (spawned_wolves < amount_of_wolves_)
        {
            spawn_pos = new Vector2(Random.Range(0, grid_width_), Random.Range(0, grid_height_));
            if (GroundTile.TileDictionary.ContainsKey(spawn_pos) && !Sheep.sheep_db_.ContainsKey(spawn_pos) && !Wolf.wolf_db_.ContainsKey(spawn_pos))
            {
                GameObject tempRef = Instantiate(wolf_prefab_, new Vector3(spawn_pos.x, spawn_pos.y, 0), Quaternion.identity, pack_.transform);
                tempRef.GetComponent<Wolf>().BirthWolf(spawn_pos);
                spawned_wolves++;
            }
        }
        
        Wolf.SetWolfPrefab(wolf_prefab_);
        pack_.transform.Translate(-Vector3.forward);
    }
}
