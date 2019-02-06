using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.VR;
using UnityEngine;

public class Wolf : MonoBehaviour
{
    public static Dictionary<Vector2, Wolf> wolf_db_ = new Dictionary<Vector2, Wolf>();
    
    public delegate void OnWolfAddedToGame(Wolf wolf);
    public static event OnWolfAddedToGame onWolfAddedToGame;

    public delegate void OnWolfDeletedFromGame(Wolf wolf);
    public static event OnWolfDeletedFromGame onWolfDeletedFromGame;

    private static GameObject wolf_prefab_ = null;
    private static List<GameObject> wolf_object_poll_ = new List<GameObject>();
    private static Vector2 object_pool_pos_ = new Vector2(-200, -200);
    
    
    private Vector2 pos_;

    private int vision_distance_ = 2;
    private List<Vector2> moving_options_ = new List<Vector2>();
    private List<Vector2> grazing_options_ = new List<Vector2>();

    private Vector2 nearby_sheep_pos_ = Vector2.zero;
    private bool sheep_nearby_ = false;
    private bool sheep_on_same_tile_ = false;

    private DECISION current_decision_ = DECISION.WANDER;
    
    private double sense_interval_ = 0.1f;
    private double decide_interval_ = 0.05f;
    private double act_interval_ = 0.5f;

    private double next_sense_;
    private double next_decide_;
    private double next_act_;
    
    private bool is_alive_ = true;
    private float health_ = 0.0f;
    private float health_decrease_per_act_tick_ = 1.5f;
    private float minimum_starting_health_ = 30.0f;
    private float maximum_starting_health_ = 50.0f;
    private float max_health_ = 100.0f;
    private float reproduction_cost_ = 50.0f;  

    enum DECISION
    {
        EAT,
        HUNT,
        REPRODUCE,
        WANDER,
        COUNT
    }

    public void BirthWolf(Vector2 position)
    {
        pos_ = position;
        transform.position = pos_;   
        wolf_db_.Add(pos_, this);

        health_ = Random.Range(minimum_starting_health_, maximum_starting_health_);
        gameObject.SetActive(health_ >= minimum_starting_health_);
        is_alive_ = true;
        
        onWolfAddedToGame(this);

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

    void Sense()
    {
        if (Time.time < next_sense_ || is_alive_ == false) return;

        IsTheTargetOnTheSameField();
        LookForMoveOptions();
        LookForSheep();
        next_sense_ = Time.time + sense_interval_;
    }

    void Decide()
    {
        if (Time.time < next_sense_ || is_alive_ == false) return;

        if (sheep_on_same_tile_)
        {
            current_decision_ = DECISION.EAT;
        } 
        else if (sheep_nearby_)
        {
            current_decision_ = DECISION.HUNT;
        }
        else if (health_ >= maximum_starting_health_)
        {
            current_decision_ = DECISION.REPRODUCE;
        }
        else
        {
            current_decision_ = DECISION.WANDER;
        }
        next_decide_ = Time.time + sense_interval_;
    }

    void Act()
    {
        if (Time.time < next_act_ || !is_alive_) return;
        switch (current_decision_)
        {
            case DECISION.EAT:
            {
                TakeABite();
                break;
            }
            case DECISION.HUNT:
            {
                LookForSheep();
                break;
            }
            case DECISION.REPRODUCE:
            {
                Reproduce();
                break;
            }
            case DECISION.WANDER:
            {
                Wander();
                break;
            }
            default:
            {
                Debug.LogError("Woops, that's bad..." + pos_);
                break;
            }
        }

        health_ -= health_decrease_per_act_tick_;
        next_act_ = Time.time + act_interval_;
        if (health_ <= 0) Died();
    }

    void LookForMoveOptions()
    {
        moving_options_.Clear();

        for (int i = -1; i <= vision_distance_; i++)
        {
            for (int j = -1; j <= vision_distance_; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);

                if (GroundTile.TileDictionary.ContainsKey(adjacent_tile_pos) == false) continue;
                if (wolf_db_.ContainsKey(adjacent_tile_pos)) continue;

                moving_options_.Add(adjacent_tile_pos);
            }
        }
    }

    bool IsTheTargetOnTheSameField()
    {

        return true;
    }

