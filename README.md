# Crystal Rush - Individual Activity 3

A Unity 6000.3 multiplayer crystal collection activity. Each crystal awards 25 points.

The playable scene is `Assets/Scenes/CrystalArena.unity`. Reusable meshes, materials and prefabs are in `Assets/CrystalRush/Generated/`. Open **CrystalArena.unity**, press Play and click **HOST GAME** to play. See [testing evidence](Evidence/README.md) for the host/client test status and requirement mapping.

## 1. Install packages and generate the scene

Open this project in Unity **6000.3.22f1**. Allow Package Manager to resolve Netcode for GameObjects 2.7.0 and its Unity Transport dependency. Wait for scripts to compile. Select **Crystal Rush > Create Arena and Assets**. Save any scene changes when prompted. The tool creates or opens `Assets/Scenes/CrystalArena.unity` and adds newly created scenes to Build Settings. It will not overwrite an existing arena. Older copies with a scene in `Generated` are moved to `Scenes` with their asset identity and references preserved.

## 2. Inspect the required components

- NetworkManager object: NetworkManager and UnityTransport, with both network prefabs registered.
- NetworkPlayer prefab: NetworkObject, server-authoritative NetworkTransform, and CrystalPlayer (NetworkBehaviour).
- NetworkCrystal prefab: NetworkObject and NetworkCrystal (NetworkBehaviour).
- CrystalGame: connection menu, server collectible spawning, shared scoreboard, and announcement display.

## 3. Run a host and client

Build a Windows player using File > Build Profiles. Run the build and click **HOST GAME**. Enter Play Mode in Unity, keep `127.0.0.1` as the address, and click **JOIN GAME**. You can also host in the editor and join from the build. For two computers on the same LAN, enter the host computer's IPv4 address and allow Unity/the build through the private-network firewall (UDP port 7777). This is a local/LAN demo; no internet relay is configured.

Focus each game window and use **WASD or arrow keys**. Walk into a crystal to collect it. Both windows show every player's score. Disconnect both participants and start a new host session to reset the round.

## 4. Understand the assignment flow

1. Only `IsOwner` reads keyboard input. MoveRpc sends input to the server; the server validates the sender and moves that player's object. NetworkTransform replicates movement.
2. The local player detects a nearby collectible and sends its NetworkObjectId with CollectRpc.
3. The server checks sender ownership, object existence, distance, and the crystal's one-time claim flag.
4. The server adds 25 to `NetworkVariable<int> Score`, initially zero and writable only by the server. All clients read this variable on the scoreboard.
5. AnnounceRpc targets ClientsAndHost and displays the collector's player number and points on every connected participant.
6. The server calls `Despawn(true)` so that the collectible disappears everywhere and cannot award points again.

Movement uses server validation and a short input timeout. Collection never accepts points or a player identity supplied by the client. The arena has 15 crystals (375 total available points). Late joiners receive current scores and remaining spawned objects; earlier transient announcements are not replayed.

## 5. Reusable asset

`Assets/CrystalRush/Crystal.obj` is also included as a standalone 3D model you can drag into Unity or Blender. On first compilation, the setup runs automatically if there are no unsaved scenes and the editor is not in Play Mode; otherwise use the menu in step 1.

The setup tool creates an original faceted crystal mesh (`FacetedCrystal.asset`), teal material, and **CrystalVisual.prefab**, which can be reused independently of networking. **NetworkCrystal.prefab** is the networked version. These assets are generated locally from the included source; no paid downloads are required. You may use and modify the original crystal asset for this project or other projects.

## 6. Required testing evidence

Record both game windows while completing these checks; do not submit this checklist as proof that tests ran.

- Host starts and client connects; two player objects are visible on both sides.
- Move host, then client: each window controls only its own player and observes the other's movement.
- Both players start at zero and see all 15 crystals.
- Host picks up one crystal: host score becomes 25 on BOTH windows, both receive the announcement, and the crystal disappears on both.
- Client picks up another: client score becomes 25 on BOTH windows and both receive the correct player announcement.
- Both approach the same crystal: it awards only 25 points to one player and disappears once.
- Join after collection: current scores and only remaining crystals appear.
- Disconnect the client and reconnect; the new player begins at zero. Disconnect the host; the client exits the session.
- Clear the arena; total awarded points for players who stayed connected is 375.

## 7. GitHub submission

Create your repository as `GameNetworking_RPC_SURNAME_FIRSTNAME_COURSE_SECTION`, using your actual details. Include Assets (with all .meta files), Packages, ProjectSettings, .gitignore, this README, and your recorded evidence. Do not include Library, Temp, Logs, or builds. Uploading or publishing the project is a separate action; no repository is created automatically.

The project includes `Assets/`, `Packages/`, `ProjectSettings/`, `.gitignore`, `.gitattributes`, `Evidence/`, and `Tools/`. In PowerShell, run `./Tools/Package-Submission.ps1` to create a source-project ZIP in `Submission/`; this excludes generated caches, local builds and Git history. On GitHub, upload the extracted project files at the repository root so the instructor can inspect the code directly. Keep `.meta` files and `Packages/packages-lock.json`.

To create a playable Windows build, use **Crystal Rush > Build Windows Test Player**. The result is `Builds/CrystalRush/CrystalRush.exe`; keep the executable with its accompanying folders. Builds are local convenience files and are excluded from the source submission.

API reference: https://docs.unity.cn/Packages/com.unity.netcode.gameobjects%402.7/manual/advanced-topics/message-system/rpc.html
