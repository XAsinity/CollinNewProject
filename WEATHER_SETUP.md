# Weather System Setup Guide

This guide walks you through setting up the full weather system in Unity for **CollinNewProject**.  
The system builds on top of the existing `DayNightCycle.cs` and `Custom/DayNightSkybox` shader.

---

## Files Added / Modified

| File | Change |
|------|--------|
| `Assets/Scripts/Weather/WeatherProfile.cs` | **New** — ScriptableObject with per-weather settings |
| `Assets/Scripts/Weather/WeatherManager.cs` | **New** — MonoBehaviour that drives transitions |
| `Assets/Scripts/DayNightCycle.cs` | **Modified** — accepts weather multipliers from WeatherManager |
| `Assets/Shaders/DayNightSkybox.shader` | **Modified** — cloud edge/horizon coverage fix + brightness/shadow props |

---

## Step 1 — Create Weather Profile Assets

Each weather condition is a separate **ScriptableObject asset** you create in the Project window.

1. In the **Project** panel, right-click any folder (e.g. `Assets/Settings/Weather/`) → **Create → Weather → Weather Profile**
2. Name the asset (e.g. `Clear`, `Overcast`, `HeavyStorm`, etc.)
3. In the **Inspector**, every section has tooltips — hover any field to read its description
4. To fill in sane defaults instantly, **right-click the component header** (or use the three-dot menu) → choose one of the **Preset:** entries:

| Preset | `[ContextMenu]` name |
|--------|----------------------|
| Clear | *Preset: Clear* |
| Slightly Cloudy | *Preset: Slightly Cloudy* |
| Partly Cloudy | *Preset: Partly Cloudy* |
| Mostly Cloudy | *Preset: Mostly Cloudy* |
| Overcast | *Preset: Overcast* |
| Super Cloudy | *Preset: Super Cloudy* |
| Light Rain | *Preset: Light Rain* |
| Heavy Storm | *Preset: Heavy Storm* |
| Fog | *Preset: Fog* |
| Snow | *Preset: Snow* |

After choosing a preset the fields auto-fill — click **Apply** if using an older Unity version that requires it.

### Key fields to understand

| Field | What it does |
|-------|--------------|
| `cloudCoverageMin` / `cloudCoverageMax` | A random value between these is picked each transition — gives diversity so "Super Cloudy" still looks slightly different each time |
| `cloudDensity` | How solid/opaque individual cloud formations are |
| `cloudSharpness` | Higher = harder cloud edges |
| `cloudBrightness` | Multiplies the top (lit) side of clouds |
| `cloudDarkness` | How dark the undersides/shadow of clouds are |
| `cloudColor` | Tint blended on top of the time-of-day cloud colour |
| `cloudShadowColor` | Colour used for the shadowed undersides |
| `fogDensityMultiplier` | Multiplied against DayNightCycle's base fog density |
| `overrideFogColor` | If true, `fogColorTint` completely replaces the time-of-day fog colour |
| `sunIntensityMultiplier` | Scales how bright the directional sun light is |
| `starVisibilityMultiplier` | 0 = no stars (overcast), 1 = full star brightness |

---

## Step 2 — Scene Setup

### 2a. Add WeatherManager to the Scene

1. In the **Hierarchy**, right-click → **Create Empty** → rename to `WeatherManager`
2. **Add Component** → search **WeatherManager** → add it
3. In the Inspector, assign:
   - **Day Night Cycle** — drag your existing `DayNightController` (or whichever GameObject holds `DayNightCycle`)
   - **Weather Profiles** — expand the array, set size to 10 (or however many you created), drag each profile asset in
   - **Current Weather** — drag the starting weather profile (e.g. `Clear`)
   - **Transition Duration** — default `10` seconds is a smooth blend; set lower for snappier changes
   - **Auto Weather** — enable and set `Min/Max Time Between Changes` if you want automatic cycling

### 2b. URP Global Volume

The WeatherManager **automatically creates a child `WeatherVolume` GameObject** at runtime with `priority = 1` so it overrides the scene's default volume.  
You do **not** need to set up any Volume manually — it will be created when you press Play.

If you already have a `Volume` component in the scene (Global), it will be left untouched (priority 0).  
The weather volume sits on top of it with its Bloom, Vignette, and Color Adjustments overrides.

---

## Step 3 — Skybox Material

