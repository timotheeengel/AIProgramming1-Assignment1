using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sheep : MonoBehaviour
{
    public static Dictionary<Vector2, Sheep> sheep_db_ = new Dictionary<Vector2, Sheep>();

    private Vector2 pos_;
    private SpriteRenderer sprite_renderer_;

    private int vision_distance_ = 1; // note: a unit is one square on the grid
    private List<Vector2> moving_options_ = new List<Vector2>();
    
    
    private void Awake()
    {
        sprite_renderer_ = GetComponent<SpriteRenderer>();
        
        pos_ = new Vector2(transform.position.x, transform.position.y);
        sheep_db_.Add(pos_, this);
        

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void LookAround()
    {

        for (int i = -1; i <= vision_distance_; i += 2)
        {
            for (int j = -1; j <= vision_distance_; j += 2)
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
        
    }
    
    void Move()
    {
        if (moving_options_.Count <= 0) return;

        Debug.Log(sheep_db_.Count);
        sheep_db_.Remove(pos_);
        Debug.Log(sheep_db_.Count);
        
        pos_ = moving_options_[Random.Range(0, moving_options_.Count)];
        transform.position = pos_;
        sheep_db_.Add(pos_, this);
        

    }
    
}
