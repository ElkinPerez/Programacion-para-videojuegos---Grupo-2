using UnityEngine;

/// <summary>
/// Plays the predator's bite animation when GameEvents.OnPredatorBite is raised.
/// The bite number is sent to the Animator so the first and the second bite
/// (the one that kills) can use different animations, or the same one.
/// It only reads events: it never moves or changes the predator.
///
/// Animator setup (on the predator's Animator):
///   Parameters: Bite (Trigger), BiteNumber (Int)
///   Any State -> Bite1 : Bite + BiteNumber == 1   (Has Exit Time OFF)
///   Any State -> Bite2 : Bite + BiteNumber == 2   (Has Exit Time OFF)
///   Bite1 / Bite2 -> default state : no condition (Has Exit Time ON)
///   If you only have one bite clip, use a single Any State -> Bite with only the Bite trigger.
/// </summary>
public class PredatorAnimationController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Animator of the predator. Empty = searched on this object and its children.")]
    [SerializeField] private Animator predatorAnimator;

    [Header("Animator")]
    [Tooltip("Name of the default (chasing/swimming) state the predator returns to on a new run.")]
    [SerializeField] private string defaultStateName = "Swim";

    // Animator parameter hashes (the text must match the Animator exactly, including capitals)
    private static readonly int AnimTriggerBite = Animator.StringToHash("Bite");
    private static readonly int AnimBiteNumber = Animator.StringToHash("BiteNumber");

    private int defaultStateHash;

    private void Awake()
    {
        if (predatorAnimator == null) predatorAnimator = GetComponentInChildren<Animator>();
        if (predatorAnimator == null)
            Debug.LogWarning("PredatorAnimationController: no Animator found.", this);

        defaultStateHash = Animator.StringToHash(defaultStateName);
    }

    private void OnEnable()
    {
        GameEvents.OnPredatorBite += HandlePredatorBite;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnPredatorBite -= HandlePredatorBite;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandlePredatorBite(int biteNumber)
    {
        if (predatorAnimator == null) return;

        // The number goes first so the transition can read it when the trigger fires.
        predatorAnimator.SetInteger(AnimBiteNumber, biteNumber);
        predatorAnimator.SetTrigger(AnimTriggerBite);
    }

    // A new run starts at Countdown: clear any pending bite and go back to the normal state.
    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Countdown || predatorAnimator == null) return;

        predatorAnimator.ResetTrigger(AnimTriggerBite);
        predatorAnimator.SetInteger(AnimBiteNumber, 0);
        predatorAnimator.Play(defaultStateHash, 0, 0f);
    }
}