1. Open **Window → Rendering → Lighting** → **Environment** tab
2. Confirm **Skybox Material** is set to `Assets/Materials/Sky/SkyBox.mat`  
   *(this material must use the `Custom/DayNightSkybox` shader — it already does)*
3. The WeatherManager reads the `skyboxMaterial` field from `DayNightCycle` automatically

### New cloud shader properties (available on the material)

| Property | Purpose |
|----------|---------|
| `_CloudBrightness` | Top-face brightness of clouds (default 1.0) |
| `_CloudDarkness` | Shadow darkness on cloud undersides (default 0.5) |
| `_CloudColor` | Tint applied on top of the time-of-day cloud colour |
| `_CloudShadowColor` | Colour blended into the shadowed underside |
| `_CloudHorizonCoverage` | **Cloud edge fix** — how far below the horizon clouds extend (default 0.8) |

### Cloud Horizon / Edge Coverage Fix

Previously, when `_CloudCoverage` was at 1.0, the edges/sides of the skybox still showed clear sky because clouds were masked to `dir.y > 0` (upper hemisphere only).

**The fix:** `_CloudHorizonCoverage` (Range 0–1, default **0.8**) controls how far below the horizon clouds extend.  
The formula is: `horizonPush = _CloudHorizonCoverage × clamp(_CloudCoverage × 2, 0, 1)`

- At low coverage (clear sky) `horizonPush ≈ 0`, so clouds still only appear in the upper sky — no visible change
- At full coverage + `_CloudHorizonCoverage = 0.8`, clouds extend down to `dir.y ≈ -0.8`, wrapping the entire visible sky sphere including the horizon edge
- The transition is smooth (smoothstep) — no hard cutoff line

**Recommended settings for full wrapping:**
- `_CloudHorizonCoverage = 0.8` (default) — good for Overcast / Super Cloudy
- `_CloudHorizonCoverage = 1.0` — fully wrapped even at lower coverage levels
- `_CloudHorizonCoverage = 0.5` — partial wrapping, slight gap at horizon even when overcast

---

## Step 4 — Testing Weather Changes

### In the Inspector (Play Mode)
1. Press **Play**
2. Select the **WeatherManager** GameObject
3. In the Inspector, change the **Current Weather** field to a different profile
4. You'll see the transition begin — skies, fog, and lighting blend over `transitionDuration` seconds

### Via Script
```csharp
// Get the manager
WeatherManager wm = WeatherManager.Instance;

// Transition by profile reference
wm.SetWeather(clearProfile);

// Transition by name (must match WeatherProfile.profileName)
wm.SetWeatherByName("Heavy Storm");
```

### Debug Button
The WeatherManager Inspector has a **Debug Force Random Weather** checkbox.  
Ticking it in Play Mode immediately triggers a random weather change.

### Auto Weather
Enable **Auto Weather** on the WeatherManager and set `Min/Max Time Between Changes`.  
The manager will randomly pick a new profile from the `weatherProfiles` array on a timer.

---

## Step 5 — URP Volume Override Details

The WeatherManager creates overrides for these URP Volume components at runtime:

| URP Effect | Controlled by WeatherProfile field |
|------------|-------------------------------------|
| **Bloom** intensity | `bloomIntensity` |
| **Bloom** threshold | `bloomThreshold` |
| **Vignette** intensity | `vignetteIntensity` |
| **Color Adjustments** post exposure | `colorAdjustmentExposure` |
| **Color Adjustments** contrast | `colorAdjustmentContrast` |
| **Color Adjustments** saturation | `colorAdjustmentSaturation` |

The weather volume sits at **priority 1** so it always wins over the default scene volume (priority 0).  
If you want certain effects (e.g. ambient occlusion) to always be active regardless of weather, put them on the scene volume — they won't be touched by the WeatherManager.

---

## Step 6 — Recommended Workflow for Configuring Profiles

1. Create a profile asset → apply a preset via ContextMenu
2. Press **Play** with that profile set as **Current Weather** on WeatherManager
3. Tweak fields on the ScriptableObject while in Play Mode — changes are live
4. When happy, exit Play Mode — ScriptableObject changes persist (they live in the asset, not the scene)

> **Tip:** The `_CloudHorizonCoverage` on the material controls the edge wrapping globally.  
> Set it to **0.8+** on the `SkyBox.mat` material to ensure overcast profiles wrap the full horizon.

---

## Precipitation

`precipitationType` and `precipitationIntensity` on `WeatherProfile` are **metadata fields** for future particle systems.  
The weather system stores and transitions these values but does not spawn rain/snow particles itself.

