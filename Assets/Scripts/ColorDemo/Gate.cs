using UnityEngine;

// a neutral barrier driven by world state: a plate's occupancy or a lever's colour.
// it holds no toggle of its own, so restoring a checkpoint restores the gate too
public class Gate : MonoBehaviour
{
    [SerializeField] private Plate plate;
    [SerializeField] private ColorTarget lever;
    [SerializeField] private ColorId openColor;
    [SerializeField] private Collider blocker;
    [SerializeField] private Transform visual;
    [SerializeField] private float sinkDepth = 2.6f;
    [SerializeField] private float sinkSpeed = 5f;

    private Vector3 closedPosition;

    public bool IsOpen
    {
        get
        {
            if (plate != null) { return plate.IsOccupied; }
            if (lever != null) { return lever.State.Value == openColor; }
            return false;
        }
    }

    public void Configure(Plate controlPlate, ColorTarget controlLever, ColorId leverOpenColor, Collider body, Transform visualRoot)
    {
        plate = controlPlate;
        lever = controlLever;
        openColor = leverOpenColor;
        blocker = body;
        visual = visualRoot;
    }

    void Awake()
    {
        closedPosition = visual.localPosition;
    }

    void FixedUpdate()
    {
        blocker.enabled = !IsOpen;
    }

    void Update()
    {
        Vector3 wanted = closedPosition + (IsOpen ? Vector3.down * sinkDepth : Vector3.zero);
        visual.localPosition = Vector3.MoveTowards(visual.localPosition, wanted, sinkSpeed * Time.deltaTime);
    }
}
