using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MFarm.Map
{
    public class GridMapManager : Singleton2<GridMapManager>
    {
        [Header("??????")]
        public MapData_SO mapData;  // ????????????? MapData??????? List

        [Header("???????????? Scene ???????????��??")]
        [Tooltip("????? Scene ??????? mapData ?????????????????")]
        public bool drawDebugGizmos = true;
        [Tooltip("?????????????��?????? Grid ??? Cell Size ??????????? 1x1??")]
        public Vector2 gizmoCellSize = Vector2.one;
        [Tooltip("??????????????????? MapData_SO.gridWidth/gridHeight/originX/originY????")]
        public Color mapBoundsColor = Color.yellow;
        [Tooltip("?????? NPC ?????????????")]
        public Color npcObstacleColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        [Tooltip("???????��?????????????????")]
        public Color normalTileColor = new Color(0.2f, 1f, 0.2f, 0.35f);

        // ???????????????
        private Dictionary<string, TileDetails> tileDetailsDict = new Dictionary<string, TileDetails>();

        private bool isInitialized = false;

        private void Start()
        {
            InitTileDetailsDict();
        }

        /// <summary>
        /// ????????????????
        /// </summary>
        private void InitTileDetailsDict()
        {
            if (mapData == null)
            {
                Debug.LogError("GridMapManager: mapData ��????????? Inspector ?????? MapData_SO");
                return;
            }

            tileDetailsDict.Clear();

            foreach (TileProperty tileProperty in mapData.tileProperties)
            {
                TileDetails tileDetails = new TileDetails
                {
                    girdX = tileProperty.tileCoordinate.x,
                    gridY = tileProperty.tileCoordinate.y
                };

                // ?? key???????????????
                string key = tileDetails.girdX + "x" + tileDetails.gridY + "y";

                if (GetTileDetails(key) != null)
                {
                    tileDetails = GetTileDetails(key);
                }

                switch (tileProperty.gridType)
                {
                    case GridType.NPCObstacle:
                        tileDetails.isNPCObstacle = tileProperty.boolTypeValue;
                        break;
                }

                if (GetTileDetails(key) != null)
                    tileDetailsDict[key] = tileDetails;
                else
                    tileDetailsDict.Add(key, tileDetails);
            }

            isInitialized = true;
            Debug.Log($"GridMapManager: ??????????? {tileDetailsDict.Count} ????????");
        }

        /// <summary>
        /// ????key??????????
        /// </summary>
        public TileDetails GetTileDetails(string key)
        {
            if (tileDetailsDict.ContainsKey(key))
            {
                return tileDetailsDict[key];
            }
            return null;
        }

        /// <summary>
        /// ????????????????????��?????????????
        /// </summary>
        public TileDetails GetTileDetailsByPosition(int x, int y)
        {
            string key = x + "x" + y + "y";
            return GetTileDetails(key);
        }

        /// <summary>
        /// ???????????????????????
        /// </summary>
        public TileDetails GetTileDetailsOnMousePosition(Vector3Int mouseGridPos)
        {
            string key = mouseGridPos.x + "x" + mouseGridPos.y + "y";
            return GetTileDetails(key);
        }

        /// <summary>
        /// ???????��????��sceneName ?????????????????????????
        /// </summary>
        public bool GetGridDimensions(string sceneName, out Vector2Int gridDimensions, out Vector2Int gridOrigin)
        {
            gridDimensions = Vector2Int.zero;
            gridOrigin = Vector2Int.zero;

            if (mapData == null)
            {
                Debug.LogError("GridMapManager: mapData ��?????");
                return false;
            }

            gridDimensions.x = mapData.gridWidth;
            gridDimensions.y = mapData.gridHeight;
            gridOrigin.x = mapData.originX;
            gridOrigin.y = mapData.originY;

            return true;
        }

        // ===================== ���Թ��� =====================

        [ContextMenu("��ӡ��ͼ������Ϣ")]
        public void DebugPrintCoverage()
        {
            if (mapData == null)
            {
                Debug.LogWarning("[GridMapManager] mapData Ϊ�գ��޷���ӡ��");
                return;
            }

            int total = mapData.tileProperties != null ? mapData.tileProperties.Count : 0;
            int npcObstacleCount = 0;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

            if (mapData.tileProperties != null)
            {
                foreach (var t in mapData.tileProperties)
                {
                    if (t.gridType == GridType.NPCObstacle && t.boolTypeValue)
                        npcObstacleCount++;
                    if (t.tileCoordinate.x < minX) minX = t.tileCoordinate.x;
                    if (t.tileCoordinate.y < minY) minY = t.tileCoordinate.y;
                    if (t.tileCoordinate.x > maxX) maxX = t.tileCoordinate.x;
                    if (t.tileCoordinate.y > maxY) maxY = t.tileCoordinate.y;
                }
            }

            Debug.Log(
                $"[GridMapManager] mapData='{mapData.name}'  scene='{mapData.sceneName}'\n" +
                $"  ��������:  origin=({mapData.originX},{mapData.originY})  size={mapData.gridWidth}x{mapData.gridHeight}  " +
                $"���Ƿ�Χ: ({mapData.originX},{mapData.originY}) ~ ({mapData.originX + mapData.gridWidth - 1},{mapData.originY + mapData.gridHeight - 1})\n" +
                $"  �Ѽ�¼��Ƭ: {total} �������� NPC �ϰ� {npcObstacleCount} ��\n" +
                $"  ��Ƭ���귶Χ: " + (total == 0 ? "��" : $"({minX},{minY}) ~ ({maxX},{maxY})"));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos) return;
            if (mapData == null) return;

            Vector3 boundsMin = new Vector3(mapData.originX * gizmoCellSize.x, mapData.originY * gizmoCellSize.y, 0f);
            Vector3 boundsSize = new Vector3(mapData.gridWidth * gizmoCellSize.x, mapData.gridHeight * gizmoCellSize.y, 0f);
            Vector3 boundsCenter = boundsMin + boundsSize * 0.5f;

            Gizmos.color = mapBoundsColor;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);

            if (mapData.tileProperties != null)
            {
                foreach (var t in mapData.tileProperties)
                {
                    bool isObstacle = (t.gridType == GridType.NPCObstacle) && t.boolTypeValue;
                    Gizmos.color = isObstacle ? npcObstacleColor : normalTileColor;
                    Vector3 cellCenter = new Vector3(
                        (t.tileCoordinate.x + 0.5f) * gizmoCellSize.x,
                        (t.tileCoordinate.y + 0.5f) * gizmoCellSize.y,
                        0f);
                    Gizmos.DrawCube(cellCenter, new Vector3(gizmoCellSize.x * 0.95f, gizmoCellSize.y * 0.95f, 0.01f));
                }
            }
        }
#endif
    }
}
