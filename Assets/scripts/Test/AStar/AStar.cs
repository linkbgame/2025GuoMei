using System.Collections.Generic;
using MFarm.Map;
using UnityEngine;

namespace MFarm.AStar
{
    public class AStar : Singleton2<AStar>
    {
        private GridNodes gridNodes;
        private Node2 startNode;
        private Node2 targetNode;
        private int gridWidth;
        private int gridHeight;
        private int mapOriginX;
        private int mapOriginY;
        private int regionWorldOriginX;
        private int regionWorldOriginY;

        private List<Node2> openNodeList;
        private HashSet<Node2> closedNodeList;

        private bool pathFound;

        /// <summary>
        /// ����·����д�� Stack������С������Ѱ·��ʧ����ȫͼ������ÿ�η��� 500��500 �ڵ㣩
        /// </summary>
        public void BuildPath(string sceneName, Vector2Int startPos, Vector2Int endPos, Stack<MovementStep> npcMovementStack)
        {
            if (TrySolvePath(sceneName, startPos, endPos, npcMovementStack, useFullGrid: false))
                return;
            TrySolvePath(sceneName, startPos, endPos, npcMovementStack, useFullGrid: true);
        }

        private bool TrySolvePath(string sceneName, Vector2Int startPos, Vector2Int endPos, Stack<MovementStep> npcMovementStack, bool useFullGrid)
        {
            pathFound = false;
            if (!GenerateGridNodes(sceneName, startPos, endPos, useFullGrid))
                return false;
            if (!FindShortestPath())
                return false;
            npcMovementStack.Clear();
            UpdatePathOnMovementStepStack(sceneName, npcMovementStack);
            return true;
        }

        private bool GenerateGridNodes(string sceneName, Vector2Int startPos, Vector2Int endPos, bool useFullGrid)
        {
            if (!GridMapManager.Instance.GetGridDimensions(sceneName, out Vector2Int gridDimensions, out Vector2Int gridOrigin))
                return false;

            mapOriginX = gridOrigin.x;
            mapOriginY = gridOrigin.y;
            int gw = gridDimensions.x;
            int gh = gridDimensions.y;

            int six = startPos.x - mapOriginX;
            int siy = startPos.y - mapOriginY;
            int eix = endPos.x - mapOriginX;
            int eiy = endPos.y - mapOriginY;

            if (six < 0 || six >= gw || siy < 0 || siy >= gh || eix < 0 || eix >= gw || eiy < 0 || eiy >= gh)
                return false;

            int minIx, maxIx, minIy, maxIy;
            if (useFullGrid)
            {
                minIx = 0;
                maxIx = gw - 1;
                minIy = 0;
                maxIy = gh - 1;
            }
            else
            {
                int pad = Mathf.Max(Mathf.Abs(eix - six), Mathf.Abs(eiy - siy)) + 60;
                minIx = Mathf.Clamp(Mathf.Min(six, eix) - pad, 0, gw - 1);
                minIy = Mathf.Clamp(Mathf.Min(siy, eiy) - pad, 0, gh - 1);
                maxIx = Mathf.Clamp(Mathf.Max(six, eix) + pad, 0, gw - 1);
                maxIy = Mathf.Clamp(Mathf.Max(siy, eiy) + pad, 0, gh - 1);
            }

            int subW = maxIx - minIx + 1;
            int subH = maxIy - minIy + 1;

            gridNodes = new GridNodes(subW, subH);
            gridWidth = subW;
            gridHeight = subH;
            regionWorldOriginX = mapOriginX + minIx;
            regionWorldOriginY = mapOriginY + minIy;

            openNodeList = new List<Node2>();
            closedNodeList = new HashSet<Node2>();

            startNode = gridNodes.GetGridNode(six - minIx, siy - minIy);
            targetNode = gridNodes.GetGridNode(eix - minIx, eiy - minIy);
            if (startNode == null || targetNode == null)
                return false;

            ResetNodesForSearch();
            MarkObstaclesInRegion();

            return true;
        }

        private void ResetNodesForSearch()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Node2 n = gridNodes.GetGridNode(x, y);
                    n.gCost = 0;
                    n.hCost = 0;
                    n.parentNode = null;
                    n.isObstacle = false;
                }
            }
        }

        private void MarkObstaclesInRegion()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    int worldX = regionWorldOriginX + x;
                    int worldY = regionWorldOriginY + y;
                    TileDetails tile = GridMapManager.Instance.GetTileDetailsByPosition(worldX, worldY);
                    if (tile != null && tile.isNPCObstacle)
                        gridNodes.GetGridNode(x, y).isObstacle = true;
                }
            }
        }

        private bool FindShortestPath()
        {
            openNodeList.Add(startNode);

            while (openNodeList.Count > 0)
            {
                openNodeList.Sort();
                Node2 closeNode = openNodeList[0];
                openNodeList.RemoveAt(0);
                closedNodeList.Add(closeNode);

                if (closeNode == targetNode)
                {
                    pathFound = true;
                    break;
                }

                EvaluateNeighbourNodes(closeNode);
            }

            return pathFound;
        }

        private void EvaluateNeighbourNodes(Node2 currentNode)
        {
            Vector2Int currentNodePos = currentNode.gridPosition;
            Node2 validNeighbourNode;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    validNeighbourNode = GetValidNeighbourNode(currentNodePos.x + x, currentNodePos.y + y);

                    if (validNeighbourNode != null)
                    {
                        if (!openNodeList.Contains(validNeighbourNode))
                        {
                            validNeighbourNode.gCost = currentNode.gCost + GetDistance(currentNode, validNeighbourNode);
                            validNeighbourNode.hCost = GetDistance(validNeighbourNode, targetNode);
                            validNeighbourNode.parentNode = currentNode;
                            openNodeList.Add(validNeighbourNode);
                        }
                    }
                }
            }
        }

        private Node2 GetValidNeighbourNode(int x, int y)
        {
            if (x >= gridWidth || y >= gridHeight || x < 0 || y < 0)
                return null;

            Node2 neighbourNode = gridNodes.GetGridNode(x, y);

            if (neighbourNode.isObstacle || closedNodeList.Contains(neighbourNode))
                return null;
            else
                return neighbourNode;
        }

        private int GetDistance(Node2 nodeA, Node2 nodeB)
        {
            int xDistance = Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
            int yDistance = Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);

            if (xDistance > yDistance)
            {
                return 14 * yDistance + 10 * (xDistance - yDistance);
            }
            return 14 * xDistance + 10 * (yDistance - xDistance);
        }

        private void UpdatePathOnMovementStepStack(string sceneName, Stack<MovementStep> npcMovementStep)
        {
            Node2 nextNode = targetNode;

            while (nextNode != null)
            {
                MovementStep newStep = new MovementStep();
                newStep.sceneName = sceneName;
                newStep.gridCoordinate = new Vector2Int(
                    nextNode.gridPosition.x + regionWorldOriginX,
                    nextNode.gridPosition.y + regionWorldOriginY);
                npcMovementStep.Push(newStep);

                nextNode = nextNode.parentNode;
            }
        }
    }
}
