"""Generates the Ashen Road CRC cards (as built) as PDF and DOCX, in the layout of the original card sheet.

Run:  uv run --with reportlab --with python-docx python3 Tools/crc_cards.py <output folder>
Writes Ashen_Road_CRC_Cards_v2.pdf and Ashen_Road_CRC_Cards_v2.docx.
"""
import os
import sys

TITLE = "Ashen Road"
SUBTITLE = "CRC Cards  |  EC-334 Game Design and Development"
MEMBERS = ["Hassan Ayoub (BCSF23M004)", "Hafiz Own (BCSF23M018)", "Abdul Basit (BCSF23M022)"]
FOOTER = "Ashen Road CRC Cards"
NAME = "Ashen_Road_CRC_Cards_v2"

# (class, subtitle, responsibilities, collaborators)
CARDS = [
    # ---------------------------------------------------------------- core
    ("IState", "Interface. Implemented by: PlayerState, EnemyState, BossState",
     ["Declare Enter, Tick, FixedTick and Exit hooks"], []),
    ("StateMachine", "Plain C# class",
     ["Know current and previous state", "Change state (Exit old, Enter new)", "Ignore re-entry of the active state unless forced",
      "Forward Tick / FixedTick to current state", "Return to the previous state", "Announce every change (StateChanged)"],
     ["IState"]),
    ("GameState / GameStateRules", "Enum + static transition table",
     ["List the game states (Boot, MainMenu, Loading, Playing, Paused, GameOver, LevelComplete, Ending, GameFinished)",
      "Know which transitions are legal", "Say which states are gameplay"], []),
    ("GameEvents", "Static event channel (Observer)",
     ["Hold the global game events (state, level, player, checkpoint, front, boss, hits, sounds, effects, hints)",
      "Provide safe Raise methods", "Clear all listeners between tests", "Keep no game state"], []),
    ("GameManager", "Singleton",
     ["Persist one instance across scenes", "Know game state, level, checkpoint, death cause, last result, deaths",
      "Validate state transitions and announce them", "Start, retry, restart or advance levels; go to menu",
      "Pause and resume (time scale, input map)", "Complete a level: save best time, unlock next level",
      "React to player death and checkpoints", "Begin the ending and finish the game"],
     ["GameEvents", "GameStateRules", "SceneLoader", "SaveSystem", "LevelCatalog", "InputReader", "ScreenFader", "LevelResult"]),
    ("GameBootstrap", "Static",
     ["Create the persistent Systems prefab before the first scene loads", "Make Play work from any scene"],
     ["GameManager"]),
    ("SceneLoader", "Static",
     ["Load the menu or level scene asynchronously", "Know loading progress", "Know which scene is active"], []),
    ("SaveSystem", "Plain C# class. Uses: ISaveStore (PlayerPrefsStore, MemorySaveStore)",
     ["Know highest unlocked level; unlock levels", "Record and report best times", "Know music and effects volume",
      "Know whether the game was finished", "Reset progress, keeping volumes"],
     ["ISaveStore"]),
    ("ObjectPool<T>", "Generic (Object Pool)",
     ["Pre-create inactive copies of a prefab", "Hand out and take back items", "Recycle the oldest item when full"], []),
    ("Layers", "Static",
     ["Know the physics layer numbers and masks", "Decide which layers collide (one source of truth)"], []),
    # ---------------------------------------------------------------- data
    ("PlayerStats", "ScriptableObject",
     ["Know movement tuning (speed, acceleration, air control)", "Know jump tuning (height, time to apex, coyote time, buffer)",
      "Compute gravity and jump velocity from height and time", "Know roll tuning (duration, speed, cooldown)",
      "Know sword tuning (damage, timings, hitbox, lunge)", "Know health tuning (hearts, invulnerability, knockback)",
      "Compute jump reach for the level checker"], []),
    ("EnemyData", "ScriptableObject (Type Object)",
     ["Know identity (name, layout symbol, prefab, sprite, animation)", "Know stats (health, speeds, sight, patrol)",
      "Know attack style, timings, hitbox and knockback", "Know contact damage", "Know immunities (Brute ignores Sword)",
      "Know despawn delay and death sound"],
     ["CharacterArt"]),
    ("BossData", "ScriptableObject",
     ["Know max health and phase-two threshold", "Know per-phase speed, telegraph and recovery times",
      "Know charge and summon settings", "Know the summoned enemy type"],
     ["EnemyData"]),
    ("LevelData", "ScriptableObject",
     ["Know index, name, subtitle and boss-level flag", "Know the text layout, hints and moving-platform tuning",
      "Know tiles (with variants), backgrounds and tints", "Know decorations and backdrop scenery",
      "Know disaster type, front sprites and speed profile", "Know fire timings, ambient debris, music and par time"],
     []),
    ("LevelCatalog", "ScriptableObject",
     ["Know the ordered list of levels", "Look up a level by index; report count and last level",
      "Find the EnemyData for a layout symbol", "Know the boss and player stats"],
     ["LevelData", "EnemyData", "BossData", "PlayerStats"]),
    ("LevelPrefabs", "ScriptableObject",
     ["Know every prefab a level is built from (knight, props, hazards, boss, princess, front)"], []),
    ("SpawnEntry", "Struct",
     ["Know an enemy type and where it spawns"], ["EnemyData"]),
    ("AudioLibrary", "ScriptableObject",
     ["Know every sound and music track by id", "Know volume, pitch variance and repeat limit per sound"], []),
    ("CharacterArt / SpriteClip", "ScriptableObject + serializable clip",
     ["Know a character's named animation clips", "Know each clip's frames, speed and looping"], []),
    # ---------------------------------------------------------------- input
    ("InputReader", "Only class that touches the Input System",
     ["Know the horizontal move axis and jump-held", "Raise Jump, Roll, Attack, Pause and Cancel events",
      "Switch between Gameplay and UI action maps"],
     ["InputBindings"]),
    ("InputBindings", "Static",
     ["Hold the action maps and default key bindings as JSON (incl. J, 0, click to attack)"], []),
    # ---------------------------------------------------------------- combat
    ("IDamageable", "Interface. Implemented by: Health",
     ["Accept damage and report whether it was applied", "Report whether it is alive"],
     ["DamageInfo"]),
    ("DamageInfo", "Struct",
     ["Know amount, damage type, source, hit point", "Know knockback direction and force, lethal flag"], []),
    ("HealthModel", "Plain C# class",
     ["Know current and max health", "Apply damage honoring invulnerability, immunity and lethal bypass",
      "Heal up to the maximum", "Run timed and manual invulnerability"],
     ["DamageInfo"]),
    ("Health", "Implements: IDamageable",
     ["Configure max health and immunities", "Raise Changed, Damaged, Died and Blocked events",
      "Start invulnerability windows; kill outright"],
     ["HealthModel", "DamageInfo"]),
    ("Hitbox", "Component",
     ["Know damage, type, knockback, lethal flag and target layers", "Open and close for an attack window",
      "Hit each target once per opening", "Report every hit (applied or blocked)"],
     ["IDamageable", "DamageInfo"]),
    ("ContactDamage", "Component",
     ["Hurt the knight on touch, limited by his invulnerability"], ["IDamageable", "DamageInfo"]),
    # ---------------------------------------------------------------- player
    ("PlayerController", "Facade for the knight",
     ["Wire sibling components and apply stats", "Own and tick the player state machine", "Latch buffered input (jump, roll, attack)",
      "Accept scripted movement for cut-scenes and tests", "Enter Hurt / Dead state on damage / death",
      "Relay health and death as global events"],
     ["PlayerMovement", "PlayerCombat", "Health", "PlayerStats", "StateMachine", "PlayerState", "InputReader", "GameEvents"]),
    ("PlayerMovement", "Component",
     ["Accelerate and decelerate horizontally", "Apply designed gravity; jump with coyote time and jump cut",
      "Roll with cooldown, passing through enemies", "Detect ground and landing; ride moving platforms",
      "Apply knockback, lunge and teleport"],
     ["PlayerStats", "MovingPlatform", "GameEvents"]),
    ("PlayerCombat", "Component",
     ["Start a swing when cooldown allows", "Run wind-up, active and recovery phases", "Open the sword hitbox for active frames only",
      "Count kills; trigger hit stop, shake and sparks"],
     ["Hitbox", "PlayerStats", "GameEvents"]),
    ("PlayerState", "Abstract. Implements: IState. Subclasses: Grounded, Airborne, Rolling, Attacking, Hurt, Dead",
     ["Know the owning PlayerController", "Provide empty state hooks", "Request state changes; start attacks from free states"],
     ["PlayerController", "StateMachine"]),
    ("PlayerGroundedState", "Superclass: PlayerState",
     ["Apply horizontal input and footsteps", "Go airborne on jump or ground lost", "Go rolling / attacking when ready"],
     ["PlayerMovement", "PlayerCombat", "PlayerAirborneState", "PlayerRollingState", "PlayerAttackingState"]),
    ("PlayerAirborneState", "Superclass: PlayerState",
     ["Apply air control", "Allow coyote jumps and air attacks", "Return to grounded on landing"],
     ["PlayerMovement", "PlayerAttackingState", "PlayerGroundedState"]),
    ("PlayerRollingState", "Superclass: PlayerState",
     ["On enter: start roll and invulnerability", "On exit: restore layer, start cooldown", "Return to grounded / airborne"],
     ["PlayerMovement", "Health"]),
    ("PlayerAttackingState", "Superclass: PlayerState",
     ["On enter: begin swing, lock facing, lunge", "Allow slowed movement", "Return when the swing ends"],
     ["PlayerCombat", "PlayerMovement"]),
    ("PlayerHurtState", "Superclass: PlayerState",
     ["Cancel the swing, apply knockback", "Block input during the stun, then return"],
     ["PlayerMovement", "PlayerCombat"]),
    ("PlayerDeadState", "Superclass: PlayerState",
     ["Lock input, stop the knight", "Switch to the corpse layer"], ["PlayerMovement"]),
    # ---------------------------------------------------------------- enemies
    ("EnemyAI", "Component (one class for every enemy type)",
     ["Initialize components from EnemyData", "Own the enemy state machine", "Know spawn anchor",
      "React to hits (stun), blocked hits (flinch) and death", "Raise kill events; despawn"],
     ["EnemyData", "Health", "EnemyMotor", "EnemyPerception", "Hitbox", "ContactDamage", "StateMachine", "EnemyState", "GameEvents"]),
    ("EnemyMotor", "Component. Also used by the boss",
     ["Move horizontally and face travel direction", "Detect walls and ledges ahead", "Apply knockback with resistance", "Freeze on death"],
     []),
    ("EnemyPerception", "Component",
     ["Detect the knight in range (front and rear) with line of sight", "Know target position, distance and direction",
      "Remember the target briefly after losing it"],
     ["PlayerController"]),
    ("EnemyState", "Abstract. Implements: IState. Subclasses: Patrol, Chase, Attack, Stunned, Dead",
     ["Know the owning EnemyAI", "Provide hooks, a change helper and Resume (chase or patrol)"],
     ["EnemyAI", "StateMachine"]),
    ("EnemyPatrolState", "Superclass: EnemyState",
     ["Start walking toward the knight's side", "Patrol within radius, pausing at the ends", "Switch to chase when the knight is seen"],
     ["EnemyMotor", "EnemyPerception", "EnemyChaseState"]),
    ("EnemyChaseState", "Superclass: EnemyState",
     ["Move toward the knight at chase speed", "Stop at ledges", "Attack when in range and off cooldown"],
     ["EnemyMotor", "EnemyPerception", "EnemyAttackState"]),
    ("EnemyAttackState", "Superclass: EnemyState",
     ["Telegraph, strike, recover", "Swipe / Slam in place or Charge forward (Runner)", "Open the attack hitbox for the strike"],
     ["EnemyMotor", "Hitbox", "EnemyData", "EnemyChaseState"]),
    ("EnemyStunnedState", "Superclass: EnemyState",
     ["Flinch briefly after a hit or a blocked hit", "Turn to face an attacker behind it"],
     ["EnemyMotor", "EnemyPerception"]),
    ("EnemyDeadState", "Superclass: EnemyState",
     ["Disable contact damage and hitbox", "Fade out, then despawn"], ["EnemyAI", "Hitbox"]),
    ("EnemyFactory", "Factory",
     ["Create an enemy from EnemyData (only place enemies are made)", "Stream the spawn table in ahead of the knight",
      "Despawn enemies behind the front", "Spawn boss summons; create the boss", "Know live enemies"],
     ["EnemyData", "EnemyAI", "SpawnEntry", "BossController"]),
    # ---------------------------------------------------------------- boss
    ("BossController", "Component",
     ["Initialize components from BossData", "Start the fight when the knight enters the arena", "Lock and release arena walls",
      "Enter phase two below the health threshold", "Summon Shamblers", "Raise boss intro, health and defeated events"],
     ["BossData", "Health", "EnemyMotor", "EnemyPerception", "Hitbox", "StateMachine", "BossState", "EnemyFactory", "ArenaWall", "GameEvents"]),
    ("BossState", "Abstract. Implements: IState. Subclasses: Intro, PhaseOne, PhaseTwo, Defeated",
     ["Know the owning BossController", "Provide hooks and a change helper"], ["BossController", "StateMachine"]),
    ("BossIntroState", "Superclass: BossState",
     ["Freeze the boss and roar", "Go to phase one after the intro"], ["BossController"]),
    ("BossCombatState", "Abstract. Superclass: BossState (Template Method)",
     ["Run the fight loop: approach, telegraph, swing, recover", "Summon on a timer", "Charge across the arena when allowed",
      "Let subclasses supply the numbers"],
     ["EnemyMotor", "Hitbox", "BossController"]),
    ("BossPhaseOneState", "Superclass: BossCombatState",
     ["Slow telegraphed swings and regular summons"], ["BossData"]),
    ("BossPhaseTwoState", "Superclass: BossCombatState",
     ["Faster swings, shorter recovery, charge attack, fewer summons"], ["BossData"]),
    ("BossDefeatedState", "Superclass: BossState",
     ["Disable hitboxes and collisions", "Fade out, release the arena, announce defeat"], ["BossController", "GameEvents"]),
    # ---------------------------------------------------------------- level
    ("LevelController", "One per level scene",
     ["Load level data for the current index", "Spawn the knight at the start or last checkpoint", "Set camera bounds and backdrop",
      "Place and configure the disaster front", "Stream enemies; create the boss on level 8",
      "Report completion when the exit is reached", "Pause the front for the boss; clear summons on defeat"],
     ["GameManager", "LevelCatalog", "LevelPrefabs", "LevelBuilder", "EnemyFactory", "DisasterFront", "CameraFollow", "ParallaxBackground", "EndingController", "AmbientDebris"]),
    ("LevelBuilder", "Component",
     ["Turn the text layout into tilemaps (with tile variants) and colliders", "Place props, hazards, checkpoints, exit, signs",
      "Build the spawn table", "Place backdrop scenery on flat ground", "Set level walls, kill zone and bounds"],
     ["LevelLayout", "LevelData", "LevelPrefabs", "BuiltLevel"]),
    ("LevelLayout", "Plain C# class",
     ["Parse the text grid (one character per tile)", "Know solid and one-way cells and markers", "Validate a level's rules"], []),
    ("LevelReachability", "Plain C# class",
     ["Prove a level is finishable with the knight's real jump", "Account for platforms and moving platforms"],
     ["LevelLayout", "PlayerStats"]),
    ("BuiltLevel", "Plain C# class",
     ["Know what the builder produced (start, checkpoints, exit, spawns, boss, princess, arena, bounds)"], []),
    ("DisasterFront", "Component",
     ["Advance each physics step", "Kill the knight when caught behind it", "Publish distance to the knight",
      "Animate the wall; rumble, shake and bolts when close", "Pause, resume, stop at the boss arena"],
     ["FrontModel", "LevelData", "PlayerController", "Health", "GameEvents"]),
    ("FrontModel", "Plain C# class",
     ["Know position, elapsed time, paused flag", "Ramp speed over time with a cap", "Add capped catch-up when far behind",
      "Never move backwards; respect a stop line"], []),
    ("Checkpoint", "Component",
     ["Know its index and spawn point", "Activate on first contact; raise event", "Show activated flag"],
     ["PlayerController", "GameEvents"]),
    ("ExitGate", "Component",
     ["Open as the knight approaches", "Signal 'reached' once"], ["PlayerController", "GameEvents"]),
    ("KillZone", "Component",
     ["Apply lethal fall damage to anything entering"], ["IDamageable", "DamageInfo"]),
    ("HeartPickup", "Component",
     ["Heal one heart only if the knight is hurt", "Raise pickup event and disappear"], ["Health", "GameEvents"]),
    ("MovingPlatform", "Component",
     ["Travel back and forth with pauses", "Know its velocity so the knight rides it", "Size itself to its width"], []),
    ("ArenaWall", "Component",
     ["Rise to lock the arena; sink to release it"], []),
    ("TutorialSign", "Component",
     ["Show a hint once when the knight passes"], ["GameEvents"]),
    ("EndingController", "Castle level only",
     ["On boss defeat, take control of the knight", "Open the cage; walk the knight to the princess",
      "Play the reunion, then tell GameManager the game is finished"],
     ["GameEvents", "PlayerController", "GameManager"]),
    ("ParallaxBackground", "Component",
     ["Show sky, far and near layers scaled to height", "Scroll each layer slower than the camera"], ["LevelData"]),
    ("Hazard", "Abstract (Template Method). Subclasses: FirePatch, FallingDebris, Spikes",
     ["Own a hitbox that hurts the knight and enemies", "Run the fixed cycle: telegraph, activate, stay active, deactivate, rest"],
     ["Hitbox", "DamageInfo"]),
    ("FirePatch", "Superclass: Hazard",
     ["Glow as a warning, burn, go out on a timer", "Re-damage occupants at intervals (lure Brutes)"], ["Hitbox"]),
    ("FallingDebris", "Superclass: Hazard",
     ["Show a shadow, then drop a rock", "Hit once on impact, then break", "Tell a pool when done"], ["Hitbox", "PlayerController"]),
    ("Spikes", "Superclass: Hazard",
     ["Stay active; re-hit at intervals"], ["Hitbox"]),
    ("AmbientDebris", "Component",
     ["Drop pooled rocks ahead of the knight at intervals"], ["ObjectPool<T>", "FallingDebris", "LevelLayout"]),
    # ---------------------------------------------------------------- camera, audio, visuals
    ("CameraFollow", "Component",
     ["Follow the knight with smoothing and look-ahead", "Clamp to level bounds", "Shake on request"],
     ["PlayerController", "GameEvents"]),
    ("AudioManager", "Singleton",
     ["Play music with cross-fades and sound effects from a voice pool", "Turn game events into sounds",
      "Pick music by game state and level", "Apply saved volumes"],
     ["AudioLibrary", "GameEvents", "GameManager"]),
    ("SpriteAnimator", "Component",
     ["Play named clips frame by frame", "Show a clip at a given progress (sword swing)"], ["CharacterArt"]),
    ("PlayerAnimator / EnemyAnimator / BossAnimator", "Components",
     ["Choose the clip for the current state", "Add squash, tilt, telegraph glow, hit flash and blink"],
     ["SpriteAnimator", "PlayerController", "EnemyAI", "BossController"]),
    ("EffectPlayer", "Component",
     ["Play dust, sparks, slashes and debris on request", "Reuse effects through one pool per type"],
     ["ObjectPool<T>", "PooledParticle", "GameEvents"]),
    # ---------------------------------------------------------------- UI
    ("UIFlowController", "Component",
     ["Listen for game state changes", "Show the screens for each state", "Handle Back / Cancel and sub-menus",
      "Point the UI input module at the UI action map"],
     ["GameEvents", "InputReader", "UIPanel"]),
    ("UIPanel", "Abstract. Subclasses: MainMenu, LevelSelect, Settings, HUD, Pause, GameOver, LevelComplete, Credits",
     ["Build its widgets once from code", "Show / hide and select the first button"], ["UIFactory", "UIStyle", "UIFlowController"]),
    ("UIFactory / UIStyle", "Static helpers + ScriptableObject",
     ["Create styled text, buttons, sliders and panels", "Know fonts, sprites and colours"], []),
    ("HUDController", "Superclass: UIPanel",
     ["Show level name and timer", "Show the front distance meter", "Show hints and the boss health bar"],
     ["GameEvents", "HeartDisplay", "GameManager"]),
    ("HeartDisplay", "Component",
     ["Render max and filled hearts", "Pop hearts that are lost or gained"], ["GameEvents"]),
    ("GameOverPanel", "Superclass: UIPanel",
     ["Show the cause of death", "Offer Retry, Restart, Main menu"], ["GameManager"]),
    ("LevelCompletePanel", "Superclass: UIPanel",
     ["Show time, best time, hearts and kills", "Offer Next, Replay, Main menu"], ["GameManager", "LevelResult"]),
    ("Main / LevelSelect / Settings / Pause / Credits", "Superclass: UIPanel",
     ["Start or continue the game; pick unlocked levels", "Set volumes, fullscreen, reset progress",
      "Resume, retry, restart; show the ending credits"],
     ["GameManager", "SaveSystem", "AudioManager"]),
    ("ScreenFader", "Component",
     ["Fade to and from black between scenes (unscaled time)"], []),
]


