# Game Design Document – Working Draft

## Title:

## Game Overview

* Genre: First-Person Shooter (boomer-shooter inspired, fast-paced combat)
* Platform: PC, Windows
* Perspective: First-person
* Loop: Enter level → Clear enemies with pistol & kick combo → Push forward → Repeat
* Core Identity: A fusion of Hotline Miami’s stylish, high-intensity violence and I Am Your Beast’s speed-driven FPS flow.

## Tone & Atmosphere

* Setting: Early 2000s New York, imagined through a late-PS2 lens.
* Visual Style:

  * Late PS2-style low poly models: \~2–6k tris for characters, 3–8k for environments.
  * Chunky silhouettes, sharp edges, but more detail than PS1 blockiness.
  * Murky baked lighting, rain reflections, dirty textures.
  * Ultra violent, messy feedback.
* Mood: Somber, rainy, oppressive urban decay, cut with bursts of neon carnage and surreal style moments.

### Inspirations:

* The Punisher (2004)
* Manhunt (dark, grimy tone)
* Hotline Miami (stylish rhythm of violence, flow, and audiovisual punch)
* I Am Your Beast (combat feel, speed, aggression)
* Superhot (cambot , without speed)

## Gameplay

### Core Mechanics

1. Pistol (Primary Weapon)

   * Semi-auto, fast, reliable.
   * Crunchy, punchy sound.
   * Limited ammo pickups encourage aggressive kicks.

2. Kick (Melee Mechanic)

   * Strong melee knockback, slight stun.
   * Physics-based ragdoll knockdowns (low-poly style).
   * Part of core loop: weaken → slide → kick.

3. Sliding Movement

   * Momentum-driven slide for traversal and style kills.
   * Can fire or kick mid-slide.

4. Movement

   * Fast boomer-shooter pace.
   * No cover system — aggression is the defence.
   * Momentum chaining (bunny hopping/sliding).

5. No Reload System

   * The game has no traditional reload.
   * Instead: Throw weapon > reload.
   * Player can hurl empty/half-empty guns at enemies for damage or stun, then grab another weapon.
   * Encourages flow and improvisation, in the spirit of Hotline Miami.

### Combat Style

* Loop: Shoot enemies → Slide in → Kick → Keep moving.
* Designed for flow and rhythm: kills chain together in a Hotline Miami-like rhythm of violence, but in first-person with speed and intensity like I Am Your Beast.
* Enemies are placed in ambushes and arenas to encourage mobility and flow-state combat.

## Level Design

* Theme:

  * PS2-style grimy environments, with low-poly detail and moody lighting.
  * Interiors: peeling wallpaper, moldy bathrooms, flickering fluorescents.
  * Exteriors: rain-soaked streets, neon signs, trash-filled alleys.
  * Underground: subways, boiler rooms, dripping water, steam pipes.
* Structure:

  * Corridor → Encounter room → Chokepoint → Arena.
  * Semi-linear levels with hidden side rooms for ammo/style bonuses.
  * Flow-focused layouts: arenas designed to encourage chaining kills without stopping.

## Aesthetic & Audio

* Graphics (PS2-inspired):

  * Character polycount \~3–6k, environment assets 1–5k each.
  * Textures: 256×256 to 512×512, gritty & hand-painted with baked dirt/grime.
  * Lighting: baked vertex lighting + minimal dynamic lights (retro mood).
  * Fog + rain + bloom for atmosphere.
* Audio:

  * Ambient: distant thunder, traffic hum, muffled neighbours.
  * Weapons: sharp, metallic, echoing gunshots.
  * Kick: crunchy "thack" with ragdoll thud.
  * Music: Industrial / lo-fi with bursts of surreal synth intensity (Hotline Miami influence). Tracks push flow-state, then cut to silence for tension.

## Enemies

* Thugs: Basic melee rushers.
* Gunmen: Pistols, take cover but still reckless.
* Heavies: Bigger late-game enemies, harder to stagger with kicks.
* Style: Low-poly PS2-level detail, simple animations, gritty textures.

## Player Progression

* Primarily skill-driven progression.
* No RPG elements, but unlockable modifiers:

  * CRT filter, VHS static, alternate rain density, gun skins (PS2 nostalgia nods).
  * Hotline Miami-style “mask/skin modifiers” could be aesthetic unlocks.

## Pacing

* Rhythmic Violence Flow: Fast combat chains (Hotline Miami vibe) → Quiet hallway (tension) → Explosive firefight (I Am Your Beast energy).
* Rain and fog naturally punctuate pacing between encounters.

## Technical Notes

* Engine: Unity
* Art Direction: Late PS2-style (low poly but detailed).
* Core Systems Needed:

  * Kick (physics + stun).
  * Sliding system.
  * Enemy AI (patrol, chase, shoot).
  * Retro-style shaders: vertex lighting, gritty low-res textures, fake reflections.
  * Flow-state scoring system (optional): Style points for kill chaining, à la Hotline Miami.
  * No reload system: throw weapons instead of reloading.

