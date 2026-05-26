# Guide d'implémentation — Énigme tuyauterie scene2_usine

## État du projet (déjà fait par Claude)

- ✅ 7 scripts énigme tuyaux présents (`pointAncrageTuyau`, `gestionEnigmeTuyauterie`, `controleurPlacementTuyau`, `orientationTuyau`, `iconeFlottanteCurseur`, `debugAjoutePiece`, `donneesEnigme`)
- ✅ 9 prefabs `place_holders/` récupérés depuis branche `olivier_vernet_enigme_tuyauterie`
- ✅ 10 scriptable objects `tuyau_*.asset` dans `Assets/asset_objet_scriptable/usine/`
- ✅ `prefabModele3D` rempli sur 9/10 (manque seulement `tuyau_court.asset`)
- ✅ `categorie: Tuyaux` (1) sur les 10 scriptable objects — **fix appliqué** : avant c'était `0` (Tuto), ce qui empêchait le contrôleur d'accepter les tuyaux
- ✅ 3 matériaux ghost (`mat_ombre_tuyau_neutre/correct/incorrect`)
- ✅ Prefab `snap_point.prefab` pré-configuré avec les 3 matériaux ghost, BoxCollider 0.5³, `rayonDetection: 1`, `ignorerOrientation: true`

## Mécanique du jeu (rappel)

1. Le joueur ramasse les tuyaux dispersés dans la scène (clic → ajout inventaire, comme la bouteille scene0)
2. Le joueur ouvre l'inventaire (touche `i`)
3. Le joueur sélectionne un tuyau dans l'inventaire
4. Curseur sur un snap_point → un "ghost" du tuyau s'affiche (vert si bon, rouge si mauvais)
5. Touche `R` pour rotater, touche `E` pour placer
6. Si correct : tuyau retiré de l'inventaire, snap rempli
7. Si trop d'erreurs (configurable) : reset complet de l'énigme

## Étapes à faire dans Unity Editor

### Étape 1 — Renommer les scriptable objects pour matcher les tuyaux à placer

Tu as 10 scriptable objects `tuyau_*.asset` avec des noms génériques (`tuyau_court`, `tuyau_l_diametre_grand`...). Tu m'as dit vouloir les renommer pour qu'ils matchent les **8 tuyaux à placer** dans `enigme_tuyauterie > tuyaux_a_placer`.

**Action :** dans Project window, sélectionne chaque `.asset`, F2 pour renommer. Proposition de mapping (tu peux ajuster selon le visuel) :

| Tuyau dans scene2 | Scriptable object à renommer en |
|---|---|
| `tuyau_droit_petit` | `tuyau_droit_petit` (renommer `tuyau_court` ?) |
| `tuyau_t_2_petit_et_1_gros` | `tuyau_t_2_petit_et_1_gros` (renommer `tuyau_t_diametre_petit_grand`) |
| `tuyau_petit_long_avec_intersection` | `tuyau_petit_long_avec_intersection` (renommer `tuyau_t_diametre_moyen`) |
| `tuyau_l_gros_et_petit` | `tuyau_l_gros_et_petit` (renommer `tuyau_l_diametre_moyen_grand`) |
| `tuyau_petit_long_et_bout_intersection` | `tuyau_petit_long_et_bout_intersection` (renommer `tuyau_t_diametre_moyen 1`) |
| `tuyau_l_gros` | `tuyau_l_gros` (renommer `tuyau_l_diametre_grand`) |
| `tuyau_petit_court_et_bout_intersection` | `tuyau_petit_court_et_bout_intersection` (renommer `tuyau_l_diametre_moyen_petit`) |
| `tuyau_t_2_gro_1_petit` | `tuyau_t_2_gro_1_petit` (renommer `tuyau_t_diametre_moyen_grand`) |

Le `tuyau_croix_diametres_moyen_grand` et `tuyau_grand_diametre` peuvent rester (probablement pour les `tuyau_deja_en_place` ou inutilisés).

**Important :** ne renomme PAS dans Finder/Explorer — utilise Unity Project window pour préserver le `.meta` et les références.

Pour chaque scriptable object renommé, vérifie aussi dans l'Inspector :
- `nomObjet` : remplir avec un nom user-friendly (ex : "Tuyau en L gros")
- `prefabModele3D` : doit pointer vers le modèle 3D du tuyau (le visuel quand placé)
- `image` : sprite pour l'icône inventaire
- `categorie` : déjà mis à **Tuyaux**

### Étape 2 — Créer 8 snap_points dans la scène

**Action :**