# ====================================================================== PDF (ReportLab, like the original)
def build_pdf(path):
    from reportlab.lib import colors
    from reportlab.lib.pagesizes import A4
    from reportlab.lib.styles import ParagraphStyle
    from reportlab.lib.units import mm
    from reportlab.platypus import BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer, Table, TableStyle, FrameBreak, KeepTogether

    W, H = A4
    margin = 15 * mm
    gap = 8 * mm
    col_w = (W - 2 * margin - gap) / 2
    header_h = 32 * mm

    name_style = ParagraphStyle("name", fontName="Helvetica-Bold", fontSize=9.5, leading=11.5, alignment=1)
    sub_style = ParagraphStyle("sub", fontName="Helvetica-Oblique", fontSize=6.5, leading=8, alignment=1)
    head_style = ParagraphStyle("head", fontName="Helvetica-Bold", fontSize=7, leading=8.5)
    cell_style = ParagraphStyle("cell", fontName="Helvetica", fontSize=6.8, leading=8.4)

    def card(name, sub, resp, coll):
        r = "<br/>".join("• " + x for x in resp)
        c = "<br/>".join(coll) if coll else "–"
        data = [
            [[Paragraph(name, name_style), Paragraph(sub, sub_style)], ""],
            [Paragraph("Responsibilities", head_style), Paragraph("Collaborators", head_style)],
            [Paragraph(r, cell_style), Paragraph(c, cell_style)],
        ]
        t = Table(data, colWidths=[col_w * 0.64, col_w * 0.36])
        t.setStyle(TableStyle([
            ("SPAN", (0, 0), (1, 0)),
            ("BACKGROUND", (0, 0), (1, 0), colors.HexColor("#D9D9D9")),
            ("BOX", (0, 0), (-1, -1), 0.8, colors.black),
            ("LINEBELOW", (0, 0), (1, 0), 0.6, colors.black),
            ("LINEBELOW", (0, 1), (1, 1), 0.4, colors.grey),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("TOPPADDING", (0, 0), (-1, -1), 2),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 2.5),
            ("LEFTPADDING", (0, 0), (-1, -1), 3),
            ("RIGHTPADDING", (0, 0), (-1, -1), 3),
        ]))
        return KeepTogether([t, Spacer(1, 4 * mm)])

    def first_page(c, doc):
        c.saveState()
        c.setFont("Helvetica-Bold", 18)
        c.drawCentredString(W / 2, H - 20 * mm, TITLE)
        c.setFont("Helvetica", 9.5)
        c.drawCentredString(W / 2, H - 26 * mm, SUBTITLE)
        c.setFont("Helvetica", 8.5)
        for i, m in enumerate(MEMBERS):
            x = margin + (W - 2 * margin) * (i + 0.5) / 3
            c.drawCentredString(x, H - 32 * mm, m)
        c.line(margin, H - 35 * mm, W - margin, H - 35 * mm)
        footer(c, doc)
        c.restoreState()

    def footer(c, doc):
        c.setFont("Helvetica", 7.5)
        c.drawCentredString(W / 2, 10 * mm, f"{FOOTER}  |  {doc.page}")

    doc = BaseDocTemplate(path, pagesize=A4, title=f"{TITLE} CRC Cards", author=", ".join(MEMBERS))
    top_first = H - margin - header_h + 10 * mm
    f1 = [Frame(margin, 16 * mm, col_w, top_first - 16 * mm, id="l1", showBoundary=0, leftPadding=0, rightPadding=0),
          Frame(margin + col_w + gap, 16 * mm, col_w, top_first - 16 * mm, id="r1", leftPadding=0, rightPadding=0)]
    f2 = [Frame(margin, 16 * mm, col_w, H - margin - 16 * mm, id="l", leftPadding=0, rightPadding=0),
          Frame(margin + col_w + gap, 16 * mm, col_w, H - margin - 16 * mm, id="r", leftPadding=0, rightPadding=0)]
    doc.addPageTemplates([
        PageTemplate(id="first", frames=f1, onPage=first_page, autoNextPageTemplate="rest"),
        PageTemplate(id="rest", frames=f2, onPage=lambda c, d: (c.saveState(), footer(c, d), c.restoreState())),
    ])
    doc.build([card(*c) for c in CARDS])


