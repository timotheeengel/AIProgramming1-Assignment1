using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;

public class Sheep : MonoBehaviour
{
    public static Dictionary<Vector2, Sheep> sheep_db_ = new Dictionary<Vector2, Sheep>();

    public delegate void OnSheepAddedToGame(Sheep sheep);
    public static event OnSheepAddedToGame onSheepAddedToGame;

    public delegate void OnSheepDeletedFromGame(Sheep sheep);
    public static event OnSheepDeletedFromGame onSheepDeletedFromGame;
    
    private static GameObject sheep_prefab_ = null;
    private static List<GameObject> sheep_object_pool_ = new List<GameObject>();
    private static Vector2 object_pool_pos_ = new Vector2(-100, -100);


    
    // TODO: Use life states (e.g. alive, dead, decomposing, etc...) for attracting wolves and plant seeds upon decomposition
    // TODO: Clean up Move, Reproduce. Too many checks that kind of fly in the face of the whole "sensing" thing.
    enum LIFE_STATE
    {
        ALIVE,
        DEAD,
        DECOMPOSING,
        COUNT
    }
    
    
    enum DECISION
    {
        FLEE,
        GRAZE,
        REPRODUCE,
        WANDER,
        COUNT
    }
    
    private Vector2 pos_;

    private int vision_distance_ = 1; // note: a unit is one square on the grid
    private List<Vector2> moving_options_ = new List<Vector2>();
    private List<Vector2> grazing_options_ = new List<Vector2>();
    
    private Vector2 nearby_wolf_pos_ = Vector2.zero;
    private bool wolves_nearby_ = false;

    private DECISION current_decision_ = DECISION.WANDER;
    
    
    
    // note: sense_inverval_ needs to be greater than act_interval_ or sheep will try to move to the same spaces.
    // Could this be a limitation of dictionaries in comparison to hashtables? Throwing an error when the same key tries to be added twice?
    private double sense_interval_ = 0.1f;
    private double decide_interval_ = 0.2f;
    private double act_interval_ = 0.3f;

    private double next_sense_;
    private double next_decide_;
    private double next_act_;
    
    private bool is_alive_ = true;
    private float health_ = 0.0f;
    private float health_decrease_per_act_tick_ = 0.5f;
    private float minimum_starting_health_ = 50.0f;
    private float maximum_starting_health_ = 80.0f;
    private float max_health_ = 100.0f;
    private float reproduction_cost_ = 10.0f;
    
    public void BirthSheep(Vector2 position)
    {
        pos_ = position;
        transform.position = pos_;   
        sheep_db_.Add(pos_, this);

        health_ = Random.Range(minimum_starting_health_, maximum_starting_health_);
        gameObject.SetActive(health_ >= minimum_starting_health_);
        is_alive_ = true;
        
        onSheepAddedToGame(this);

        next_sense_ = Time.time + sense_interval_;
        next_decide_ = Time.time + decide_interval_;
        next_act_ = Time.time + act_interval_;
        
    }

    public void Tick()
    {
        Sense();
        Decide();
        Act();
    }
    
    void Sense ()
    {
        if (Time.time < next_sense_ || health_ < 0) return;

        AreThereWolvesNearby();
        LookForMoveOptions();
        LookForGrassOptions();
        next_sense_ = Time.time + sense_interval_;
    }

    void Decide()
    {
        if (Time.time < next_decide_ || health_ < 0) return;

        if (AreThereWolvesNearby())
        {
            current_decision_ = DECISION.FLEE;
        }
        else if (health_ >= max_health_)
        {
            current_decision_ = DECISION.REPRODUCE;
        }
        else if (DoesCurrentTileHaveGrass())
        {
            current_decision_ = DECISION.GRAZE;
        }
        else
        {
            current_decision_ = DECISION.WANDER;
        }
        next_decide_ = Time.time + decide_interval_;
    }

    void Act()
    {
        if (Time.time < next_act_ || health_ < 0) return;
        switch (current_decision_)
        {
            case DECISION.GRAZE:
            {
                Graze();
                break;
            }
            case DECISION.WANDER:
            case DECISION.FLEE:
            {
                Move();
                break;
            }
            case DECISION.REPRODUCE:
            {
                Reproduce();
                break;
            }
            default:
            {
                Debug.LogError("Uh-oh, what happened there?! " + pos_);
                break;
            }
        }
        health_ -= health_decrease_per_act_tick_;
        next_act_ = Time.time + act_interval_;
        if (health_ <= 0) Died();
    }

