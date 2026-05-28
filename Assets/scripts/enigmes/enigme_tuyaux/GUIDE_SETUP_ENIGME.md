# Guide de setup complet — Énigme tuyauterie scene2_usine

**Objectif :** finir l'énigme avec toutes les features demandées.
**Ordre recommandé :** suivre les sections de haut en bas.

---

## ÉTAPE 1 — Persistance Canvas HUD (débloquer la collègue)

**But :** que l'inventaire fonctionne entre les scènes (scene2 → scene3).

1. Dans CHAQUE scène (scene0, scene1, scene2, scene3) :
   - Hierarchy → trouve le GameObject parent du `canvas_hud`
   - **Add Component** → `persistanceCanvasHUD`
2. Sauvegarder toutes les scènes

✅ Test : ramasser tuyaux en scene2, transitionner scene3, vérifier que l'inventaire affiche encore les tuyaux ET qu'on peut cliquer dedans.

---

## ÉTAPE 2 — Tuile explicative qui s'affiche à Entrée

**Diagnostic :** ton script `zoneLancementEnigme` cherche le GameObject `tuileExplicativeGameObject` + 2 enfants TMP_Text nommés `titre_enigme` et `explication_enigme`.

**Setup :**
1. Hierarchy scene2 → trouve le GameObject de la zone d'énigme (`zone_lancement_enigme` ou similaire)
2. Inspector → script `zoneLancementEnigme` :
   - **`Tuile Explicative GameObject`** → drag-drop le GameObject UI de ta tuile (probablement `ensemble_tuile_explicative` dans canvas_hud)
   - **`Donnees Tuile Source`** → drag-drop un `DonneesTutoriel` scriptable (le plus propre) OU :
   - **`Titre Tuile`** = "Énigme tuyauterie" (en texte direct)
   - **`Explication Tuile`** = "Place les tuyaux aux bons endroits pour rétablir le circuit." (en texte direct)
   - **`Fermable Avec Esc`** = ✓ coché

