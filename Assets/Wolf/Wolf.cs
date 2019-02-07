using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.VR;
using UnityEngine;
using Random = UnityEngine.Random;

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
    private List<Sheep> nearby_sheep_list_ = new List<Sheep>();
    
    private Vector2 nearby_sheep_pos_ = Vector2.zero;
    private bool sheep_nearby_ = false;
    
    private Sheep target_sheep_;
    private bool sheep_on_same_tile_ = false;

    private DECISION current_decision_ = DECISION.WANDER;
    
    private double sense_interval_ = 0.08f;
    private double decide_interval_ = 0.1f;
    private double act_interval_ = 0.15f;

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
        if (Time.time < next_decide_ || is_alive_ == false) return;

//        if (sheep_on_same_tile_)
//        {
//            current_decision_ = DECISION.EAT;
//        } 
//        else 
        if (health_ >= max_health_)
        {
            current_decision_ = DECISION.REPRODUCE;
        }
        else if (sheep_nearby_)
        {
            current_decision_ = DECISION.HUNT;
        }
        else if(nearby_sheep_list_.Count <= 0)
        {
            current_decision_ = DECISION.WANDER;
        } 

        next_decide_ = Time.time + decide_interval_;
    }

    void Act()
    {
        if (Time.time < next_act_ || !is_alive_) return;
        switch (current_decision_)
        {
//            case DECISION.EAT:
//            {
//                TakeABite();
//                break;
//            }
            case DECISION.HUNT:
            {
                LookForSheep();
                HuntTargetDown();
                break;
            }
            case DECISION.REPRODUCE:
            {
                Reproduce();
                break;
            }
            case DECISION.WANDER:
            {
                LookForSheep();
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

        for (int i = -vision_distance_; i <= vision_distance_; i++)
        {
            for (int j = -vision_distance_; j <= vision_distance_; j++)
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
        if (nearby_sheep_pos_ == pos_ && Sheep.sheep_db_.ContainsKey(nearby_sheep_pos_))
        {
            return true;
        }

        return false;
    }

    void HuntTargetDown()
    {
        if (target_sheep_ != null &&  Sheep.sheep_db_.TryGetValue(nearby_sheep_pos_, out target_sheep_) && IsTheTargetOnTheSameField())
        {
            TakeABite();
            return;
        }
       
        target_sheep_ = null;
        
        if (target_sheep_ == null)
        {
            foreach (var target in nearby_sheep_list_)
            {
                nearby_sheep_pos_ = target.transform.position;
                if (IsTheTargetOnTheSameField())
                {
                    Sheep.sheep_db_.TryGetValue(nearby_sheep_pos_, out target_sheep_);
                    TakeABite();
                    return;
                }
            }
        
            if (nearby_sheep_list_.Count > 0 && target_sheep_ == null)
            {
                nearby_sheep_pos_ = nearby_sheep_list_[Random.Range(0, nearby_sheep_list_.Count)].transform.position;
                Sheep.sheep_db_.TryGetValue(nearby_sheep_pos_, out target_sheep_);
            }     
        }

        if (target_sheep_ == null) return;

        nearby_sheep_pos_ = target_sheep_.transform.position;
        Vector2 hunt_dir = nearby_sheep_pos_ - pos_;
        Vector2 move_pos;
        if (Mathf.Abs(hunt_dir.x) > Mathf.Abs(hunt_dir.y))
        {
            if (hunt_dir.x > 0) move_pos = pos_ + Vector2.right;
            else move_pos = pos_ + Vector2.left;

        }
        else
        {
            if (hunt_dir.y > 0) move_pos = pos_ + Vector2.up;
            else move_pos = pos_ + Vector2.down;
        }

        if(wolf_db_.ContainsKey(move_pos)) return;
        if (GroundTile.TileDictionary.ContainsKey(move_pos) == false) return;

        wolf_db_.Remove(pos_);
        wolf_db_.Add(move_pos, this);
        pos_ = move_pos;
        transform.position = pos_;
        
    }
    
    void Wander()
    {
        if (moving_options_.Count <= 0) return;
        
        Vector2 move_pos;

        List<Vector2> possible_moves = new List<Vector2>();
        for(int i = 0; i < moving_options_.Count; i++)
        {
            move_pos = moving_options_[i];

            Vector2 adjusted_move_pos;
            if (Mathf.Abs(move_pos.x) > Mathf.Abs(move_pos.y))
            {
                if (move_pos.x > 0) adjusted_move_pos = pos_ + Vector2.right;
                else adjusted_move_pos = pos_ + Vector2.left;
            }
            else
            {
                if (move_pos.y > 0) adjusted_move_pos = pos_ + Vector2.up;
                else adjusted_move_pos = pos_ + Vector2.down;
            }

            if (GroundTile.TileDictionary.ContainsKey(adjusted_move_pos) && !wolf_db_.ContainsKey(adjusted_move_pos))
            {
                possible_moves.Add(adjusted_move_pos);
            }

            if (moving_options_.Count <= 0) return;
        }

        
        if (possible_moves.Count <= 0) return;
        move_pos = possible_moves[Random.Range(0, possible_moves.Count)];
        Debug.Log("I am currently at " + pos_ + " and will be moving to " + move_pos);
        
        if (GroundTile.TileDictionary.ContainsKey(move_pos) == false) return;
        if(wolf_db_.ContainsKey(move_pos)) return;
        
        wolf_db_.Remove(pos_);
        wolf_db_.Add(move_pos, this);

        pos_ = move_pos;
        transform.position = pos_;
    }

    void LookForSheep()
    {
        if (target_sheep_ != null)
        {
            nearby_sheep_pos_ = target_sheep_.transform.position;
            sheep_nearby_ = Sheep.sheep_db_.ContainsKey(nearby_sheep_pos_);
            if (sheep_nearby_) return;
        }
        
        sheep_nearby_ = false;
        nearby_sheep_list_.Clear();
        
        for (int i = -vision_distance_; i <= vision_distance_; i++)
        {
            for (int j = -vision_distance_; j <= vision_distance_; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);

                if (GroundTile.TileDictionary.ContainsKey(adjacent_tile_pos) == false) continue;
                if (Sheep.sheep_db_.TryGetValue(adjacent_tile_pos, out Sheep nearby_sheep))
                {
                    nearby_sheep_list_.Add(nearby_sheep);
                    sheep_nearby_ = true;
                }
            }
        }
    }

    void TakeABite()
    {
        if(Sheep.sheep_db_.TryGetValue(pos_, out Sheep sheep_on_tile))
        {
            float sheep_tastiness = sheep_on_tile.Maul();
            health_ += Random.Range(sheep_tastiness / 2, sheep_tastiness);
//            Debug.Log("hmmm, that was good " + sheep_tastiness);
        }
        target_sheep_ = null;
        sheep_nearby_ = false;
    }

    void Reproduce()
    {
        if (moving_options_.Count <= 0 || health_ < 100) return;
        
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
        health_ -= reproduction_cost_;
        
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

//        Debug.Log("Birth Wolf, my health is " + health_);
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
