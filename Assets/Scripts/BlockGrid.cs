using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockGrid : MonoBehaviour
{
	public static int width  = 10;
	public static int height = 25;
	
	public static Transform[,] grid = new Transform[width,height];
	
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
	public static Vector2 roundVec2(Vector2 v) {
		return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
	}
	
	public static bool insideGrid(Vector2 pos) {
		return ((int) pos.x >= 0 && (int) pos.x < width && (int) pos.y >= 0);
	}
	
	public static void deleteRow(int y) {
		for (int x = 0; x < width; x++) {
			Destroy(grid[x, y].gameObject);
			grid[x, y] = null;
		}
	}
	
	public static void dropRow(int y) {
		for (int x = 0; x < width; x++) {
			if(grid[x, y] != null) {
				//move down
				grid[x, y - 1] = grid[x, y];
				grid[x, y] = null;
				
				//update block position
				grid[x, y - 1].position += new Vector3(0, -1, 0);
			}
		}
	}
	
	public static void dropRowsAbove(int y) {
		for (int i = y; i < height; i++) {
			dropRow(i);
		}
	}
	
	public static bool isRowFull(int y) {
		for (int x = 0; x < width; x++) {
			if (grid[x, y] == null) {
				return false;
			}
		}
		return true;
	}
	
	public static void deleteFullRows() {
		for (int y = 0; y < height; y++) {
			if(isRowFull(y)) {
				deleteRow(y);
				dropRowsAbove(y + 1);
				y--;
			}
		}
	}
}