3. **Vérifier que la tuile UI a les bons enfants :**
   - GameObject `ensemble_tuile_explicative` doit avoir 2 enfants TMP_Text :
     - Un nommé `titre_enigme` (le script accepte aussi `titre_tuto` ou `titre`)
     - Un nommé `explication_enigme` (le script accepte aussi `explication_tuto` ou `explication`)
   - Si les noms sont différents → renommer dans la Hierarchy
   - Le GameObject `ensemble_tuile_explicative` doit être **désactivé par défaut** (décoché en haut de l'Inspector)

4. **Tester :** lance scene2, va à la zone énigme, appuie Entrée → la tuile s'affiche avec titre + texte.

**Tuile qui doit disparaître au 1er tuyau placé** (demande user) :
- Voir ÉTAPE 5 ci-dessous (système d'événements).

---

## ÉTAPE 3 — Ajouter glow + highlight aux tuyaux à ramasser

**But :** les tuyaux dans la scène scintillent (prefab_glow) + s'illuminent quand on les pointe (HighlightObjet).

**Pour chaque tuyau dans `tuyaux_a_placer/` :**

1. **Ajouter le composant `gestionHighlightHover`** :
   - Inspector → Add Component → `gestionHighlightHover`
   - Laisse les champs vides (le script auto-find dans les enfants)

2. **Ajouter un enfant `highlight_particules`** :
   - Click droit sur le tuyau dans Hierarchy → Create Empty Child
   - Renommer en `highlight_particules`
   - Add Component → ParticleSystem (configurer : Play On Awake OFF, Looping ON)
   - Add Component → Light (Point Light, intensity 0)

3. **Ajouter le composant `gestionGlowIndices`** sur le parent `tuyaux_a_placer` (pas sur chaque tuyau individuellement) :
   - Add Component → `gestionGlowIndices`
   - `Tags A Reveler` = `obj_int` (le tag des tuyaux)
   - `Cacher Indices Au Demarrage` = ✓ coché
   - `Id Action Declencheur` = `indices_visibles` (ou autre signal)
   - `Animer Glow` = ✓ coché

4. **Ajouter un enfant `prefab_glow`** sur chaque tuyau :
   - Click droit sur le tuyau → Drag-drop `prefab_glow.prefab` (depuis Assets/prefabs/prefab_glow.prefab)
   - Le rend enfant du tuyau

5. **Vérifie que chaque tuyau a le tag `obj_int`** (sinon gestionGlowIndices ne les trouve pas).

**Comportement attendu :**
- Tuyaux invisibles au démarrage (`cacherIndicesAuDemarrage`)
- Quand l'action `indices_visibles` est signalée → tous les tuyaux apparaissent avec glow
- Quand le joueur pointe un tuyau → highlight s'allume

---

## ÉTAPE 4 — Séquence narrative complète

**Flux demandé par l'utilisateur :**

```
Contact pointeur_personnage (tavernier captif)
    ↓ +3s
Bannière chapitre "Énigme tuyauterie"
    ↓ après bannière finie
    ↓ +3s
Pointeur_enigme apparaît
    ↓ contact pointeur_enigme
Bandeau infos : "Rechercher et ramasser les tuyaux manquants"
    ↓ +1s
Tuyaux apparaissent dans la scène
    ↓ joueur ramasse 4 tuyaux
Bandeau : "Vous pouvez commencer à placer les tuyaux" (2s)
    ↓ retour zone énigme
Bandeau : "Appuyer sur Entrée..."
    ↓ Entrée
Tuile explicative + minuteur démarre
    ↓ 1er tuyau placé
Tuile explicative disparaît
```

**Setup Unity (les scripts existent déjà) :**

| Étape | Composant à ajouter | idAction concernées |
|---|---|---|
| Contact pointeur_personnage signale l'action | déjà OK (signaleurZone existant) | `joueur_a_atteint_enigme` (ou autre) |
| +3s puis bannière chapitre | **`declencheurActionApresDelai`** (sur GameObject vide) → idActionEcoutee + idActionADeclencher + délai 3s | écoute action contact → signale `chapitre_enigme_demarre` |
| Bannière chapitre "Énigme tuyauterie" | scriptable `donneesChapitre` avec idActionDemarrage = `chapitre_enigme_demarre` | (utiliser gestionChapitres existant) |
| +3s après bannière → pointeur_enigme apparaît | sur pointeur_enigme : **`gestionActivationAction`** + idActionActivation = `chapitre_enigme_banniere_terminee` + délai 3s | signal auto émis par gestionChapitres à fin bannière |
| Contact pointeur_enigme → bandeau "Cherche tuyaux" | sur pointeur_enigme : signaleurZone → signale `joueur_a_touche_pointeur_enigme` | + scriptable `DonneesBandeauInfo` avec idActionDeclenchement = même action |
| +1s après bandeau → tuyaux apparaissent | sur parent tuyaux_a_placer : **`gestionActivationAction`** + idActionActivation = `joueur_a_touche_pointeur_enigme` + délai 1s | + signaler `indices_visibles` pour le glow |
| 4 tuyaux → bandeau "Tu peux placer" | **Nouveau script à créer : `surveillanceInventaire`** qui écoute gestionInventaire et signale `joueur_a_4_tuyaux` quand le compte atteint 4 | + DonneesBandeauInfo écoutant cette action |
| Retour zone → bandeau "Appuyer Entrée" | déjà OK (texteApproche dans zoneLancementEnigme) | géré par OnTriggerEnter |
| 1er tuyau placé → tuile disparaît | sur tuileExplicative : **`gestionDisparitionAction`** + idActionDisparition = `premier_tuyau_place` | + dans gestionEnigmeTuyauterie.SurRemplissage, signaler cette action à la 1ère fois |

**Action concrète pour toi :**
1. Crée les `DonneesBandeauInfo` scriptables nécessaires (clic droit Project → Create → ScriptableObject → DonneesBandeauInfo)
2. Configure leurs `idActionDeclenchement` selon le tableau
3. Ajoute les composants `gestionActivationAction`, `gestionDisparitionAction`, `signaleurZone` sur les bons GameObjects
4. **Pour le script `surveillanceInventaire` (nouveau)** : je le code ci-dessous

---

## ÉTAPE 5 — Minuteur + canvas reprise + sons

**Minuteur :**
- Le script `minuteurEnigmeTuyauterie` existe déjà
- Démarre sur Entrée → déjà géré par `zoneLancementEnigme.LancerEnigme()` qui signale `enigme_tuyauterie_lancee`
- Le minuteur écoute cette action et démarre

**Pause sur Esc :**
- Dans `zoneLancementEnigme.QuitterEnigme()`, déjà : `minuteurEnigmeTuyauterie.Instance.MettreEnPause()`

**Canvas reprise 5s :**
- Sur le GameObject `canvas_reprise_enigme` :
  - Inspector du script `gestionEcranReprise` → champ `dureeAffichage` = 5

**Sons réussite/échec :**
- Déjà gérés via `sonsEnigmeTuyauterie` qui écoute `onVictoire` et `onReset` du gestionEnigmeTuyauterie
- Drag-drop des AudioClips dans son Inspector

---

## ÉTAPE 6 — Fin énigme (succès)

**Pointeur porte 3s après victoire :**
- Sur GameObject `pointeur_porte_sortie` :
  - Add Component `gestionActivationAction`
  - `idActionActivation` = `enigme_tuyauterie_reussie` (signalée par onVictoire de l'énigme)
  - `delaiAvantActivation` = 3

**Transition scene3 :**
- Sur le pointeur_porte_sortie, ajouter un `signaleurZone` qui signale `joueur_va_a_scene3`
- Sur un GameObject avec `transitionAvecFondu` ou similaire :
  - Écoute `joueur_va_a_scene3`
  - Délai 1s puis charge `scene3_taverne2`

---

## SCRIPT NOUVEAU À AJOUTER : surveillanceInventaire

Ce script surveille l'inventaire et signale une action quand un nombre cible d'objets d'une catégorie est atteint.

(Je le code et tu l'ajoutes sur un GameObject vide)

---

## Checklist finale avant de tester

- [ ] `persistanceCanvasHUD` sur les 4 scènes
- [ ] `Tuile Explicative GameObject` assigné dans zoneLancementEnigme
- [ ] Tuile UI a des enfants TMP_Text `titre_enigme` + `explication_enigme`
- [ ] `Donnees Tuile Source` rempli OU `Titre Tuile`/`Explication Tuile` remplis
- [ ] Tuyaux ont tag `obj_int` + composant `gestionHighlightHover` + enfant `highlight_particules` + enfant `prefab_glow`
- [ ] Parent `tuyaux_a_placer` a `gestionGlowIndices` avec idActionDeclencheur
- [ ] Tous les `gestionActivationAction` et `gestionDisparitionAction` ont leurs idAction correctement renseignés
- [ ] Sons assignés sur `sonsEnigmeTuyauterie`
- [ ] Canvas reprise réduit à 5s
- [ ] Pointeur porte + transition scene3 configurés

Tester pas à pas, scène par scène.