    void Wander()
    {
        if (sheep_on_same_tile_) return;
        
        if (moving_options_.Count <= 0) return;
        
        int count = 0;
        Vector2 move_pos = moving_options_[Random.Range(0, moving_options_.Count)];
        while (moving_options_.Count > 0 && count < 3) // Careful hardcoded number
        {
            if (GroundTile.TileDictionary.ContainsKey(move_pos) == false || wolf_db_.ContainsKey(move_pos))
            {
                moving_options_.Remove(move_pos);
            }

            if (moving_options_.Count <= 0) return;
            move_pos = moving_options_[Random.Range(0, moving_options_.Count)];
            count++;
        }
               
        if (moving_options_.Count <= 0) return;
        if(wolf_db_.ContainsKey(move_pos)) return;

        wolf_db_.Remove(pos_);
        wolf_db_.Add(move_pos, this);
        if(pos_.x - move_pos.x > 1 ) Debug.Log("I just moved from " + pos_ + " to " + move_pos);

        pos_ = move_pos;
        transform.position = pos_;
    }

    void LookForSheep()
    {
        nearby_sheep_pos_ = -Vector2.one;
        sheep_nearby_ = false;
        
        List<Vector2> nearby_sheep_list = new List<Vector2>();
        for (int i = -vision_distance_; i <= vision_distance_; i++)
        {
            for (int j = -vision_distance_; j <= vision_distance_; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);

                if (GroundTile.TileDictionary.TryGetValue(adjacent_tile_pos, out GroundTile adjacent_tile) ==
                    false) continue;
                if (Sheep.sheep_db_.ContainsKey(adjacent_tile_pos))
                {
                    nearby_sheep_list.Add(adjacent_tile_pos);
                    sheep_nearby_ = true;
                    break;
                }
            }
        }

        if (nearby_sheep_list.Count > 0)
        {
            nearby_sheep_pos_ = nearby_sheep_list[Random.Range(0, nearby_sheep_list.Count)];
        }   
    }

    void TakeABite()
    {
        if(Sheep.sheep_db_.TryGetValue(pos_, out Sheep sheep_on_tile))
        {
            float sheep_tastiness = sheep_on_tile.Maul();
            health_ = Random.Range(sheep_tastiness / 2, sheep_tastiness);
        }
    }

    void Reproduce()
    {
        if (moving_options_.Count <= 0) return;
        
        int count = 0;
        Vector2 spawn_pos = moving_options_[Random.Range(0, moving_options_.Count)];
        while (moving_options_.Count > 0 && count < 3)
        {
            if (GroundTile.TileDictionary.ContainsKey(spawn_pos) == false || wolf_db_.ContainsKey(spawn_pos))
            {
                moving_options_.Remove(spawn_pos);
            }

            if (moving_options_.Count <= 0) return;
            spawn_pos = moving_options_[Random.Range(0, moving_options_.Count)];
            count++;
        }
        
        if (moving_options_.Count <= 0) return;
        if (wolf_db_.ContainsKey(spawn_pos)) return;
        
        // TODO: implement birthing cost to avoid rabbit like reproduction rates
        health_ -= 10;
        
        GameObject tempRef = null;
        if (wolf_object_poll_.Count > 0)
        {
            tempRef = wolf_object_poll_[0];
            wolf_object_poll_.RemoveAt(0);
        }
        else
        {
            tempRef = Instantiate(wolf_prefab_, gameObject.transform.parent, true);
        }

        moving_options_.Remove(spawn_pos);
        tempRef.GetComponent<Wolf>().BirthWolf(spawn_pos);
    }

    void Died()
    {
        is_alive_ = false;
        if (GroundTile.TileDictionary[pos_].type_ == GroundTile.TILETYPE.DIRT)
        {
            GroundTile.TileDictionary[pos_].PlantSeed();            
        }

        onWolfDeletedFromGame(this);
        MoveWolfToObjectPool();
    }

    void MoveWolfToObjectPool()
    {
        wolf_db_.Remove(pos_);
        pos_ = object_pool_pos_;
        wolf_object_poll_.Add(gameObject);
        gameObject.SetActive(false);
    }
    
    public static void SetWolfPrefab(GameObject prefab)
    {
        wolf_prefab_ = prefab;
    }
}
