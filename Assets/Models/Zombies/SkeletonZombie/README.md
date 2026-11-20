# Free Skeleton Zombie

One textured low-poly character with six animations. All files are free for personal and commercial finished projects under LICENSE.txt.

## Contents

- `models/skeleton-zombie.glb`: self-contained character, all six clips and 2K PBR textures.
- `models/skeleton-zombie-mobile.glb`: the same geometry, rig and animation buffers with 1K textures.
- `fbx/skeleton-zombie.fbx`: the same character and six animation takes, with companion maps in `fbx/textures/`.
- `blender/skeleton-zombie.blend`: clean editable scene with packed textures and all six actions.
- `project.godot`: Godot 4.6 animation viewer. Import this project, then run it and select an animation, Replay/Pause and Front/Side.
- `animations.json`: bone names, durations, loop settings, movement speeds and authored event markers.

14,572 triangles; 21 bones; 24 fps; up to four normalized skin weights. Metric scale, Y-up and +Z forward in GLB/Godot. This is a custom character rig rather than a predefined humanoid retargeting preset. Texture variants share identical mesh, skin and animation buffers.

## Animation setup

| Clip | Duration | Playback | Root movement |
| --- | --- | --- | --- |
| ZombieWalk | 1.667 s | Loop | No |
| ZombieRun | 1.000 s | Loop | No |
| ZombieSwipe | 2.000 s | One-shot | No |
| ZombieBite | 1.750 s | One-shot | No |
| ZombieStumble | 3.000 s | One-shot | Yes |
| ZombieDie | 3.000 s | One-shot | Yes |

Walk and run are in-place loops. At normal playback speed, pair them with approximately 0.433 m/s and 1.619 m/s of forward movement. Stumble travels 1.10 m forward. Stumble and death animate the named Root bone; handle their root movement through your engine's root-motion workflow. Avoid applying the same movement twice through a character controller.

Swipe, bite, stumble and death play once. Death holds its final pose. The fitted jaw hinges during the bite and stays closed in the other clips. Root movement and event markers are described per clip in animations.json. Hit detection and gameplay transitions belong in your game.

Every animation explicitly resets the rig when selected. The included viewer supports immediate switches; your game can add crossfades and an animation state machine. This six-action pack does not contain a separate idle animation.

## Formats

GLBs contain embedded PBR textures. Select one texture variant for each character instance. For FBX, use base-color.png as sRGB; normal.png, roughness.png and metallic.png are data maps. FBX material mapping varies between importers. Take names may include a rig-name prefix.

Open the Blender scene in Blender 4.5.13 or later. The walk is active by default. Select another action in the Action Editor; the six NLA tracks are initially muted to avoid stacking animations. All textures are packed into the file.

## Verification and provenance

Both GLBs were freshly imported in Godot 4.6-stable (official), including six clips and all 36 immediate action pairs per variant. FBX was freshly imported in Blender 4.5.13. Final rendered motion, surface contacts, rigid limbs, skull/mandible and self-clearance were checked. Unity and Unreal imports have not been tested.

The base model and textures were generated under the creator's paid Meshy plan. Rigging, locally authored animation and optimization used Blender automation with AI coding assistance. No third-party animation-library motion data is included. See LICENSE.txt for permitted uses; standalone asset redistribution and resale are not permitted.
