# Guide de prefab-isation des systèmes du jeu

## Pourquoi des prefabs

Tu utilises déjà ce pattern pour `audio_manager`, `gestion_journal`, `gestion_tutoriel`, etc.
On reste sur ce pattern : chaque scène a une instance des prefabs nécessaires. Modifier le prefab → toutes les scènes sont synchronisées automatiquement.

## Audit du projet (fait le 2026-05-23)

### Prefabs existants ✅
- `Assets/prefabs/audio_manager.prefab`
- `Assets/prefabs/Journal/gestion_journal.prefab`
- `Assets/prefabs/Tuto/gestion_tutoriel.prefab`
- `Assets/prefabs/Tuto/detecteur_tuto.prefab`
- `Assets/prefabs/Tuto/bouteille_tuto.prefab`
- `Assets/prefabs/scene_switch_tester.prefab`
- `Assets/prefabs/joueur/joueur.prefab`
- `Assets/prefabs/perso.prefab`

### À prefab-iser (managers dupliqués entre scènes)

| Manager | Refs scene-specific | Phase | Status code |
|---|---|---|---|
| `bandeau_info` | Aucune (UI interne) | **Phase 1** 🔴 | ✅ Prêt |
| `gestionnaire_bandeaux_infos` | Aucune | **Phase 1** 🔴 | ✅ Prêt |
| `gestionPointeur` | Aucune (sprites + UI) | **Phase 3a** 🟡 | ✅ Prêt |
| `gestionSousTitre` | Aucune (UI interne) | **Phase 3a** 🟡 | ✅ Prêt |
| `gestionBanniere` | Volume Global | **Phase 3b** 🟡 | ✅ Refactored (auto-find Volume) |
| `gestionFlou` | Volume Global | **Phase 3b** 🟡 | ✅ Refactored (auto-find Volume) |
| `gestionInteractionClic` | Camera + Pointeur | **Phase 3c** 🟢 | ✅ Refactored (auto-find Camera et Pointeur) |

---

## Phase 1 — Bandeau info (urgence : résout le bug "pas de bandeau dans recherche_indices")

### Étape 1.1 — Créer prefab `bandeau_info`

1. Ouvre `Assets/scenes/scene0_tuto.unity`
2. Sélectionne `bandeau_info` (sous canvas_hud)
3. Crée dossier `Assets/prefabs/ui/` si pas existant
4. Glisse `bandeau_info` depuis Hierarchy vers `Assets/prefabs/ui/`
5. Choisis **Original Prefab**

### Étape 1.2 — Créer prefab `gestionnaire_bandeaux_infos`

1. Toujours dans `scene0_tuto`, sélectionne `gestionnaire_bandeaux_infos`
2. Glisse vers `Assets/prefabs/ui/`
3. Choisis **Original Prefab**

### Étape 1.3 — Synchroniser `scene1_taverne1`

Pour `bandeau_info` :
- Supprime l'instance existante dans la scène
- Drag `Assets/prefabs/ui/bandeau_info.prefab` dans `canvas_hud`

Pour `gestionnaire_bandeaux_infos` (MANQUANT dans cette scène) :
- Drag `Assets/prefabs/ui/gestionnaire_bandeaux_infos.prefab` dans `--DONTDESTROYONLOAD/--GESTIONNAIRES/`

---

## Phase 3a — Prefabs simples (UI internes, zéro risque)

### `gestionPointeur` → `Assets/prefabs/ui/pointeur.prefab`

1. Ouvre `scene0_tuto`
2. Trouve le GameObject portant `gestionPointeur` (probablement dans canvas_hud)
3. Vérifie ses refs : `imagePointeur`, `rectPointeur` (enfants), `spriteDefaut/Interactif/PNJ/Mecanique` (assets)
4. Glisse vers `Assets/prefabs/ui/`
5. Dans `scene1_taverne1` : supprime la copie, drag le prefab

### `gestionSousTitre` → `Assets/prefabs/ui/sous_titres.prefab`

1. Trouve le widget des sous-titres (probablement `sous_titres` ou `conteneur_sous_titre`)
2. Vérifie refs : `texteInterlocuteur`, `textePropos`, `conteneurSousTitre`, `panelAjustable`, `fxBrouillard` — tous enfants ✅
3. Glisse vers `Assets/prefabs/ui/`
4. Synchronise dans `scene1_taverne1`

