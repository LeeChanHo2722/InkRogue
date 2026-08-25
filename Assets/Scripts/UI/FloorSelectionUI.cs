using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloorSelectionUI : MonoBehaviour
{
    private const int CardCount = 3;

    [Serializable]
    private sealed class FloorSelectionCard
    {
        public Button button;
        public TMP_Text floorIdText;
        public TMP_Text difficultyText;
    }

    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private FloorSelectionCard[] cards =
        new FloorSelectionCard[CardCount];

    private readonly FloorCandidate[] displayedCandidates =
        new FloorCandidate[CardCount];

    private Action<FloorCandidate> selected;
    private bool initialized;

    private void Awake()
    {
        initialized = ValidateReferences();

        if (initialized)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int cardIndex = i;
                cards[i].button.onClick.AddListener(
                    () => SelectCard(cardIndex)
                );
            }
        }

        Hide();
    }

    public bool ShowCandidates(
        IReadOnlyList<FloorCandidate> candidates,
        Action<FloorCandidate> onSelected)
    {
        if (!initialized ||
            candidates == null ||
            candidates.Count < CardCount ||
            onSelected == null)
        {
            return false;
        }

        selected = onSelected;

        for (int i = 0; i < CardCount; i++)
        {
            FloorCandidate candidate = candidates[i];
            displayedCandidates[i] = candidate;

            bool available = candidate?.Floor != null;
            FloorSelectionCard card = cards[i];

            card.button.interactable = available;
            card.floorIdText.text =
                available
                    ? candidate.Floor.FloorId ?? string.Empty
                    : string.Empty;
            card.difficultyText.text =
                available
                    ? candidate.Difficulty.ToString()
                    : string.Empty;
        }

        panelRoot.SetActive(true);
        return true;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        selected = null;
        Array.Clear(
            displayedCandidates,
            0,
            displayedCandidates.Length
        );
    }

    private void SelectCard(int index)
    {
        if (index < 0 || index >= displayedCandidates.Length)
            return;

        FloorCandidate candidate = displayedCandidates[index];

        if (candidate != null)
            selected?.Invoke(candidate);
    }

    private bool ValidateReferences()
    {
        if (panelRoot == null ||
            cards == null ||
            cards.Length != CardCount)
        {
            Debug.LogError(
                "FloorSelectionUI requires a panel and exactly 3 cards.",
                this
            );
            return false;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            FloorSelectionCard card = cards[i];

            if (card == null ||
                card.button == null ||
                card.floorIdText == null ||
                card.difficultyText == null)
            {
                Debug.LogError(
                    "FloorSelectionUI card " + i + " is incomplete.",
                    this
                );
                return false;
            }
        }

        return true;
    }
}
