using UnityEngine;

// one encounter plus its incoming connector. captures a snapshot the first time the player
// arrives and restores exactly that snapshot on retry; other regions keep their current state
public class ColorRegion : MonoBehaviour
{
    [SerializeField] private int route;
    [SerializeField] private Vector3 respawn;
    [SerializeField] private bool elevated;
    [SerializeField] private ColorTarget[] targets = new ColorTarget[0];
    [SerializeField] private Rigidbody[] boxes = new Rigidbody[0];

    private bool captured;
    private ColorId playerColor;
    private ColorId[] colors;
    private bool[] spent;
    private Vector3[] boxPositions;
    private Quaternion[] boxRotations;

    public int Route => route;
    public bool Elevated => elevated;

    public void Configure(int routeNumber, Vector3 respawnFeet, bool isElevated, ColorTarget[] regionTargets, Rigidbody[] regionBoxes)
    {
        route = routeNumber;
        respawn = respawnFeet;
        elevated = isElevated;
        targets = regionTargets;
        boxes = regionBoxes;
    }

    public void Capture(ColorId arrivalColor)
    {
        if (captured) { return; }
        captured = true;
        playerColor = arrivalColor;

        colors = new ColorId[targets.Length];
        spent = new bool[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            colors[i] = targets[i].State.Value;
            spent[i] = targets[i].Spent;
        }

        boxPositions = new Vector3[boxes.Length];
        boxRotations = new Quaternion[boxes.Length];
        for (int i = 0; i < boxes.Length; i++)
        {
            boxPositions[i] = boxes[i].position;
            boxRotations[i] = boxes[i].rotation;
        }
    }

    public void Restore(DemoPlayer player)
    {
        if (!captured) { return; }

        ColorContactPolicy.BeginBatch();
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].State.Set(colors[i]);
            targets[i].SetSpent(spent[i]);
        }
        for (int i = 0; i < boxes.Length; i++)
        {
            boxes[i].linearVelocity = Vector3.zero;
            boxes[i].angularVelocity = Vector3.zero;
            boxes[i].position = boxPositions[i];
            boxes[i].rotation = boxRotations[i];
            boxes[i].transform.SetPositionAndRotation(boxPositions[i], boxRotations[i]);
        }
        player.Teleport(respawn, playerColor);
        ColorContactPolicy.EndBatch();

        Physics.SyncTransforms();
    }
}
