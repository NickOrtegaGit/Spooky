using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stack plates without dropping one on the table.
///
/// A plate slides across the top of the stack; space stops it and it falls the
/// short distance onto the pile. Land it lopsided and the stack tips. Any plate
/// touching the table fails the whole task — the punishment is time, not the
/// run. See Docs/Tasks.md.
///
/// The stack is real 2D physics living off in world space, filmed by its own
/// camera into a RenderTexture the panel displays. UI space has no physics, and
/// hand-rolled tipping would not feel the same.
/// </summary>
public class DishStackMinigame : PanelMinigame
{
    // Resolved from DishStackStage.Instance: this panel is a prefab, and a
    // prefab cannot reference scene objects.
    private Transform stageRoot;
    private Transform spawnPoint;
    private DishStackTable table;

    [Header("Plates")]
    [Tooltip("The plate prefab: SpriteRenderer + Rigidbody2D + Collider2D.")]
    [SerializeField] private Rigidbody2D platePrefab;

    [Tooltip("How many plates must be stacked to finish the task.")]
    [SerializeField] private int plateCount = 6;

    [Tooltip("Spawn a fixed plate on the table to stack onto. Without one the " +
             "first plate has nothing to land on but the table, which is a loss.")]
    [SerializeField] private bool spawnBasePlate = true;

    [Tooltip("Where the base plate sits. Usually the middle of the table.")]
    [SerializeField] private Vector2 basePlateOffset = Vector2.zero;

    [Tooltip("How high above the settled stack the next plate slides.")]
    [SerializeField] private float slideHeightAboveStack = 0.6f;

    [Header("Slide")]
    [Tooltip("How far the plate travels left to right, in world units.")]
    [SerializeField] private float slideWidth = 3f;

    [SerializeField] private float slideSpeed = 2.5f;

    [Tooltip("Added to slide speed per plate placed, so it tightens as you go.")]
    [SerializeField] private float slideSpeedPerPlate = 0.35f;

    [Header("Settling")]
    [Tooltip("Seconds a plate must stay still before it counts as placed.")]
    [SerializeField] private float settleSeconds = 0.5f;

    [Tooltip("Speed below which a plate counts as still.")]
    [SerializeField] private float settleSpeedThreshold = 0.05f;

    [Header("Leaving")]
    [Tooltip("Key that abandons the task. Progress is lost — interacting again " +
             "starts from an empty table.")]
    [SerializeField] private Key quitKey = Key.X;

    [Header("UI")]
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private TMP_Text messageLabel;

    [Tooltip("Seconds after the panel is up before the labels begin to appear.")]
    [SerializeField] private float labelDelaySeconds = 0.4f;

    [Tooltip("Seconds the labels take to fade in.")]
    [SerializeField] private float labelFadeSeconds = 0.5f;

    [Tooltip("Seconds the labels take to fade out when the task ends. Runs " +
             "before the panel slides away.")]
    [SerializeField] private float labelFadeOutSeconds = 0.2f;

    private readonly List<Rigidbody2D> placed = new List<Rigidbody2D>();

    private Coroutine labelFade;
    private Rigidbody2D sliding;
    private Rigidbody2D basePlate;
    private float slideDirection = 1f;
    private float slideOriginX;
    private bool acceptingInput;
    private bool finished;

    /// <summary>
    /// Hide the labels before the panel even starts sliding. OnPanelReady only
    /// runs once it is up, which would leave them visible for the whole slide.
    /// </summary>
    private void Awake()
    {
        SetLabelAlpha(0f);
    }

    protected override void OnPanelReady()
    {
        DishStackStage stage = DishStackStage.Instance;

        if (stage == null)
        {
            Debug.LogError("DishStackMinigame found no DishStackStage in the scene; " +
                           "completing so the player is not stuck in an empty panel.", this);
            EndWithSuccess();
            return;
        }

        stageRoot = stage.transform;
        spawnPoint = stage.SpawnPoint;
        table = stage.Table;

        if (spawnPoint != null) slideOriginX = spawnPoint.position.x;

        if (table != null) table.PlateHitTable += OnPlateHitTable;

        if (spawnBasePlate) SpawnBasePlate();

        UpdateProgress();
        labelFade = StartCoroutine(FadeInLabels());
        SpawnNextPlate();
    }

    /// <summary>
    /// Holds the labels back a moment, then eases them in, so the panel settles
    /// before the text arrives rather than everything appearing at once.
    /// </summary>
    private IEnumerator FadeInLabels()
    {
        SetLabelAlpha(0f);

        if (labelDelaySeconds > 0f) yield return new WaitForSeconds(labelDelaySeconds);

        float elapsed = 0f;
        while (elapsed < labelFadeSeconds)
        {
            elapsed += Time.deltaTime;
            SetLabelAlpha(Mathf.Clamp01(elapsed / labelFadeSeconds));
            yield return null;
        }

        SetLabelAlpha(1f);
    }

    private void SetLabelAlpha(float alpha)
    {
        if (progressLabel != null) progressLabel.alpha = alpha;
        if (messageLabel != null) messageLabel.alpha = alpha;
    }

    /// <summary>
    /// A fixed plate resting on the table, so the first real plate has
    /// something to land on. Kinematic, so it never tips and never counts
    /// toward the goal — it is scenery with a collider.
    /// </summary>
    private void SpawnBasePlate()
    {
        if (platePrefab == null || spawnPoint == null) return;

        Vector3 position = spawnPoint.position + (Vector3)basePlateOffset;
        basePlate = Instantiate(platePrefab, position, Quaternion.identity, stageRoot);
        basePlate.bodyType = RigidbodyType2D.Static;
    }

