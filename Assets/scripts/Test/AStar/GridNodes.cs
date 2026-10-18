using System.Collections.Generic;

using UnityEngine;

namespace MFarm.AStar
{
    public class GridNodes
    {
        private int width;
        private int height;
        private Node2[,] gridNode;

        /// <summary>
        /// ??????????????��????
        /// </summary>
        /// <param name="width">???????</param>
        /// <param name="height">??????</param>
        public GridNodes(int width, int height)
        {
            this.width = width;
            this.height = height;

            gridNode = new Node2[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridNode[x, y] = new Node2(new Vector2Int(x, y));
                }
            }
        }
        public Node2 GetGridNode(int xPos, int yPos)
        {
            // ????????????
            if (xPos >= 0 && yPos >= 0 && xPos < width && yPos < height)
            {
                return gridNode[xPos, yPos];
            }
            return null;
        }
    }
}