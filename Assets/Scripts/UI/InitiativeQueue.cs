using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the initiative queue display.
/// </summary>
public class InitiativeQueue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Settings")]
    [SerializeField] private int maxSlotsToShow = 8;

    private readonly List<InitiativeSlot> slots = new List<InitiativeSlot>();

    public void UpdateQueue(List<Gladiator> turnOrder, int currentTurnIndex)
    {
        if (turnOrder == null || turnOrder.Count == 0)
        {
            ClearQueue();
            return;
        }

        int slotsToShow = Mathf.Min(maxSlotsToShow, turnOrder.Count);

        while (slots.Count < slotsToShow)
        {
            CreateSlot();
        }

        for (int i = slotsToShow; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < slotsToShow; i++)
        {
            int turnIndex = (currentTurnIndex + i) % turnOrder.Count;
            Gladiator glad = turnOrder[turnIndex];
            bool isCurrent = i == 0;
            slots[i].Setup(glad, isCurrent);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    private void CreateSlot()
    {
        if (slotPrefab == null || slotsParent == null)
        {
            Debug.LogError("InitiativeQueue: slotPrefab or slotsParent is null!");
            return;
        }

        GameObject slotObj = Instantiate(slotPrefab, slotsParent);
        InitiativeSlot slot = slotObj.GetComponent<InitiativeSlot>();
        if (slot == null)
        {
            Debug.LogError("InitiativeQueue: Slot prefab doesn't have InitiativeSlot component!");
            Destroy(slotObj);
            return;
        }

        slots.Add(slot);
    }

    private void ClearQueue()
    {
        foreach (InitiativeSlot slot in slots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(false);
            }
        }
    }
}
