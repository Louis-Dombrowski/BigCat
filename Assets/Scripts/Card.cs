using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class Card : MonoBehaviour
{
    public delegate void Callback();

    [Header("Parts")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Transform prefabSpawnLocation;
    [SerializeField] private Transform target;
    [Header("Properties")]
    [SerializeField] public bool environmental = false; // If this card is purely decorative
    [SerializeField] private GameObject prefab;
    [SerializeField] public Approximator approx; // initialized externally on prefab instantiation
    [SerializeField] public QuaternionApproximator rotApprox;
    [Header("State")]
    [SerializeField] public bool shouldFollowTarget = false;
    [SerializeField] public bool shouldUpdateCard;
    [SerializeField] private BaseCard editModeCard; // A disabled version that's used for edit mode previews
    [SerializeField] private BaseCard playModeCard; // A dormant version that gets copied into the simulation

    private void Start()
    {
        // Instantiate the target externally, so it isnt a child of this GameObject
        target = Instantiate(targetPrefab).transform;
        target.name = "TargetOf_" + name;
    }

    private void OnDestroy()
    {
        Destroy(target);
    }

    // LateUpdate, for following its target
    void LateUpdate()
    {
        if (shouldFollowTarget)
        {
            transform.position = approx.Update(Time.deltaTime, target.position);
            transform.rotation = rotApprox.Update(Time.deltaTime, target.rotation);
        }

        if (editModeCard != null)
        {
            // Make the children follow the prefab spawn location while they're instantiated under the different asset trees
            editModeCard.transform.position = prefabSpawnLocation.transform.position;
            editModeCard.transform.rotation = prefabSpawnLocation.transform.rotation;
            playModeCard.transform.position = prefabSpawnLocation.transform.position;
            playModeCard.transform.rotation = prefabSpawnLocation.transform.rotation;
        }

        if (shouldUpdateCard)
        {
            var input = InputHandler.PollCardInput();

            if (input.ModifyCard != 0)
            {
                editModeCard.ChangeProperty(input.ModifyCard);
                playModeCard.ChangeProperty(input.ModifyCard);
            }
        }
    }

    // Copies the contents of t into the card's target transform. It doesn't assign the reference t to the target.
    public void SetTarget(Transform t)
    {
        shouldFollowTarget = true;

        if (approx.position.Length != 3)
        {
            Debug.LogWarning("Tried to set target of card without a properly initialized approximator");
            approx.Initialize(transform.position);
        }
        
        target.position = t.position;
        target.rotation = t.rotation;
    }

    public void ExitHand()
    {
        editModeCard = Instantiate(prefab, prefabSpawnLocation.position, prefabSpawnLocation.rotation, GameObject.Find("EditModeAssets").transform).GetComponent<BaseCard>();
        editModeCard.DisableDistractions();
        editModeCard.DisablePlayFunctionality();
        editModeCard.SetEditModeShaders();
        editModeCard.EnableEditFunctionality();
        
        playModeCard = Instantiate(prefab, prefabSpawnLocation.position, prefabSpawnLocation.rotation, GameObject.Find("PlayModeAssets_ToCopy").transform).GetComponent<BaseCard>();
        playModeCard.gameObject.SetActive(false);

        shouldUpdateCard = true;
    }

    public void EnterHand()
    {
        if(editModeCard != null) Destroy(editModeCard.gameObject);
        if(editModeCard != null) Destroy(playModeCard.gameObject);

        shouldUpdateCard = false;
    }
}
