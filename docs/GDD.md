# **Game Design Document – Working Draft**

## Title: 
## **Game Overview**
+ - **Genre:** First-Person Shooter (boomer-shooter inspired, fast-paced combat)
- **Platform:** PC, windows
- **Perspective:** First-person
- **Loop:** Enter level → Clear enemies with pistol & kick combo → Push forward → Repeat

## **Tone & Atmosphere**
+ - **Setting:** Early 2000s New York, imagined through a **late-PS2 lens**.
- **Visual Style:**
    - **Late PS2-style low poly models:** ~2–6k tris for characters, 3–8k for environments.
    - Chunky silhouettes, sharp edges, but **more detail than PS1 blockiness.**
    - Murky baked lighting, rain reflections, dirty textures.
    - Ultra Violent 
- **Mood:** Somber, rainy, oppressive urban decay.
### **Inspirations:**
- Punisher(2004)
- _Manhunt_ (dark, grimy tone).
- _I Am Your Beast_ (combat feel).
## **Gameplay**
### **Core Mechanics**

1. **Pistol (Primary Weapon)**
- Semi-auto, fast, reliable.
- Crunchy, punchy sound.
- Limited ammo pickups encourage aggressive kicks.
  
1. **Kick (Melee Mechanic)**
- Strong melee knockback, slight stun.
- Physics-based ragdoll knockdowns (low-poly style).
- Part of core loop: weaken → slide → kick.

2. **Sliding Movement**
- Momentum-driven slide for traversal and style kills.
- Can fire or kick mid-slide.

3. **Movement**
- Fast boomer-shooter pace.
- No cover system — aggression is the defence.
- Momentum chaining (bunny hopping/sliding).

### **Combat Style**
- **Loop:** Shoot enemies → Slide in → Kick → Keep moving.
- Designed for flow and speed.
- Enemies placed in ambushes and arenas to encourage mobility.

## **Level Design**
- **Theme:**
- PS2-style grimy environments, with **low-poly detail** and moody lighting.
- Interiors: peeling wallpaper, moldy bathrooms, flickering fluorescents.
- Exteriors: rain-soaked streets, neon signs, trash-filled alleys.
- Underground: subways, boiler rooms, dripping water, steam pipes.
- **Structure:**
    - Corridor → Encounter room → Chokepoint → Arena.
    - Levels are semi-linear with some hidden side rooms.

## **Aesthetic & Audio**
- **Graphics (PS2-inspired):**
    - Character polycount ~3–6k, environment assets 1–5k each.
    - Textures: 256×256 to 512×512, gritty & hand-painted with baked dirt/grime.
    - Lighting: baked vertex lighting + minimal dynamic lights (retro mood).
    - Fog + rain + bloom for atmosphere.
- **Audio:**
    - Ambient: distant thunder, traffic hum, muffled neighbours.
    - Weapons: sharp, metallic, echoing gunshots.
    - Kick: crunchy "thack" with ragdoll thud.
    - Music: industrial / lo-fi, occasional silence for tension.

## **Enemies**
- **Thugs:** Basic melee rushers
- **Gunmen:** Pistols, take cover but still reckless.
- **Heavies:** Bigger late-game enemies, harder to stagger with kicks.
- **Style:** Low-poly PS2-level detail, simple animations, gritty textures.

## **Player Progression**
- Primarily **skill-driven progression**.
- No RPG elements, but maybe unlockable modifiers:
    - CRT filter, VHS static, alternate rain density, gun skins (PS2 nostalgia nods).

## **Pacing**
- Fast combat → Quiet hallway → Tense buildup → Explosive firefight.
- Rain and fog create natural pacing between encounters.

## **Technical Notes**

- **Engine:** Unity
- **Art Direction:** Late PS2-style (low poly but detailed).
- **Core Systems Needed:
    - Kick (physics + stun).
    - Sliding system.
    - Enemy AI (patrol, chase, shoot).
    - Retro-style shaders: vertex lighting, gritty low-res textures, fake reflections.