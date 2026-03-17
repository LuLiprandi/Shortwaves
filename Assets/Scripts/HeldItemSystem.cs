using UnityEngine;

public class HeldItemSystem : MonoBehaviour
{
    public static HeldItemSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform holdAnchor;

    private const string HELD_ITEM_LAYER = "HeldItem";

    private GameObject currentHeldItem;

    public bool HasItem => currentHeldItem != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>Picks up an item and displays its 3D model in the hold anchor.</summary>
    public void PickUp(GameObject itemModel)
    {
        if (HasItem) return;

        currentHeldItem = Instantiate(itemModel, holdAnchor);
        currentHeldItem.transform.localPosition = Vector3.zero;
        currentHeldItem.transform.localRotation = Quaternion.identity;

        SetLayerRecursive(currentHeldItem, LayerMask.NameToLayer(HELD_ITEM_LAYER));
    }

    /// <summary>Removes the currently held item from hand.</summary>
    public void Drop()
    {
        if (!HasItem) return;

        Destroy(currentHeldItem);
        currentHeldItem = null;
    }

    private void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