---

## Phase 3b — Refactor + prefab (refs scene-specific automatiquement résolues)

J'ai modifié `gestionFlou` et `gestionBanniere` pour qu'ils trouvent le **Volume Global** automatiquement au runtime via `FindFirstObjectByType<Volume>()` si la référence Inspector est vide ou cassée.

→ Tu peux maintenant les prefab-iser sans souci pour la ref Volume.

⚠️ **DÉPENDANCE EXTERNE À GÉRER** : `gestionBanniere` et `gestionFlou` sont référencés via `[SerializeField]` depuis :
- `gestionChapitres.gestionBanniere`
- `gestionsTransitions.gestionFlou`
- `gestionInputsJeu.gestionFlou`

Quand tu remplaces l'instance par un prefab dans une scène, la référence existante peut se casser. **Tu devras réassigner manuellement** dans l'Inspector des managers cités après remplacement. C'est une seule manipulation par scène.

### `gestionBanniere` → `Assets/prefabs/ui/banniere_chapitre.prefab`

1. Trouve le widget bannière (probablement `banniere_titre` ou similaire) dans `scene0_tuto`
2. **Optionnel** : tu peux laisser `volumeGlobal` assigné — le script utilisera l'auto-find seulement si null
3. Glisse vers `Assets/prefabs/ui/`
4. Synchronise

### ⚠️ `gestionFlou` — NE PAS prefab-iser

Après audit, `gestionFlou` est attaché au GameObject **`gestion_ui`** (racine), qui contient probablement d'autres composants/scripts. Prefab-iser ce GameObject entier inclurait des éléments non liés au flou. Créer un GameObject séparé juste pour le flou casserait les références existantes dans gestionsTransitions et gestionInputsJeu.

→ **Garder une copie par scène**. Le refactor auto-find Volume reste utile pour la résilience (Volume trouvé automatiquement si la ref Inspector est cassée).

---

## Phase 3c — `gestionInteractionClic`

### ⚠️ `gestionInteractionClic` — NE PAS prefab-iser

Après audit, ce script est attaché à **`camera_capture`** (sous `--CAMERAS`), qui est la caméra principale. Prefab-iser une caméra n'a pas de sens (position, FOV, Cinemachine sont scene-specific).

→ **Garder une copie par scène**. Le refactor auto-find (Camera.main + Pointeur) reste utile pour la résilience.

---

## Maintenance future (après prefab-isation)

Quand tu modifies un manager (style, animation, paramètres…) :

1. Double-clique sur le prefab dans Project (mode Prefab édition) OU modifie l'instance dans une scène
2. Si modifié dans l'instance : clic droit sur GameObject → Apply → **Apply All**
3. Toutes les scènes héritent automatiquement ✨

## Ajouter un nouveau bandeau info pour un futur chapitre

1. Crée un nouveau `bandeauInfos_*.asset` (Right-click → Create → Letallurgie → Donnees bandeau info)
2. Configure `idActionDeclenchement`, `texte`, `dureeAffichage`
3. Ouvre le **prefab** `gestionnaire_bandeaux_infos.prefab`
4. Ajoute le ScriptableObject dans le tableau "Bandeaux"
5. Apply — toutes les scènes en bénéficient

## ⚠️ Pièges à éviter

- **Ne pas mettre `DontDestroyOnLoad` dans un script qui devient prefab par scène** — créerait des doublons persistants
- **Ne pas référencer un GameObject de la scène depuis le prefab** — la référence se cassera dans les autres scènes (utilise FindFirstObjectByType si nécessaire)
- **Garder les ScriptableObjects (`bandeauInfos_*.asset`, `donneesChapitre_*.asset`) en assets** — ils sont référencés par fileID dans le prefab, ça marche partout

## Managers que je RECOMMANDE PAS de prefab-iser

| Manager | Pourquoi pas |
|---|---|
| `gestionChapitres` | Référence `chapitres[]` (ScriptableObjects assets), Banniere, Tutoriel, VideoPlayer — viable mais demande coordination |
| `gestionInputsJeu` | Référence ~20 canvas/menus scene-specific — refactor massif requis |
| `gestionsTransitions` | Référence caméras Cinemachine scene-specific |
| Les `gestionOptions*` | Liés au canvas_options (whole panel system) — prefab-iser le canvas entier serait plus propre |
