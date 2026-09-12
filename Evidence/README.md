# Evidence of testing

**Capture status: not ready for submission.** The local host/client state checks passed, but the current PNGs are black because the test ran in hidden windows. They must be replaced by a visible-window capture and pass `Verify-Evidence.ps1` before submission. The screenshot mapping below describes the intended capture sequence.

These are screenshots captured directly from two running Windows development builds of Crystal Rush: one host and one client connected through Unity Transport on localhost. An opt-in integration-test driver supplies movement input to each owning player. Movement, pickup requests, score replication, announcements and despawning use the game's normal Netcode for GameObjects code. No scores or collectible states are prefilled by the test.

The `host-` and `client-` images at each numbered stage show the same session from both participants. The matching JSON files record actual network state at capture time; they supplement the screenshots.

| Requirement | Evidence |
| --- | --- |
| 1. Host running | `host-01-connected.png`: HOST / ONLINE |
| 2. Client connected | `client-01-connected.png`: CLIENT / ONLINE, both players present |
| 3. Players moving | Compare `01-connected`, `02-host-moving`, and `04-client-moving` images; both players change position in their respective input phases |
| 4. Collectibles visible | Both `01-connected` images show 15 crystals |
| 5. Player collecting an object | Both `03-host-collected` images show Player 1's successful pickup; `05-client-collected` shows Player 2's |
| 6. Score increasing | Player 1 goes from 0 to 25 in stage 03; Player 2 goes from 0 to 25 in stage 05 |
| 7. Score synchronized | Compare host and client scoreboards at stages 03 and 05 |
| 8. Collection announcement | Stage 03 identifies Player 1 and +25 points; stage 05 identifies Player 2 and +25 points |
| 9. Collectible removed/despawned | The two south-row crystals disappear between stages 01, 03 and 05; state captures show 15 -> 14 -> 13 crystals |

Stage `06-no-repeat` checks that scores remain 25/25 and only 13 crystals remain after both players have stayed near their collected objects. This verifies the two collected objects do not award again; it is not a simultaneous-contention test.

## Review the screenshots

Initial host:

![Host connected, initial scores](host-01-connected.png)

Initial client:

![Client connected, initial scores](client-01-connected.png)

After the host collects:

![Host receives Player 1 collection](host-03-host-collected.png)

![Client receives Player 1 collection](client-03-host-collected.png)

After the client collects:

![Host receives Player 2 collection](host-05-client-collected.png)

![Client receives Player 2 collection](client-05-client-collected.png)

## Repeat the test

1. Open `Assets/Scenes/CrystalArena.unity` in Unity 6000.3.22f1.
2. Select **Crystal Rush > Build Windows Test Player** and wait for `CRYSTAL_RUSH_BUILD_OK`.
3. From PowerShell in the project folder, run `./Tools/Run-Evidence.ps1 -OutputFolder Evidence-Rerun -ShowWindows`. Keep both game windows open, unminimized and visible; move them side by side if needed. Hidden game windows can produce black images.
4. Wait about one minute; both game instances exit automatically.
5. Run `./Tools/Verify-Evidence.ps1 -OutputFolder Evidence-Rerun`.

The driver is active only in development builds with explicit `--cr-evidence` command-line arguments. Launching the game normally gives the interactive host/client menu and keyboard controls. The test does not establish internet connectivity, late-join behavior, simultaneous pickups, keyboard usability, or behavior on another physical computer; those need separate manual checks.
