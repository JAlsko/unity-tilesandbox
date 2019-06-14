using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : Singleton<EntityManager> 
{
    public string entitySettingsFilePath = "Misc_Data/EntityAttributes/";
    public string defaultEntitySettingsFileName = "default";
    private List<FloatAttribute> defaultEntityAttrList = new List<FloatAttribute>();
    private Dictionary<string, EntityAttributes> allEntityOverrides = new Dictionary<string, EntityAttributes>();

    [Header("Attribute Names")]
	public string healthAttributeName = "health";
	public string armorAttributeName = "armor";
	public string moveSpeedAttributeName = "moveSpeed";
	public string jumpHeightAttributeName = "jumpHeight";
	public string healthRegenAttributeName = "healthRegen";

    public void InitializeEntityAttributes() {
        EntityAttributes defaultAttrs = Resources.Load(entitySettingsFilePath + defaultEntitySettingsFileName) as EntityAttributes;
        for (int i = 0; i < defaultAttrs.entityAttributes.Count; i++) {
            defaultEntityAttrList.Add(new FloatAttribute(defaultAttrs.entityAttributes[i]));
        }

        Resources.LoadAll(entitySettingsFilePath);
        EntityAttributes[] foundEntityAttrs = (EntityAttributes[]) Resources.FindObjectsOfTypeAll(typeof(EntityAttributes));
        foreach (EntityAttributes i in foundEntityAttrs) {
            allEntityOverrides[i.entityName] = i;
            Debug.Log("Loaded entity settings for " + i.entityName);
        }
    }

    public void InitializeExistingEntities() {
        Entity[] allEntities = FindObjectsOfType<Entity>();

        for (int i = 0; i < allEntities.Length; i++) {
            allEntities[i].InitializeEntity();
        }
    }

    public Dictionary<string, FloatAttribute> GetDefaultEntityAttrs() {
        Dictionary<string, FloatAttribute> defaultEntityAttrDict = new Dictionary<string, FloatAttribute>();

        for (int i = 0; i < defaultEntityAttrList.Count; i++) {
            FloatAttribute f_attr = defaultEntityAttrList[i];
            defaultEntityAttrDict[f_attr.attributeName] = new FloatAttribute(f_attr);
        }

        return defaultEntityAttrDict;
    }

    public List<FloatAttribute> GetEntityAttrs(string entityType) {
        EntityAttributes entityAttrs = allEntityOverrides[entityType];
        if (entityAttrs == null) {
            Debug.LogError("Can't find entity settings for entity: " + entityType + "! Make sure the file name matches the entity name!");
            return null;
        }

        return entityAttrs.entityAttributes;
    }
}