    bool AreThereWolvesNearby()
    {
        nearby_wolf_pos_ = -Vector2.one;
        wolves_nearby_ = false;
        
        for (int i = -vision_distance_; i <= vision_distance_; i++)
        {
            for (int j = -vision_distance_; j <= vision_distance_; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);

                if (GroundTile.TileDictionary.TryGetValue(adjacent_tile_pos, out GroundTile adjacent_tile) ==
                    false) continue;
                if (Wolf.wolf_db_.ContainsKey(adjacent_tile_pos))
                {
                    nearby_wolf_pos_ = adjacent_tile_pos;
                    wolves_nearby_ = true;
                    break;
                }
            }
        }
        // TODO: if true, Increase sensing and acting speed for a short while?
        return wolves_nearby_;
    }
    
    bool DoesCurrentTileHaveGrass()
    {
        if(GroundTile.TileDictionary.TryGetValue(pos_, out GroundTile value) == false) return false;
        return value.type_ == GroundTile.TILETYPE.GRASS;
    }
    
    void LookForGrassOptions()
    {
        grazing_options_.Clear();
        
        for (int i = -1; i <= vision_distance_; i++)
        {
            for (int j = -1; j <= vision_distance_; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);
                if (adjacent_tile_pos == pos_) continue;
                
                if (GroundTile.TileDictionary.TryGetValue(adjacent_tile_pos, out GroundTile adjacent_tile) == false) continue;
                if (sheep_db_.ContainsKey(adjacent_tile_pos)) continue;
                if (Wolf.wolf_db_.ContainsKey(adjacent_tile_pos)) continue;
                                
                if (adjacent_tile.type_ == GroundTile.TILETYPE.DIRT) continue;
                
                grazing_options_.Add(adjacent_tile_pos);
            }
        }
    }
    
    void LookForMoveOptions()
    {
        moving_options_.Clear();

        if (wolves_nearby_)
        {
            Vector2 flee_dir = pos_ - nearby_wolf_pos_;
            Vector2 away_from_danger = pos_ + flee_dir.normalized;
            if (GroundTile.TileDictionary.ContainsKey(away_from_danger))
            {
                moving_options_.Add(away_from_danger);
                return;
            }
        }
        
        for (int i = -1; i <= vision_distance_; i++)
        {
            for (int j = -1; j <= vision_distance_; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);

//                Debug.Log(adjacent_tile_pos + "Does adjacent tile exists: " + GroundTile.TileDictionary.ContainsKey(adjacent_tile_pos));
//                Debug.Log("Is the field occupied by another sheep: " + sheep_db_.ContainsKey(adjacent_tile_pos));
//                Debug.Log("Is there a wolf on this field: " + Wolf.wolf_db_.ContainsKey(adjacent_tile_pos));

                if (GroundTile.TileDictionary.ContainsKey(adjacent_tile_pos) == false) continue;
                if (sheep_db_.ContainsKey(adjacent_tile_pos)) continue;
                if (Wolf.wolf_db_.ContainsKey(adjacent_tile_pos)) continue;
                
                moving_options_.Add(adjacent_tile_pos);
            }
        }

        if (moving_options_.Count <= 0)
        {
            moving_options_.Add((pos_ - nearby_wolf_pos_) + pos_);
        }
    }
    
    void Move()
    {
        if (moving_options_.Count <= 0) return;
        
        int count = 0;
        Vector2 move_pos = moving_options_[Random.Range(0, moving_options_.Count)];
        while (moving_options_.Count > 0 && count < 3) // Careful hardcoded number
        {
            if (GroundTile.TileDictionary.ContainsKey(move_pos) == false || sheep_db_.ContainsKey(move_pos))
            {
                moving_options_.Remove(move_pos);
            }

            if (moving_options_.Count <= 0) return;
            move_pos = moving_options_[Random.Range(0, moving_options_.Count)];
            count++;
        }
               
        if (moving_options_.Count <= 0) return;
        if(sheep_db_.ContainsKey(move_pos)) return;

        sheep_db_.Remove(pos_);
        sheep_db_.Add(move_pos, this);
        if(pos_.x - move_pos.x > 1 ) Debug.Log("I just moved from " + pos_ + " to " + move_pos);

        pos_ = move_pos;
        transform.position = pos_;
    }

    void Graze()
    {
        if (GroundTile.TileDictionary[pos_].ChewGrass() == false)
        {
            // note: this action results in a decision, because otherwise the sheep would eat dirt.
            current_decision_ = DECISION.WANDER;
        }

        health_++;
    }

    void Reproduce()
    {
        if (moving_options_.Count <= 0) return;
        
        int count = 0;
        Vector2 spawn_pos = moving_options_[Random.Range(0, moving_options_.Count)];
        while (moving_options_.Count > 0 && count < 3)
        {
            if (GroundTile.TileDictionary.ContainsKey(spawn_pos) == false || sheep_db_.ContainsKey(spawn_pos))
            {
                moving_options_.Remove(spawn_pos);
            }

            if (moving_options_.Count <= 0) return;
            spawn_pos = moving_options_[Random.Range(0, moving_options_.Count)];
            count++;
        }
        
        if (moving_options_.Count <= 0) return;
        if (sheep_db_.ContainsKey(spawn_pos)) return;
        
        // TODO: implement birthing cost to avoid rabbit like reproduction rates
        health_ -= reproduction_cost_;
        
        GameObject tempRef = null;
        if (sheep_object_pool_.Count > 0)
        {
            tempRef = sheep_object_pool_[0];
            sheep_object_pool_.RemoveAt(0);
        }
        else
        {
            tempRef = Instantiate(sheep_prefab_, gameObject.transform.parent, true);
        }

        moving_options_.Remove(spawn_pos);
        tempRef.GetComponent<Sheep>().BirthSheep(spawn_pos);
    }

    void Died()
    {
        is_alive_ = false;
        if (GroundTile.TileDictionary[pos_].type_ == GroundTile.TILETYPE.DIRT)
        {
            GroundTile.TileDictionary[pos_].PlantSeed();            
        }
        onSheepDeletedFromGame(this);
        MoveSheepToObjectPool();
    }

    void MoveSheepToObjectPool()
    {
        sheep_db_.Remove(pos_);
        pos_ = object_pool_pos_;
        sheep_object_pool_.Add(gameObject);
        gameObject.SetActive(false);
    }

    public float Maul()
    {
        Died();
        return health_;
    }

    public static void SetSheepPrefab(GameObject prefab)
    {
        sheep_prefab_ = prefab;
    }
    
}
