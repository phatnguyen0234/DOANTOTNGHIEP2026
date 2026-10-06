# Luồng Spawn Cây trên Map

## Tổng quan

```mermaid
flowchart TD
    DEV["👨‍💻 Developer\nchạy menu Unity"] --> MENU

    subgraph EDITOR ["🛠️ Editor Tools (chỉ chạy trong Unity Editor)"]
        MENU["Tools → Map → Generate Main Map\nMainMapGenerator.Generate()"]
        LAYOUT["Đọc MainLayout.txt\nMainLevels.txt"]
        TILEMAP["Vẽ lại địa hình\nGround / Sea / Water / Bridge / Forest / Cliff"]
        REMOVE["RemoveHandPlacedObjects()\nXóa cây đặt tay cũ\n(layer Tree / Rock)"]
        DECORATE["MainMapDecorator.Decorate()\nSinh cây + decor mới"]

        MENU --> LAYOUT
        LAYOUT --> TILEMAP
        TILEMAP --> REMOVE
        REMOVE --> DECORATE

        subgraph DECORATE_DETAIL ["MainMapDecorator — chi tiết"]
            LOAD_ASSETS["LoadAssets()\nLoad prefab từ\nAssets/Prefabs/Tree/"]
            CLEAR_ZONES["Tính vùng cấm\n- PreservedTileAreas (nhà đặt tay)\n- GameplayAreas (cửa AreaSwitch, Player)"]
            PASS1["Pass 1: Cầu thang 'S' + Hàng rào 'x'"]
            PASS2["Pass 2: Cây lớn + Bụi\n(có collider, làm mờ)"]
            PASS3["Pass 3: Hoa, cỏ, đá nhỏ\n(không collider)"]

            LOAD_ASSETS --> CLEAR_ZONES --> PASS1 --> PASS2 --> PASS3
        end

        DECORATE --> LOAD_ASSETS
    end

    subgraph PASS2_DETAIL ["Pass 2 — Logic spawn cây theo ký tự layout"]
        F_CELL["Ô 'f' (cỏ rừng)"]
        G_CELL["Ô 'g' (cỏ thường)"]
        CHECK["IsOpenForBig() ✓\nKhông cạnh: w b B S e d x + cliff"]
        SPACE["HasSpace() ✓\nKiểm tra khoảng cách\nvs bigObjects list"]

        F_CELL --> CHECK --> SPACE
        G_CELL --> CHECK

        SPACE -->|"40% chance"| ROLL["Random roll"]
        ROLL -->|"0–50%"| PINE["🌲 PlacePrefab(Pine*)"]
        ROLL -->|"50–65%"| OTHER["🌳 PlacePrefab(OtherTree)"]
        ROLL -->|"65–75%"| FOREST["🌿 PlacePrefab(ForestTree)"]
        ROLL -->|"75–100%"| BUSH["🌱 PlaceBushCluster()"]

        SPACE -->|"3% chance"| STUMP["🪵 CreateSolid(Stump)"]
        SPACE -->|"2% chance"| LOG["🪓 CreateSolid(Log)"]

        G_CELL --> SPACE2["HasSpace(3f)"]
        SPACE2 -->|"2% chance"| TREE_G["🌲 PlacePrefab(Pine/Other)"]
        SPACE2 -->|"1.5% chance"| BUSH_G["🌱 PlaceBushCluster()"]
    end

    subgraph RUNTIME ["🎮 Runtime (lúc chơi game)"]
        PLAYER["Player\ndùng Rìu (Axe tool)"]
        TREE_COMP["Tree.cs\ntrên prefab cây"]
        HIT["Hit() được gọi\n(qua FarmInputController)"]
        SHAKE["Shake coroutine\n+ leafParticles.Play()"]
        FELL["FellTree()\nsau maxHits nhát"]
        DROP["DropSystem.TriggerDrop()\nrơi Gỗ + Hạt giống"]
        DESTROY["Destroy(gameObject, 0.1f)"]

        PLAYER --> HIT
        HIT --> TREE_COMP
        TREE_COMP --> SHAKE
        SHAKE -->|"đủ số nhát"| FELL
        FELL --> DROP
        FELL --> DESTROY
    end

    PASS2 -.->|"PrefabUtility.InstantiatePrefab\n(prefab có Tree.cs)"| TREE_COMP
```

## Các file liên quan

| File | Vai trò |
|---|---|
| [`MainLayout.txt`](file:///d:/Game_Project/Totnghiep2026/Assets/MapLayout/MainLayout.txt) | Bản đồ ký tự định nghĩa loại ô (`f`, `g`, `w`...) |
| [`MainMapGenerator.cs`](file:///d:/Game_Project/Totnghiep2026/Assets/Editor/MainMapGenerator.cs) | Entry point: vẽ địa hình → gọi Decorator |
| [`MainMapDecorator.cs`](file:///d:/Game_Project/Totnghiep2026/Assets/Editor/MainMapDecorator.cs) | Sinh cây/bụi/decor theo random seed |
| [`Assets/Prefabs/Tree/`](file:///d:/Game_Project/Totnghiep2026/Assets/Prefabs/Tree) | Prefab Pine, OtherTree, ForestTree, Bush |
| [`Tree.cs`](file:///d:/Game_Project/Totnghiep2026/Assets/Scripts/Tree.cs) | Runtime: xử lý chặt cây → rơi item |
| [`TreeFade.cs`](file:///d:/Game_Project/Totnghiep2026/Assets/Scripts/TreeFade.cs) | Làm mờ cây khi player đứng sau |
