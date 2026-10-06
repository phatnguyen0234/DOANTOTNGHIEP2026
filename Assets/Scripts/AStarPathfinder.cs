using System.Collections.Generic;
using UnityEngine;

// Thuật toán A* tìm đường ngắn nhất trên WalkableGrid.
// - Đi 8 hướng: ngang / dọc chi phí 10, chéo chi phí 14 (xấp xỉ 10 * căn 2).
// - Không cắt góc: chỉ đi chéo khi cả 2 ô ngang và dọc bên cạnh đều đi được (tránh kẹt vào góc vật cản).
// - Heuristic octile (khoảng cách 8 hướng) - không bao giờ ước lượng quá chi phí thật nên đường tìm được là ngắn nhất.
// - Tập mở dùng binary heap để chạy nhanh trên map lớn (~40.000 ô).
public static class AStarPathfinder
{
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    // Trả về danh sách ô từ start tới goal (gồm cả 2 đầu), hoặc null nếu không có đường.
    public static List<Vector3Int> FindPath(WalkableGrid grid, Vector3Int start, Vector3Int goal)
    {
        if (grid == null || !grid.IsWalkable(start) || !grid.IsWalkable(goal)) return null;
        if (start == goal) return new List<Vector3Int> { start };

        BoundsInt bounds = grid.Bounds;
        int count = bounds.size.x * bounds.size.y;

        int[] gCost = new int[count];
        int[] parent = new int[count];
        bool[] closed = new bool[count];
        for (int i = 0; i < count; i++)
        {
            gCost[i] = int.MaxValue;
            parent[i] = -1;
        }

        int startIndex = ToIndex(start, bounds);
        int goalIndex = ToIndex(goal, bounds);
        gCost[startIndex] = 0;

        MinHeap open = new MinHeap();
        open.Push(startIndex, Heuristic(start, goal), Heuristic(start, goal));

        while (open.Count > 0)
        {
            int current = open.Pop();
            if (closed[current]) continue;      // bản ghi cũ trong heap (đã có đường tốt hơn)
            if (current == goalIndex) return BuildPath(parent, current, bounds);
            closed[current] = true;

            Vector3Int cell = ToCell(current, bounds);
            foreach (Vector2Int direction in Directions)
            {
                Vector3Int next = new Vector3Int(cell.x + direction.x, cell.y + direction.y, 0);
                if (!grid.InBounds(next) || !grid.IsWalkable(next)) continue;

                bool diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal)
                {
                    bool sideX = grid.IsWalkable(new Vector3Int(cell.x + direction.x, cell.y, 0));
                    bool sideY = grid.IsWalkable(new Vector3Int(cell.x, cell.y + direction.y, 0));
                    if (!sideX || !sideY) continue;
                }

                int nextIndex = ToIndex(next, bounds);
                if (closed[nextIndex]) continue;

                int newCost = gCost[current] + (diagonal ? DiagonalCost : StraightCost);
                if (newCost >= gCost[nextIndex]) continue;

                gCost[nextIndex] = newCost;
                parent[nextIndex] = current;
                int h = Heuristic(next, goal);
                open.Push(nextIndex, newCost + h, h);
            }
        }

        return null;
    }

    // Bỏ các ô nằm giữa một đoạn thẳng (cùng hướng), chỉ giữ điểm rẽ - đường vẽ gọn và Player đi mượt hơn.
    public static List<Vector3Int> Simplify(List<Vector3Int> path)
    {
        if (path == null || path.Count <= 2) return path;

        List<Vector3Int> result = new List<Vector3Int> { path[0] };
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3Int before = path[i] - path[i - 1];
            Vector3Int after = path[i + 1] - path[i];
            if (before != after) result.Add(path[i]);
        }
        result.Add(path[path.Count - 1]);
        return result;
    }

    // Khoảng cách octile: đi chéo hết phần chung rồi đi thẳng phần còn lại.
    private static int Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Mathf.Min(dx, dy);
    }

    private static List<Vector3Int> BuildPath(int[] parent, int goalIndex, BoundsInt bounds)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        for (int index = goalIndex; index != -1; index = parent[index])
        {
            path.Add(ToCell(index, bounds));
        }
        path.Reverse();
        return path;
    }

    private static int ToIndex(Vector3Int cell, BoundsInt bounds)
    {
        return (cell.y - bounds.yMin) * bounds.size.x + (cell.x - bounds.xMin);
    }

    private static Vector3Int ToCell(int index, BoundsInt bounds)
    {
        int width = bounds.size.x;
        return new Vector3Int(bounds.xMin + index % width, bounds.yMin + index / width, 0);
    }

    // Binary heap nhỏ nhất theo f, bằng nhau thì ưu tiên h nhỏ hơn (gần đích hơn).
    private class MinHeap
    {
        private readonly List<(int index, int f, int h)> items = new List<(int, int, int)>();

        public int Count => items.Count;

        public void Push(int index, int f, int h)
        {
            items.Add((index, f, h));
            int child = items.Count - 1;
            while (child > 0)
            {
                int parentIndex = (child - 1) / 2;
                if (!Less(child, parentIndex)) break;
                Swap(child, parentIndex);
                child = parentIndex;
            }
        }

        public int Pop()
        {
            int top = items[0].index;
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);

            int current = 0;
            while (true)
            {
                int left = current * 2 + 1;
                int right = left + 1;
                int smallest = current;
                if (left < items.Count && Less(left, smallest)) smallest = left;
                if (right < items.Count && Less(right, smallest)) smallest = right;
                if (smallest == current) break;
                Swap(current, smallest);
                current = smallest;
            }
            return top;
        }

        private bool Less(int a, int b)
        {
            return items[a].f < items[b].f || (items[a].f == items[b].f && items[a].h < items[b].h);
        }

        private void Swap(int a, int b)
        {
            (items[a], items[b]) = (items[b], items[a]);
        }
    }
}