1. Dans la Hierarchy, sélectionne `enigme_tuyauterie`
2. Crée un GameObject enfant vide nommé `snap_points`
3. Glisse-dépose le prefab `Assets/prefabs/enigmes/enigme_Tuyaux/snap_point/snap_point.prefab` 8 fois sous `snap_points`
4. Renomme chaque snap_point selon le tuyau qu'il accueille : `snap_tuyau_droit_petit`, `snap_tuyau_t_2_petit_et_1_gros`, etc.
5. Pour chaque snap_point :
   - **Position** : place-le EXACTEMENT à l'endroit où le tuyau doit aller (dans le mur, là où il y a un "trou")
   - **Inspector → pointAncrageTuyau** :
     - `pieceAttendue` : glisse le scriptable object correspondant
     - `orientationAttendue` : `Zero` par défaut (laisse si `ignorerOrientation: true`)
     - `ignorerOrientation` : **coché** (true) pour commencer simple — décoche plus tard si tu veux forcer une rotation précise
     - `rayonDetection` : `1` (laisse)
     - Les 3 matériaux ghost sont déjà assignés par le prefab

### Étape 3 — Attacher le script gestionEnigmeTuyauterie

1. Sélectionne le GameObject `enigme_tuyauterie` (parent)
2. Add Component → `gestionEnigmeTuyauterie`
3. Dans l'Inspector :
   - `Points Ancrage` : ouvre la liste, ajoute 8 entrées, glisse chaque snap_point dedans
   - `Nombre Erreurs Max` : `5` (à ajuster selon la difficulté voulue)
   - `Ecran Reprise` : laisse vide pour l'instant (optionnel)
   - **Events** (`onVictoire`, `onReset`, etc.) : laisse vide, on peut les configurer plus tard

### Étape 4 — Attacher le contrôleur de placement

Le `controleurPlacementTuyau` doit être sur un GameObject toujours actif (ex : caméra ou un manager).

1. Sélectionne le GameObject caméra principal (`Main Camera` sous `--CAMERAS`)
2. Add Component → `controleurPlacementTuyau`
3. Inspector :
   - `Gestionnaire Enigme` : glisse `enigme_tuyauterie`
   - `Camera Joueur` : glisse `Main Camera`
   - `Icone Flottante` : laisse vide (optionnel)
   - `Touche Placement` : `E` (par défaut)
   - `Touche Rotation` : `R` (par défaut)
   - `Distance Raycast` : `10` (par défaut)

### Étape 5 — Disperser les tuyaux à ramasser dans la scène

Le joueur doit trouver les tuyaux pour les mettre dans son inventaire. Comme la bouteille de scene0.

Pour chaque tuyau à ramasser (8 au total) :

1. Glisse le prefab du tuyau 3D dans la scène à un endroit visible (sur une table, par terre, etc.) — utilise le prefab du dossier `Assets/prefabs/enigmes/enigme_Tuyaux/place_holders/` (ou ailleurs si tu en as d'autres)
2. Add Component → `objetRamassable`
3. Inspector :
   - `Ajouter Inventaire` : **coché**
   - `Objet Inventaire` : glisse le scriptable object correspondant
   - `Ajouter Journal` : décoché (sauf si tu veux une entrée journal)
   - `Son Ramasser` : `son_objetbois.mp3` (ou autre son métallique)
   - `Id Action A Declencher` : laisse vide (sauf si lié à un tuto)
4. Vérifie qu'il y a un **Collider** sur le tuyau (sinon le clic ne marchera pas)

### Étape 6 — Tester

1. Lance scene2 en Play
2. Trouve un tuyau dispersé → clique dessus → vérifie qu'il s'ajoute à l'inventaire (touche `i`)
3. Ouvre l'inventaire (`i`), clique sur le tuyau pour le sélectionner
4. Ferme l'inventaire
5. Approche un snap_point avec le curseur → un ghost doit s'afficher
6. Touche `E` → si bon tuyau au bon snap, ça s'instancie
7. Si mauvais tuyau ou mauvaise orientation, ça compte une erreur

## Diagnostics si ça ne marche pas

- **Pas de ghost qui s'affiche** : vérifier `categorie: Tuyaux` (1) sur le scriptable object — corrigé par Claude, mais re-vérifier
- **Tuyau pas dans l'inventaire après ramassage** : vérifier `ajouterInventaire: true` et `objetInventaire` rempli
- **Snap_point ne réagit pas au curseur** : vérifier que le BoxCollider est dans le path du raycast (taille assez grande, position correcte)
- **Touche E ne place rien** : ouvrir la console, le `controleurPlacementTuyau` log `TenterPlacer - aucun snap sous le curseur` ou `TenterPlacer - résultat: MauvaisePiece/Succes`

## Prochaine action

Après ces étapes, teste scene2 en standalone. Si quelque chose pète, ramène les logs console exacts ici.
