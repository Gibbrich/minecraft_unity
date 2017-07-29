using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class GenerateLandscape : MonoBehaviour
{
    public static int width = 128;
    public static int height = 128;
    public static int depth = 128;

    public int heightScale = 20;
    public int heightOffset = 100;
    public float detailScale = 25.0f;

    public GameObject grassBlock;
    public GameObject sandBlock;
    public GameObject snowBlock;
    public GameObject cloudBlock;
    public GameObject diamondBlock;

    private Block[,,] worldBlocks = new Block[width, height, depth];

    // Use this for initialization
    void Start()
    {
        int seed = (int) Network.time * 10;
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = (int) (Mathf.PerlinNoise((x + seed) / detailScale, (z + seed) / detailScale) * heightScale) +
                        heightOffset;
                CreateBlock(y, new Vector3(x, y, z), true);

                while (y > 0)
                {
                    y--;
                    Vector3 blockPos = new Vector3(x, y, z);
                    CreateBlock(y, blockPos, false);
                }
            }
        }

        DrawClouds(20, 100);
        DrawMines(20, 3);
    }

    private void DrawClouds(int numClouds, int cSize)
    {
        for (int i = 0; i < numClouds; i++)
        {
            int xpos = Random.Range(0, width);
            int zpos = Random.Range(0, depth);
            for (int j = 0; j < cSize; j++)
            {
                Vector3 blockPos = new Vector3(xpos, height - 1, zpos);
                GameObject newBlock = Instantiate(cloudBlock, blockPos, Quaternion.identity);
                worldBlocks[(int) blockPos.x, (int) blockPos.y, (int) blockPos.z] = new Block(4, true, newBlock);
                xpos += Random.Range(-1, 2);
                zpos += Random.Range(-1, 2);
                if (xpos < 0 || xpos >= width)
                {
                    xpos = width / 2;
                }

                if (zpos < 0 || zpos >= depth)
                {
                    zpos = depth / 2;
                }
            }
        }
    }

    private void CreateBlock(int y, Vector3 blockPosition, bool create)
    {
        GameObject blockToInstantiate;
        int type;

        if (y > 15 + heightOffset)
        {
            blockToInstantiate = snowBlock;
            type = 1;
        }
        else if (y > 5 + heightOffset)
        {
            blockToInstantiate = grassBlock;
            type = 2;
        }
        else
        {
            blockToInstantiate = sandBlock;
            type = 3;
        }

        GameObject newBlock = null;
        
        if (create)
        {
            newBlock = Instantiate(blockToInstantiate, blockPosition, Quaternion.identity);
        }
        worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z] = new Block(type, create, newBlock);

        if (y > heightOffset - 20 && y < heightOffset - 15 && Random.Range(0, 100) < 10)
        {
            if (create)
            {
                newBlock = Instantiate(diamondBlock, blockPosition, Quaternion.identity);
            }

            worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z] = new Block(5, create, newBlock);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f));
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                Vector3 blockPosition = hit.transform.position;

                // if this is bottom block, do not delete it
                if ((int) blockPosition.y == 0)
                {
                    return;
                }

                // delete block from memory and physical world
                worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z] = null;
                Destroy(hit.transform.gameObject);

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            if (!(x == 0 && y == 0 && z == 0))
                            {
                                Vector3 neighbour = new Vector3(blockPosition.x + x, blockPosition.y + y,
                                    blockPosition.z + z);
                                DrawBlock(neighbour);
                            }
                        }
                    }
                }
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f));
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                Vector3 blockPosition = hit.transform.position;
                Vector3 hitVector = blockPosition - hit.point;

                hitVector.x = Mathf.Abs(hitVector.x);
                hitVector.y = Mathf.Abs(hitVector.y);
                hitVector.z = Mathf.Abs(hitVector.z);

                if (hitVector.x > hitVector.z && hitVector.x > hitVector.y)
                {
                    blockPosition.x -= Mathf.RoundToInt(ray.direction.x);
                }
                else if (hitVector.y > hitVector.x && hitVector.y > hitVector.z)
                {
                    blockPosition.y -= Mathf.RoundToInt(ray.direction.y);
                }
                else
                {
                    blockPosition.z -= Mathf.RoundToInt(ray.direction.z);
                }
                
                CreateBlock((int) blockPosition.y, blockPosition, true);
                CheckObscureNeighbours(blockPosition);
            }
        }
    }

    private void CheckObscureNeighbours(Vector3 blockPosition)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (!(x == 0 && y == 0 && z == 0))
                    {
                        Vector3 neighbour = new Vector3(blockPosition.x + x, blockPosition.y + y, blockPosition.z + z);

                        if (neighbour.x < 1 || neighbour.x > width - 2 ||
                            neighbour.y < 1 || neighbour.y > height - 2 ||
                            neighbour.z < 1 || neighbour.z > depth - 2)
                        {
                            continue;
                        }

                        if (worldBlocks[(int) neighbour.x, (int) neighbour.y, (int) neighbour.z] != null)
                        {
                            if (NeighbourCount(neighbour) == 26)
                            {
                                Destroy(worldBlocks[(int) neighbour.x, (int) neighbour.y, (int) neighbour.z].block);
                                worldBlocks[(int) neighbour.x, (int) neighbour.y, (int) neighbour.z] = null;
                            }
                        }
                    }
                }
            }
        }
    }

    private int NeighbourCount(Vector3 neighbour)
    {
        int nCount = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (!(x == 0 && y == 0 && z == 0))
                    {
                        if (worldBlocks[(int) neighbour.x, (int) neighbour.y, (int) neighbour.z] != null)
                        {
                            nCount++;
                        }
                    }
                }
            }
        }
        return nCount;
    }

    private void DrawBlock(Vector3 blockPosition)
    {
        Block block = worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z];

        if (blockPosition.x < 0 || blockPosition.x >= width
            || blockPosition.y < 0 || blockPosition.y >= height
            || blockPosition.z < 0 || blockPosition.z >= depth
            || block == null)
        {
            return;
        }

        if (!block.vis)
        {
            GameObject newBlock = null;
            block.vis = true;
            if (block.type == 1)
            {
                newBlock = Instantiate(snowBlock, blockPosition, Quaternion.identity);
            }
            else if (block.type == 2)
            {
                newBlock = Instantiate(grassBlock, blockPosition, Quaternion.identity);
            }
            else if (block.type == 3)
            {
                newBlock = Instantiate(sandBlock, blockPosition, Quaternion.identity);
            }
            else if (block.type == 5)
            {
                newBlock = Instantiate(diamondBlock, blockPosition, Quaternion.identity);
            }
            else
            {
                block.vis = false;
            }

            if (newBlock)
            {
                worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z].block = newBlock;
            }
        }
    }

    private void DrawMines(int numSize, int mSize)
    {
        int holeSize = 2;
        for (int i = 0; i < numSize; i++)
        {
            int xpos = Random.Range(10, width - 10);
            int ypos = Random.Range(10, height - 10);
            int zpos = Random.Range(10, depth - 10);

            for (int j = 0; j < mSize; j++)
            {
                for (int x = -holeSize; x < holeSize; x++)
                {
                    for (int y = -holeSize; y < holeSize; y++)
                    {
                        for (int z = -holeSize; z < holeSize; z++)
                        {
                            if (!(x == 0 && y == 0 && z == 0))
                            {
                                Vector3 blockPosition = new Vector3(xpos + x, ypos + y, zpos + z);

                                Block block = worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z];
                                if (block != null)
                                {
                                    if (block.block)
                                    {
                                        Destroy(block.block);                                        
                                    }
                                }
                                worldBlocks[(int) blockPosition.x, (int) blockPosition.y, (int) blockPosition.z] = null;
                            }
                        }
                    }
                }

                xpos += Random.Range(-1, 2);
                ypos += Random.Range(-1, 2);
                zpos += Random.Range(-1, 2);

                if (xpos < holeSize || xpos >= width - holeSize)
                {
                    xpos = width / 2;
                }
                if (ypos < holeSize || ypos >= height - holeSize)
                {
                    ypos = height / 2;
                }
                if (zpos < holeSize || zpos >= depth - holeSize)
                {
                    zpos = depth / 2;
                }

                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < depth - 1; y++)
                    {
                        for (int z = 1; z < height - 1; z++)
                        {
                            if (worldBlocks[x, y, z] == null)
                            {
                                for (int x1 = -1; x1 <= 1; x1++)
                                {
                                    for (int y1 = -1; y1 <= 1; y1++)
                                    {
                                        for (int z1 = -1; z1 <= 1; z1++)
                                        {
                                            if (!(x1 == 0 && y1 == 0 && z1 == 0))
                                            {
                                                Vector3 neighbour = new Vector3(x + x1, y + y1, z + z1);
                                                DrawBlock(neighbour);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class Block
    {
        public int type;
        public bool vis;
        public GameObject block;

        public Block(int type, bool vis, GameObject block)
        {
            this.type = type;
            this.vis = vis;
            this.block = block;
        }
    }
}