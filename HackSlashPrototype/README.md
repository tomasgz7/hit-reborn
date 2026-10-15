# Hack & Slash Prototype — Fase 1: núcleo de combate

Unity 2022.3 LTS o Unity 6 · URP · **Input System** (paquete `com.unity.inputsystem`).
Copiá la carpeta `Assets/_Project` dentro de tu proyecto.

## Arquitectura

```
Input (PlayerInputReader) ──evento──▶ InputBuffer
                                          │
PlayerController (contexto) ── StateMachine ── Locomotion / Attack / Dodge / HitStun / Dead
                                          │
           AttackState ── lee ──▶ AttackData / ComboData (ScriptableObjects)
                │
             Hitbox ──HitContact──▶ AttackState.OnHit
                                         ├─ HitstopController (atacante y víctima)
                                         ├─ IDamageable.TakeDamage ─▶ Health ─evento─▶ EnemyHitReaction
                                         └─ HitEventChannel (SO) ─▶ CameraShaker, HitVfxSpawner, (audio, UI…)
```

| Carpeta | Contenido |
|---|---|
| `Core/` | `StateMachine` (C# puro), `HitstopController` |
| `Combat/` | `AttackData`, `ComboData`, `Hitbox`, `Health`, `TargetAssist`, `DamageInfo` |
| `Input/` | `InputBuffer` (C# puro, testeable), `PlayerInputReader` |
| `Player/` | `PlayerController` + estados |
| `Enemy/` | `EnemyHitReaction` (training dummy) |
| `Events/` | `HitEventChannel` (ScriptableObject event channel) |
| `Feedback/` | `CameraRig`, `CameraShaker`, `HitVfxSpawner` |

---

## 1. Layers y física

**Project Settings → Tags and Layers**
- Layer 6: `Player`
- Layer 7: `Enemy`

**Project Settings → Physics → Layer Collision Matrix**
- Desmarcá `Player × Enemy` si no querés que se empujen al chocar (recomendado: dejarlo marcado para que no se atraviesen; el lunge ya se acorta con `TargetAssist`).

Los hitboxes **no usan colliders propios**: son consultas `OverlapBox`, filtradas por `LayerMask`.

---

## 2. Input Actions

`Create → Input Actions` → `PlayerControls.inputactions`. Action Map **Gameplay**:

| Action | Tipo | Bindings |
|---|---|---|
| `Move` | Value / Vector2 | WASD (2D Vector composite), Left Stick |
| `LightAttack` | Button | `J`, Mouse Left, Gamepad West (□/X) |
| `HeavyAttack` | Button | `K`, Mouse Right, Gamepad North (△/Y) |
| `Dodge` | Button | `Space`, Gamepad South (✕/A) |

Sin *Interactions* (press por defecto). Para mobile tipo HIT: Canvas con `On-Screen Stick` (Control Path `<Gamepad>/leftStick`) y `On-Screen Button` por acción (`<Gamepad>/buttonWest`, etc.). No hay que tocar código.

**Player Settings → Active Input Handling**: `Input System Package (New)` o `Both`.

---

## 3. ScriptableObjects

`Create → HackSlash → …`

### Hit Event Channel
Creá **uno**: `EVT_PlayerHit`. Se arrastra al Player, al CameraShaker y al HitVfxSpawner.

### Attack Data — valores de partida (combo de espada de 4 golpes)

| Asset | State | Duration | Active | Combo | DodgeCancel | Recovery | Dmg | Knock | Hitstop A/T | Shake | Lunge |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `ATK_Light1` | Light1 | 0.45 | 0.25–0.45 | 0.35 | 0.10 | 0.70 | 10 | 3 | 0.05 / 0.07 | 0.15 | 1.0 |
| `ATK_Light2` | Light2 | 0.45 | 0.25–0.45 | 0.35 | 0.10 | 0.70 | 10 | 3 | 0.05 / 0.07 | 0.15 | 1.0 |
| `ATK_Light3` | Light3 | 0.55 | 0.30–0.50 | 0.45 | 0.15 | 0.75 | 14 | 4 | 0.06 / 0.09 | 0.20 | 1.3 |
| `ATK_Light4` | Light4 (finisher) | 0.80 | 0.35–0.55 | 0.65 | 0.25 | 0.85 | 25 | 9 | 0.10 / 0.14 | 0.45 | 2.0 |
| `ATK_HeavyStarter` | HeavyStarter | 0.90 | 0.40–0.55 | 0.70 | 0.30 | 0.85 | 22 | 8 | 0.09 / 0.12 | 0.40 | 1.5 |
| `ATK_L1_Heavy` | HeavyBranch1 (lanzador) | 0.75 | 0.35–0.50 | 0.60 | 0.25 | 0.85 | 18 | 6 | 0.08 / 0.11 | 0.30 | 1.2 |
| `ATK_L2_Heavy` | HeavyBranch2 (giro) | 0.85 | 0.25–0.60 | 0.70 | 0.30 | 0.85 | 20 | 7 | 0.08 / 0.12 | 0.35 | 0.5 |

- **Duration** = largo del clip ÷ `Speed` del estado en el Animator. Si no coincide, el golpe "corta" antes o la animación termina antes que la lógica.
- Para un giro 360° (`ATK_L2_Heavy`) usá `Hitbox Center (0,1,0)` y `Size (4,1.5,4)`.
- Las ventanas se tunean jugando: seleccioná el Player en Play Mode y mirá el **gizmo rojo** del hitbox (amarillo = inactivo).

### Combo Data — `CMB_Sword`
- `Light Chain`: Light1, Light2, Light3, Light4
- `Heavy Branches`: [0] = `ATK_L1_Heavy`, [1] = `ATK_L2_Heavy` (índices sin rama: dejalos vacíos)
- `Heavy Starter`: `ATK_HeavyStarter`

---

## 4. Animator Controller — `AC_Player`

**Base Layer** (sin transiciones entre ataques: **todo se dispara desde código** con `CrossFadeInFixedTime`):

| Estado | Contenido | Notas |
|---|---|---|
| `Locomotion` (default) | Blend Tree 1D, parámetro `Speed` (float): 0 = Idle, 1 = Run | Clips en Loop |
| `Light1`…`Light4` | Clip de cada golpe | **Loop Time OFF** |
| `HeavyStarter`, `HeavyBranch1`, `HeavyBranch2` | Clips pesados | Loop OFF |
| `Dodge` | Roll / dash | Loop OFF, duración ≈ `Dodge Duration` (0.4s) |
| `Hit` | Reacción de golpe | Loop OFF, corta (0.3s) |
| `Death` | Muerte | Loop OFF |

Configuración clave:
- **Parámetros**: solo `Speed` (float).
- **Cada golpe del combo = un estado distinto**, aunque repitas clip. `CrossFade` hacia el estado que ya está activo no lo reinicia de forma fiable.
- **Sin transiciones de salida** en los ataques: cuando terminan, el código vuelve a `Locomotion`. Si querés, dejá una transición de seguridad `Any State` → nada; no es necesaria.
- **Write Defaults**: mismo valor en todos los estados (ON u OFF, pero consistente) para evitar poses raras.
- **Apply Root Motion**: OFF (el código lo fuerza igual). El desplazamiento lo dan el `lunge` y la curva de esquiva → tuneable sin re-exportar animaciones.
- En el **import del clip** (pestaña Animation) de ataques: `Root Transform Position (XZ) → Bake Into Pose` ✔ si el clip trae desplazamiento propio; si no, el personaje "patina" hacia atrás al terminar.
- **Animator → Update Mode**: `Normal`. **Culling Mode**: `Always Animate` en el jugador.

**Enemy (`AC_Dummy`)**: `Idle` (default), `Hit`, `Death`. Sin parámetros. `Hit` con una transición con Exit Time → `Idle`.

> Las animaciones de Mixamo funcionan perfecto para prototipar (Great Sword Pack / Sword and Shield Pack).

---

## 5. Prefab del Player

```
Player                      [Layer: Player]
├─ CharacterController      (Height 1.8, Radius 0.35, Center (0,0.9,0))
├─ PlayerController
├─ PlayerInputReader
├─ Health                   (Max 100)
├─ Hitbox                   (Target Mask: Enemy · Owner: Health del Player · Gizmo Preview: ATK_Light1)
├─ HitstopController        (Animator: el del modelo)
├─ TargetAssist             (Target Mask: Enemy · Radius 4.5)
└─ Model                    (FBX con Animator · Controller: AC_Player · Avatar)
```

**PlayerInputReader**: arrastrá las 4 `InputActionReference` (expandí el `.inputactions` en el Project y arrastrá cada acción).

**PlayerController**: Animator del Model, el resto de componentes del root, `Combo: CMB_Sword`, `Hit Channel: EVT_PlayerHit`. `Camera Transform`: el **CameraRig** (no la cámara hija que tiembla).

Por qué el Hitbox va en el **root** y no en el arma: el root solo rota en Y y siempre mira hacia donde ataca → el box es predecible y tuneable por AttackData. Pegar el hitbox al hueso del arma da arcos más "realistas" pero mucho más inconsistentes; HIT y la mayoría de ARPG mobile usan volúmenes en el root.

---

## 6. Prefab del Enemigo (training dummy)

```
Dummy                       [Layer: Enemy]
├─ CapsuleCollider          (o CharacterController)
├─ Health                   (Max 300)
├─ HitstopController
├─ EnemyHitReaction         (Visual Root: Model · Renderers: los SkinnedMeshRenderer · Color Property: _BaseColor)
└─ Model                    (Animator: AC_Dummy)
```

- El collider va en el **root** (layer Enemy), el temblor se aplica al **Model** hijo: así el hitbox no "ve" moverse al enemigo durante el hitstop.
- `Color Property`: `_BaseColor` en URP Lit / `_Color` en Built-in Standard. Para un flash más "glow", usá un shader con emisión y poné `_EmissionColor` (con Emission activada en el material).

---

## 7. Cámara y feedback

```
CameraRig                   (CameraRig: Target = Player, Offset (0, 9, -7.5))
└─ Main Camera              (CameraShaker: Hit Channel = EVT_PlayerHit)

CombatFeedback              (HitVfxSpawner: Hit Channel = EVT_PlayerHit, prefab de partículas)
```

Prefab de VFX: `ParticleSystem` en el root, **Looping OFF**, **Play On Awake OFF**, Duration ~0.3, Burst de 10–20 partículas, Simulation Space World.

---

## 8. Cómo leer el "feel" (qué tocar cuando algo se siente mal)

| Síntoma | Ajuste |
|---|---|
| "Aprieto y no sale el golpe" | Bajar `ComboWindowStart` o subir `Buffer Window` (0.3 → 0.4) |
| "Los combos salen solos, siento que no controlo" | Bajar `Buffer Window` (0.2) |
| "Los golpes no pesan" | Subir hitstop (0.08–0.12 en finishers) y `Shake`; bajar `CrossFade` a 0.03 |
| "Se siente lento / pesado de más" | Bajar `RecoveryStart` y `DodgeCancelStart` |
| "Atravieso al enemigo" | Subir `Lunge Stop Distance` |
| "El auto-aim me gira hacia enemigos que no quiero" | Bajar `Max Angle With Input` o subir `Angle Weight` |
| "La esquiva no me salva" | Ampliar ventana i-frames (`IFrameStart` 0, `IFrameEnd` 0.8) |

Regla práctica: **hitstop de la víctima ≥ hitstop del atacante**, y los finishers ~2× que los golpes normales.

---

## Próximas fases sugeridas
1. **IA enemiga** con la misma `StateMachine` (Idle → Chase → Telegraph → Attack → HitReact) y su propio `Hitbox` apuntando a `Player`.
2. **Super Armor / Poise**: los enemigos grandes no entran en hitstun hasta romper su barra de poise.
3. **Skills** como `AttackData` con cooldown + energía, y **Launcher / Juggle** (estado aéreo).
4. **Animation Events opcionales** para SFX de swing (whoosh) y huellas, sin acoplar la lógica de combate.
5. **UI de combo counter** suscripta al `HitEventChannel`.