# ====================================================================== DOCX (python-docx)
def build_docx(path):
    from docx import Document
    from docx.enum.section import WD_SECTION
    from docx.enum.table import WD_TABLE_ALIGNMENT
    from docx.enum.text import WD_ALIGN_PARAGRAPH
    from docx.oxml import OxmlElement
    from docx.oxml.ns import qn
    from docx.shared import Mm, Pt, RGBColor

    doc = Document()
    sec = doc.sections[0]
    sec.page_width, sec.page_height = Mm(210), Mm(297)
    for side in ("left_margin", "right_margin"):
        setattr(sec, side, Mm(15))
    sec.top_margin, sec.bottom_margin = Mm(15), Mm(15)
    normal = doc.styles["Normal"]
    normal.font.name = "Arial"
    normal.element.rPr.rFonts.set(qn("w:eastAsia"), "Arial")
    normal.font.size = Pt(7)
    normal.paragraph_format.space_after = Pt(0)

    def para(text, size, bold=False, italic=False, align=WD_ALIGN_PARAGRAPH.CENTER, after=0):
        p = doc.add_paragraph()
        p.alignment = align
        p.paragraph_format.space_after = Pt(after)
        r = p.add_run(text)
        r.font.size, r.bold, r.italic = Pt(size), bold, italic
        return p

    para(TITLE, 18, bold=True)
    para(SUBTITLE, 9.5, after=4)
    members = doc.add_table(rows=1, cols=3)
    members.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, m in enumerate(MEMBERS):
        cell = members.rows[0].cells[i]
        cell.paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
        cell.paragraphs[0].add_run(m).font.size = Pt(8.5)
    rule = para("", 2, after=6)
    border(rule, qn)

    # two-column section for the cards
    cards = doc.add_section(WD_SECTION.CONTINUOUS)
    cols = cards._sectPr.xpath("./w:cols")
    c = cols[0] if cols else OxmlElement("w:cols")
    c.set(qn("w:num"), "2")
    c.set(qn("w:space"), "454")  # 8 mm in twips
    if not cols:
        cards._sectPr.append(c)

    footer_p = sec.footer.paragraphs[0]
    footer_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    footer_p.add_run(f"{FOOTER}  |  ").font.size = Pt(7.5)
    page_field(footer_p, qn)

    for name, sub, resp, coll in CARDS:
        t = doc.add_table(rows=3, cols=2)
        t.style = "Table Grid"
        t.autofit = False
        widths = (Mm(55), Mm(31))
        fixed_width(t, widths, qn)
        head = t.cell(0, 0).merge(t.cell(0, 1))
        shade(head, "D9D9D9", qn)
        p = head.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(name)
        r.bold, r.font.size = True, Pt(9.5)
        p2 = head.add_paragraph()
        p2.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r2 = p2.add_run(sub)
        r2.italic, r2.font.size = True, Pt(6.5)
        for i, label in enumerate(("Responsibilities", "Collaborators")):
            rr = t.cell(1, i).paragraphs[0].add_run(label)
            rr.bold, rr.font.size = True, Pt(7)
        rc = t.cell(2, 0)
        rc.paragraphs[0].add_run("• " + resp[0]).font.size = Pt(6.8)
        for line in resp[1:]:
            rc.add_paragraph().add_run("• " + line).font.size = Pt(6.8)
        cc = t.cell(2, 1)
        items = coll or ["–"]
        cc.paragraphs[0].add_run(items[0]).font.size = Pt(6.8)
        for line in items[1:]:
            cc.add_paragraph().add_run(line).font.size = Pt(6.8)
        keep_together(t, qn)
        gap = doc.add_paragraph()
        gap.paragraph_format.space_after = Pt(6)
        gap.runs  # spacing paragraph between cards

    doc.core_properties.title = f"{TITLE} CRC Cards"
    doc.core_properties.author = ", ".join(MEMBERS)
    doc.save(path)


