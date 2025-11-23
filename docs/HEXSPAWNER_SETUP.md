# HexSpawner Configuration Guide

This guide shows how to properly configure the HexSpawner component in Unity to eliminate warnings about missing prefabs.

## Prerequisites

- Unity Editor open with the project loaded
- Scene containing a HexSpawner GameObject

## Step 1: Locate Required Prefabs

First, find the prefab assets in your Project window:

1. **Hex Prefab** - Should be at `Assets/Prefabs/Hex.prefab` or similar
2. **Hex Text Prefab** - Should contain a TextMeshPro component
3. **Hex Land Prefab** - The 3D model that sits on top of the hex

**To find them:**
- In Unity Project window, search for: `t:Prefab hex`
- Look for prefabs with names like:
  - `Hex` or `HexTile`
  - `HexText` or `NumberText`
  - `HexLand` or `LandModel`

## Step 2: Locate HexSpawner in Scene

1. Open the scene that needs configuration (e.g., `MainScene`)
2. In the Hierarchy window, find the GameObject with the `HexSpawner` component
   - It might be called "HexSpawner" or be a child of "GameSpawner"
3. Click on it to select it

## Step 3: Assign Prefabs in Inspector

With the HexSpawner GameObject selected:

1. **In the Inspector window**, scroll to find the `Hex Spawner (Script)` component

2. **Assign the Hex Prefab**:
   - Find the field labeled `Hex Prefab`
   - Drag the Hex prefab from the Project window into this field
   - OR click the small circle icon next to the field and select the prefab from the picker

3. **Assign the Hex Text Prefab**:
   - Find the field labeled `Hex Text Prefab`
   - This should be a prefab with a TextMeshPro component
   - Drag it into the `Hex Text Prefab` field

4. **Assign the Hex Land Prefab**:
   - Find the field labeled `Hex Land Prefab`
   - This is the 3D model that appears on resource hexes
   - Drag it into the `Hex Land Prefab` field

## Step 4: Verify Configuration

1. With HexSpawner still selected, check that all three fields show assigned prefabs (not "None")
2. Save the scene: `File > Save` or `Ctrl+S`

## Step 5: Test the Configuration

### Option A: Test with Inspector Buttons

If the HexSpawner has [Button] attributes (thanks to Odin Inspector):

1. In the Inspector with HexSpawner selected, look for buttons like:
   - "Spawn Hexes" or "Build"
   - "Update Hexes"
   - "Refresh"
2. Click one of these buttons
3. Check the Console window (`Window > General > Console`)
4. The TextMeshPro warnings should be gone if prefabs are correct

### Option B: Test with Play Mode

1. Click the Play button at the top of Unity
2. The game should spawn hexes automatically
3. Check Console for warnings
4. Stop Play mode

## Troubleshooting

### "I can't find the prefabs"

**Check these locations:**
```
Assets/Prefabs/
Assets/Resources/
Assets/Game/Prefabs/
```

**Search for them:**
1. Project window > Search bar
2. Type: `t:Prefab` (shows all prefabs)
3. Look for hex-related names

### "The prefabs don't have the right components"

**For Hex Prefab:**
- Must have a `Hex` script component
- Should have a MeshRenderer and MeshFilter
- Should have a Collider

**For Hex Text Prefab:**
- Must have a TextMeshPro component (not TextMeshProUGUI)
- Should be 3D text, not UI text

**For Hex Land Prefab:**
- Should have a `HexLandModel` script component
- Should have a 3D model (MeshRenderer)

### "I still get warnings after assigning prefabs"

**Check the hex prefab structure:**

1. Select the Hex prefab in Project window
2. In Inspector, look at its structure
3. The TextMeshPro component should be a **child** of the Hex prefab
   ```
   Hex (Prefab)
   └── HexText (TextMeshPro component here)
   ```

If it's not structured this way:
1. Open the Hex prefab for editing (double-click it)
2. Add a child GameObject: Right-click Hex > Create Empty
3. Name it "HexText"
4. Add TextMeshPro component: `Add Component > TextMesh Pro > Text - TextMeshPro`
5. Save the prefab: `File > Save` or `Ctrl+S`

### "Fields are greyed out / I can't drag prefabs"

This happens if you're in Play mode:
1. Stop Play mode (click the Play button to turn it off)
2. Try again

### "Warnings appear during Play mode but not in Edit mode"

This is normal - runtime instantiation might not create all components immediately. The warnings are informational and won't break functionality.

## Creating Missing Prefabs

If prefabs don't exist at all, you'll need to create them:

### Create Hex Prefab

1. Create a new GameObject: `GameObject > 3D Object > Cylinder` (or your hex shape)
2. Name it "Hex"
3. Add the `Hex` script component: `Add Component > Scripts > Hex`
4. Drag from Hierarchy to Project window to create prefab
5. Delete the GameObject from scene (prefab is now saved)

### Create Hex Text Prefab

1. Create: `GameObject > 3D Object > TextMeshPro - Text`
2. Name it "HexText"
3. Configure the TextMeshPro component:
   - Font size: 2-3
   - Alignment: Center/Center
   - Sort Order: 1 (render on top)
4. Add `HexText` script component if it exists
5. Drag to Project window to create prefab
6. Delete from scene

### Create Hex Land Prefab

1. Import or create your 3D land model
2. Add `HexLandModel` script component
3. Drag to Project window to create prefab

## Quick Check Script

You can check prefab assignments programmatically. In PowerShell:

```powershell
# Run from repository root
.\scripts\check-unity-errors.ps1 -Lines 200
```

If you see warnings like "TextMeshPro component not found", the prefabs need configuration.

## Related Documentation

- [Development Setup](DEV_SETUP.md) - General Unity setup
- [Testing Guide](TESTING.md) - How to test hex spawning
- [Scripts README](../scripts/README.md) - Unity error checking scripts
