using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class GroundTile : MonoBehaviour
{
    public static Dictionary<Vector2, GroundTile> TileDictionary = new Dictionary<Vector2, GroundTile>();

    public static Hashtable TileDb = new Hashtable();
    
    public enum TILETYPE
    {
        GRASS,
        DIRT,
        COUNT
    }

    enum DECISION
    {
        GROW,
        WITHER,
        SPREAD,
        IDLE,
        COUNT
    }
    
    public TILETYPE type_
    {
        get { return current_state_; }
    }

    public bool free_for_sheep_
    {
        get { return !has_sheep_; }
    }
    
    [SerializeField] private Sprite grass_sprite_ = null;
    [SerializeField] private Sprite dirt_sprite_ = null;
    private Vector2 pos_;
    
    private SpriteRenderer sprite_renderer_;

    private TILETYPE current_state_ = TILETYPE.DIRT;
    
    private bool is_tile_seeded_ = false;
    private bool is_grass_mature_ = false;

    private bool has_sheep_ = false;
    private bool has_wolf_ = false;

    private int health_ = 0;
    private int minimum_starting_health_ = 10;
    private int max_health_ = 100;

    private float sense_interval_ = 0.5f;
    private float decide_interval_ = 0.3f;
    private float act_interval_ = 0.1f;

    // note: locks to avoid S-D-A running while data is being overwritten
    private bool is_sensing_ = false;
    private bool is_deciding_ = false;
    
    private DECISION current_decision_ = DECISION.IDLE;
    private bool is_mature_ = false;
    private bool is_withering_ = false;
    private double is_mature_timer_ = 0.0f;
    private double max_mature_time_ = 5.0f;


    private float chances_of_self_seeding_ = 0.01f;
    private float chances_of_spreading_ = 0.8f; // note: percentage of change that a "spread" action will plant a seed on a nearby tile
    
    private void Awake()
    {
        // note: needs to be in Awake instead of Start, otherwise the TileDictionary will not be populated in time for SpawnSheep in GameInstantiator Start
        pos_.x = transform.position.x;
        pos_.y = transform.position.y;
        gameObject.name = "Tile " + pos_.x + " : " + pos_.y;
        
        sprite_renderer_ = GetComponent<SpriteRenderer>();
        TileDictionary.Add(new Vector2(pos_.x, pos_.y), this);
    }

    // Start is called before the first frame update
    void Start()
    {        
        SetSprite();
//        TileDb.Add(new Vector2(pos.x, pos.y), this);
//        Debug.Log(TileDb.Count);

        StartCoroutine(Sense());
        StartCoroutine(Decide());
        StartCoroutine(Act());
    }

    void SelfSeed()
    {
        float die_roll = Random.Range(0.0f, 1.0f);
        Debug.Log(gameObject.name + " attempted selfseeding  " + die_roll + " vs " + chances_of_self_seeding_);
        if (die_roll < chances_of_self_seeding_)
        {
            PlantSeed();
        }
    }
    
    public void PlantSeed()
    {
        is_tile_seeded_ = true;
        current_state_ = TILETYPE.GRASS;
        SetSprite();
        is_mature_timer_ = 0.0f;
        health_ = Random.Range(minimum_starting_health_, max_health_ / 2);
    }

    private void SetSprite()
    {
        switch (current_state_)
        {
            case TILETYPE.DIRT:
            {
                sprite_renderer_.sprite = dirt_sprite_;
                break;
            }
            case TILETYPE.GRASS:
            {
                sprite_renderer_.sprite = grass_sprite_;
                break;
            }
            default:
            {
                Debug.LogError("This should never ever happen!!!! What sorcery is this?" + "Tile Pos " + transform.position);
                break;
            }
        }
    }
    
    IEnumerator Sense()
    {
        while(true)
        {
            is_sensing_ = true;

            has_sheep_ = Sheep.sheep_db_.ContainsKey(pos_);
            has_wolf_ = Wolf.wolf_db_.ContainsKey(pos_);

            is_sensing_ = false;

            yield return new WaitForSeconds(sense_interval_);
        }
    }

    IEnumerator Decide()
    {
        while (true)
        {
            is_deciding_ = true;

            if (current_state_ == TILETYPE.DIRT)
            {
                is_deciding_ = false;
                yield return new WaitUntil(() => is_tile_seeded_ == true);
            }

            if (is_sensing_)
            {
                is_deciding_ = false;
                yield return new WaitUntil(() => is_sensing_ == false);
            }

            
            if (has_sheep_ || has_sheep_)
            {
                current_decision_ = DECISION.IDLE;
            }
            else if (is_mature_)
            {
                current_decision_ = DECISION.SPREAD;

                // TODO: find better way! Right now this is dependent ont the order the states are listed in the enum!!!
                // current_decision_ = (DECISION) Random.Range((int)DECISION.SPREAD, (int)DECISION.IDLE  + 1);
            }
            else if (is_withering_)
            {
                current_decision_ = DECISION.WITHER;
            }
            else if (is_tile_seeded_)
            {
                is_mature_timer_ = 0;
                current_decision_ = DECISION.GROW;
            } 
//        else
//        {
//            current_decision_ = (DECISION) Random.Range(0, Enum.GetValues(typeof(DECISION)).Length);
//        }

            is_deciding_ = false;
            yield return new WaitForSeconds(decide_interval_);
        }
    }

    IEnumerator Act()
    {
        while(true)
        {
            if (current_state_ == TILETYPE.DIRT)
            {
                // SelfSeed();
                yield return new WaitUntil(() => current_state_ == TILETYPE.GRASS);
                continue;
            }

            if (is_deciding_)
            {
                yield return new WaitUntil(() => is_deciding_ == false);
                continue;
            }

            switch (current_decision_)
            {
                case DECISION.IDLE:
                {
                    //Debug.Log("nothing to do but drink a cup of tea; metaphorically of course... I am just a piece of grass after all!");
                    break;
                }
                case DECISION.GROW:
                {
                    Grow();
                    break;
                }
                case DECISION.SPREAD:
                {
                    Spread();
                    break;
                }
                case DECISION.WITHER:
                {
                    Wither();
                    break;
                }
                default:
                {
                    Debug.LogError("Even an vegetation needs to act, why didn't I? " + gameObject.name);
                    break;
                }
            }

            yield return new WaitForSeconds(act_interval_);
        }
    }

    void Grow()
    {
        if (health_ >= max_health_)
        {
            is_mature_ = true;
            is_mature_timer_ = 0;
            is_mature_timer_ = Time.time + max_mature_time_;
            return;
        }
        
        health_++;
    }

    void Wither()
    {
        if (health_ <= 0)
        {
            is_tile_seeded_ = false;
            is_withering_ = false;
            current_state_ = TILETYPE.DIRT;
            SetSprite();
            return;
        }
        health_--;
    }

    void Spread()
    {
        is_mature_ = is_mature_timer_ > Time.time;
        is_withering_ = !is_mature_;
        
//        float die_role = Random.Range(0.0f, 1.0f);
        if (Random.value > chances_of_spreading_) return;
        
        for (int i = -1; i < 1; i++)
        {
            for (int j = -1; j < 1; j++)
            {
                Vector2 adjacent_tile_pos = pos_ + new Vector2(i, j);


                if (TileDictionary.TryGetValue(adjacent_tile_pos, out GroundTile adjacent_tile) == false) continue;
                if (adjacent_tile == this) continue;

                if (adjacent_tile.current_state_ == TILETYPE.DIRT)
                {
                    adjacent_tile.PlantSeed();
                }
            }
        }
    }

    public bool ChewGrass()
    {
        //Debug.Log("Nooooo!!!! Damned vegetarians, I have feelings too :'(");
        if (health_ <= 0)
        {
            sprite_renderer_.sprite = dirt_sprite_;
            is_tile_seeded_ = false;
            current_state_ = TILETYPE.DIRT;
            return false;
        }
        health_--;
        return true;
    }
}