def fixed_width(table, widths, qn):
    """Fixed table layout with explicit column widths, so Word and LibreOffice agree on the card size."""
    from docx.oxml import OxmlElement
    tbl_pr = table._tbl.tblPr
    layout = OxmlElement("w:tblLayout")
    layout.set(qn("w:type"), "fixed")
    tbl_pr.append(layout)
    tbl_w = tbl_pr.find(qn("w:tblW"))
    if tbl_w is None:
        tbl_w = OxmlElement("w:tblW")
        tbl_pr.append(tbl_w)
    tbl_w.set(qn("w:type"), "dxa")
    tbl_w.set(qn("w:w"), str(int(sum(w.twips for w in widths))))
    grid = table._tbl.tblGrid
    for i, col in enumerate(grid.findall(qn("w:gridCol"))):
        col.set(qn("w:w"), str(int(widths[i].twips)))
    for row in table.rows:
        for i, cell in enumerate(row.cells):
            cell.width = widths[i]


def shade(cell, hex_color, qn):
    from docx.oxml import OxmlElement
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), hex_color)
    tc_pr.append(shd)


def border(paragraph, qn):
    from docx.oxml import OxmlElement
    p_pr = paragraph._p.get_or_add_pPr()
    bdr = OxmlElement("w:pBdr")
    bottom = OxmlElement("w:bottom")
    for k, v in (("w:val", "single"), ("w:sz", "6"), ("w:space", "1"), ("w:color", "000000")):
        bottom.set(qn(k), v)
    bdr.append(bottom)
    p_pr.append(bdr)


def page_field(paragraph, qn):
    from docx.oxml import OxmlElement
    run = paragraph.add_run()
    for kind, text in (("begin", None), (None, "PAGE"), ("end", None)):
        if kind:
            el = OxmlElement("w:fldChar")
            el.set(qn("w:fldCharType"), kind)
        else:
            el = OxmlElement("w:instrText")
            el.set(qn("xml:space"), "preserve")
            el.text = text
        run._r.append(el)


def keep_together(table, qn):
    """Asks Word to keep each card on one column/page: rows can't split and paragraphs keep with the next."""
    from docx.oxml import OxmlElement
    for row in table.rows:
        tr_pr = row._tr.get_or_add_trPr()
        cant = OxmlElement("w:cantSplit")
        tr_pr.append(cant)
        for cell in row.cells:
            for p in cell.paragraphs:
                p.paragraph_format.keep_with_next = True


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    build_pdf(os.path.join(out, NAME + ".pdf"))
    build_docx(os.path.join(out, NAME + ".docx"))
    print(f"{len(CARDS)} cards written to {os.path.abspath(out)}")