    protected override void OnEnd()
    {
        if (table != null) table.PlateHitTable -= OnPlateHitTable;

        // The stage is shared scene furniture, so clear the plates this run
        // created rather than leaving them for the next attempt.
        foreach (Rigidbody2D plate in placed)
        {
            if (plate != null) Destroy(plate.gameObject);
        }
        placed.Clear();

        if (sliding != null) Destroy(sliding.gameObject);
        sliding = null;

        if (basePlate != null) Destroy(basePlate.gameObject);
        basePlate = null;
    }

    private void Update()
    {
        if (!InputReady || finished) return;

        // Leaving is always allowed, even mid-fall — the task simply does not
        // complete, and the next attempt starts from an empty table.
        if (Keyboard.current != null && Keyboard.current[quitKey].wasPressedThisFrame)
        {
            finished = true;
            acceptingInput = false;

            EndWithFailure();
            return;
        }

        if (sliding != null)
        {
            SlideCurrentPlate();

            if (acceptingInput && Keyboard.current != null
                && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                DropCurrentPlate();
            }
        }
    }

    /// <summary>Tracks back and forth across the slide width until dropped.</summary>
    private void SlideCurrentPlate()
    {
        float speed = slideSpeed + slideSpeedPerPlate * placed.Count;

        Vector3 position = sliding.transform.position;
        position.x += slideDirection * speed * Time.deltaTime;

        if (position.x > slideOriginX + slideWidth)
        {
            position.x = slideOriginX + slideWidth;
            slideDirection = -1f;
        }
        else if (position.x < slideOriginX)
        {
            position.x = slideOriginX;
            slideDirection = 1f;
        }

        sliding.transform.position = position;
    }

    private void SpawnNextPlate()
    {
        if (platePrefab == null || spawnPoint == null) return;

        // Slide just above whatever is already stacked, so the drop stays short.
        float y = StackTopY() + slideHeightAboveStack;
        var position = new Vector3(slideOriginX, y, spawnPoint.position.z);

        sliding = Instantiate(platePrefab, position, Quaternion.identity, stageRoot);

        // Kinematic while sliding: it is being driven by hand, not by physics.
        sliding.bodyType = RigidbodyType2D.Kinematic;
        sliding.linearVelocity = Vector2.zero;

        slideDirection = 1f;
        acceptingInput = true;
    }

    private void DropCurrentPlate()
    {
        acceptingInput = false;

        // Physics takes over for the short fall onto the stack.
        sliding.bodyType = RigidbodyType2D.Dynamic;

        StartCoroutine(WaitForPlateToSettle(sliding));
        sliding = null;
    }

    /// <summary>
    /// A plate counts as placed once it has held still briefly. Waiting on
    /// stillness rather than a fixed delay means a stack that wobbles for a
    /// moment still resolves correctly.
    /// </summary>
    private IEnumerator WaitForPlateToSettle(Rigidbody2D plate)
    {
        float stillFor = 0f;

        while (plate != null && !finished)
        {
            stillFor = plate.linearVelocity.magnitude <= settleSpeedThreshold
                ? stillFor + Time.deltaTime
                : 0f;

            if (stillFor >= settleSeconds) break;

            yield return null;
        }

        if (finished || plate == null) yield break;

        placed.Add(plate);
        UpdateProgress();

        if (placed.Count >= plateCount)
        {
            finished = true;
            EndWithSuccess();
            yield break;
        }

        SpawnNextPlate();
    }

    /// <summary>Any plate touching the table ends the run.</summary>
    private void OnPlateHitTable()
    {
        if (finished || !InputReady) return;

        finished = true;
        acceptingInput = false;

        EndWithFailure();
    }

    /// <summary>
    /// The height of the settled stack, counting the base plate, or the spawn
    /// height when nothing is down yet.
    /// </summary>
    private float StackTopY()
    {
        float top = float.MinValue;

        if (basePlate != null)
        {
            var baseCollider = basePlate.GetComponent<Collider2D>();
            top = baseCollider != null ? baseCollider.bounds.max.y : basePlate.transform.position.y;
        }

        foreach (Rigidbody2D plate in placed)
        {
            if (plate == null) continue;

            var plateCollider = plate.GetComponent<Collider2D>();
            float plateTop = plateCollider != null
                ? plateCollider.bounds.max.y
                : plate.transform.position.y;

            if (plateTop > top) top = plateTop;
        }

        return top > float.MinValue ? top : spawnPoint.position.y;
    }

    private void UpdateProgress()
    {
        if (progressLabel != null) progressLabel.text = $"{placed.Count} / {plateCount}";
    }

    /// <summary>
    /// Fade the labels out before the panel drops, so they are gone by the time
    /// it starts moving rather than lingering over an empty screen.
    /// </summary>
    protected override IEnumerator EndSequence(bool success)
    {
        // Quitting early can land mid-fade-in; that coroutine would keep
        // raising alpha while this lowers it.
        if (labelFade != null)
        {
            StopCoroutine(labelFade);
            labelFade = null;
        }

        float elapsed = 0f;
        while (elapsed < labelFadeOutSeconds)
        {
            elapsed += Time.deltaTime;
            SetLabelAlpha(1f - Mathf.Clamp01(elapsed / labelFadeOutSeconds));
            yield return null;
        }

        SetLabelAlpha(0f);
    }
}