To hook in a particle system:
```csharp
void Update()
{
    var wm = WeatherManager.Instance;
    if (wm.currentWeather != null)
    {
        float intensity = wm.currentWeather.precipitationIntensity;
        // Set your rain particle system emission rate etc.
    }
}
```

---

## Quick Reference — Default Profile Values

| Profile | CovMin | CovMax | FogMult | SunMult | Precipitation |
|---------|--------|--------|---------|---------|---------------|
| Clear | 0.00 | 0.05 | 0.3× | 1.00 | None |
| Slightly Cloudy | 0.10 | 0.25 | 0.5× | 0.95 | None |
| Partly Cloudy | 0.30 | 0.50 | 0.7× | 0.85 | None |
| Mostly Cloudy | 0.55 | 0.75 | 1.0× | 0.60 | None |
| Overcast | 0.85 | 0.95 | 1.5× | 0.35 | None |
| Super Cloudy | 0.93 | 0.99 | 2.0× | 0.20 | None |
| Light Rain | 0.70 | 0.85 | 1.8× | 0.40 | Rain 40% |
| Heavy Storm | 0.90 | 0.98 | 2.5× | 0.15 | Rain 100% |
| Fog | 0.30 | 0.50 | 4.0× | 0.50 | None |
| Snow | 0.60 | 0.80 | 1.5× | 0.50 | Snow 70% |

---

## Step 7 — Volumetric Cloud System Setup

The cloud rendering has been extracted from the skybox shader into a dedicated **fullscreen volumetric render pass**. This produces more realistic, three-dimensional clouds with proper depth, self-shadowing, and Henyey-Greenstein light scattering.

### New files

| File | Purpose |
|------|---------|
| `Assets/Shaders/VolumetricClouds.shader` | Fullscreen URP shader that raymarches 32 shell layers per cloud layer |
| `Assets/Scripts/Rendering/VolumetricCloudRenderPass.cs` | `ScriptableRendererFeature` that injects the cloud pass after the skybox |

The `DayNightSkybox.shader` no longer contains any cloud code — it handles sky gradient, sun/moon discs, stars, nebula, aurora, dust, horizon haze, and vignette only.

### 7a. Create the cloud material

1. In the **Project** panel, navigate to `Assets/Materials/Sky/`
2. Right-click → **Create → Material** → name it `VolumetricClouds`
3. In the Inspector, set the **Shader** to `Custom/VolumetricClouds`
4. Tweak the cloud properties to taste (all the same controls as before — coverage, density, scale, etc.)

### 7b. Add the renderer feature

1. Open your **URP Renderer asset** (usually `Assets/Settings/<something>Renderer.asset`)
2. Click **Add Renderer Feature** → select **Volumetric Cloud Render Feature**
3. Assign the `VolumetricClouds` material to the **Cloud Material** slot on the feature

### 7c. Assign the material to WeatherManager

1. Select the **WeatherManager** GameObject in the Hierarchy
2. In the Inspector, find the **Cloud Material** field (just below **References**)
3. Drag the `VolumetricClouds` material into that slot

### 7d. Assign the material to DayNightCycle

1. Select the **DayNightController** (or whichever GameObject holds `DayNightCycle`)
2. In the Inspector, find the **Cloud Material** field (just below **Skybox Material**)
3. Drag the `VolumetricClouds` material into that slot

> **Why both?**  
> `DayNightCycle` pushes `_TimeOfDay`, `_SunDirection`, and `_MoonDirection` every frame so the cloud shader gets correct time-of-day coloring and Henyey-Greenstein light scattering direction.  
> `WeatherManager` pushes all the coverage/density/color/speed properties during weather transitions.

### 7e. Notes

- All **WeatherProfile** fields work exactly as before — they are now routed to the cloud material instead of the skybox material.
- The skybox material should **not** receive cloud properties — WeatherManager no longer sets any `_Cloud*` values on it.
- The cloud pass renders **after the skybox** (`RenderPassEvent.AfterRenderingSkybox`) and **before opaque geometry**, so clouds appear above the sky background and can be occluded by game objects.
- **Shell radius, shell flattening, and zenith blend** are geometry properties — set them on the cloud material directly. WeatherManager intentionally never overrides them during transitions.
- If you do not assign `cloudMaterial` to WeatherManager, a warning is logged and cloud properties are silently skipped — the game will not crash.

