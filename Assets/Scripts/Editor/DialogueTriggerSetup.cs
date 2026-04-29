#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class DialogueTriggerSetup : EditorWindow
{
    [MenuItem("Tools/Generer Triggers de Dialogue")]
    public static void CreateTriggers()
    {
        // 1. Trigger Couloir Feu
        CreateTrigger("DialogueTrigger_CouloirFeu", new DialogueLine[]
        {
            new DialogueLine { subtitle = "Merveilleux, le bâtiment brûle... \nJ'ai oublié pourquoi je déteste ce job.", clip = Resources.Load<AudioClip>("le_batiments_brule") },
            new DialogueLine { subtitle = "Tant pis pour le boss, je vais pas cramer pour ça !", clip = Resources.Load<AudioClip>("tant_pis, je crame pas") }
        }, true);

        // 2. Trigger Sortie Salle Elec
        CreateTrigger("DialogueTrigger_SortieElec", new DialogueLine[]
        {
            new DialogueLine { subtitle = "Mon Dieu, le feu est super proche !", clip = Resources.Load<AudioClip>("le_feu_est_proche") }
        }, false);

        // 3. Trigger Grande Salle
        CreateTrigger("DialogueTrigger_GrandeSalle", new DialogueLine[]
        {
            new DialogueLine { subtitle = "Peut-être puis-je utiliser les ascenseurs ? Non, ils sont condamnés. Le boss planque les clés de son hélico dans son bureau. Je ne sais pas le conduire, mais c'est ma seule chance.", clip = Resources.Load<AudioClip>("helicot") }
        }, false);

        // 4. Trigger Bureau Boss
        CreateTrigger("DialogueTrigger_BureauBoss", new DialogueLine[]
        {
            new DialogueLine { subtitle = "Mais quel bordel ici ! Je dois trouver les clés dans ce foutoir.", clip = Resources.Load<AudioClip>("trouve_les_clefs") }
        }, false);

        // 5. Cle Helicoptère (sur l'item directement, pas une zone)
        GameObject keyItem = GameObject.Find("HelicopterKey"); // On essaye de la trouver, sinon on crée un objet vide
        if (keyItem == null) keyItem = GameObject.Find("ClefHelico");
        if (keyItem == null) keyItem = GameObject.Find("Key");

        if (keyItem != null)
        {
            ItemPickupDialogue pickup = keyItem.GetComponent<ItemPickupDialogue>();
            if (pickup == null) pickup = keyItem.AddComponent<ItemPickupDialogue>();
            pickup.clip     = Resources.Load<AudioClip>("je_dois_partir");
            pickup.subtitle = "Allez, je dois partir tout de suite !";
            pickup.delay    = 0f;
            Debug.Log("[Dialogue] Configuré la clé d'hélicoptère automatique !");
        }
        else
        {
            Debug.LogWarning("[Dialogue] Je n'ai pas trouvé l'objet de la clé dans la scène. Tu devras ajouter le script ItemPickupDialogue manuellement sur la clé.");
        }

        Debug.Log("Tous les triggers ont été générés au centre de la scène ! Il te suffit de les déplacer aux bons endroits.");
    }

    private static void CreateTrigger(string name, DialogueLine[] lines, bool requirePowerCut)
    {
        if (GameObject.Find(name) != null)
        {
            Debug.Log($"[Dialogue] {name} existe déjà, je l'ignore.");
            return;
        }

        GameObject obj = new GameObject(name);
        
        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 3f, 3f); // Taille assez grande pour une porte/couloir

        AreaDialogueTrigger trigger = obj.AddComponent<AreaDialogueTrigger>();
        trigger.lines = lines;
        trigger.requirePowerCut = requirePowerCut;
        trigger.triggerOnce = true;

        Selection.activeGameObject = obj;
    }
}
#endif
