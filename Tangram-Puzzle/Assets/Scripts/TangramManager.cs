using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

/// <summary>
/// Tangram Board Manager
/// </summary>
public class TangramManager : MonoBehaviour
{
    private PieceController selectedPiece = null; // Transform Reference of the currently selected piece
    private List<int> dockedPieces; // List of pieces (indexes) docked in a solution location 
    private List<PieceController> piecesController; // List of PieceController of the Tangram pieces
    private Sprite targetTangram; // Sprite of Tangram shape that should be solutioned
    private Vector2 offsetTangramModel; // Tangram shape position. It is used to adjust the tangram pieces positions
    private Vector2 offsetBGPieces; // Tangram intial shape position. It is used to adjust the tangram pieces positions.

    [SerializeField]
    private DataTangram dataDefault; // Standard DataTangram reference (data on pieces positions/orientations/scales for Tangram shape solution)
    private readonly List<List<InfoPosition>> infoPositions = new List<List<InfoPosition>>(); // All possible InfoPositions for each tangram piece
    private readonly List<Solution> infoSolutions = new List<Solution>(); // All possible solutions
    private readonly List<int> availableSolutions = new List<int>(); // Available solutions (it modifies with a new docked piece)

    public static TangramManager _Instance;

    void Awake()
    {
        if (_Instance == null)
        {
            _Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitTangramManager();
    }

    /// <summary>
    /// Start a Tangram round with a new shape chalenge
    /// </summary>
    /// <param name="nameTangram"></param>
    public void StartTangram(string nameTangram)
    {
        ResetPieces();
        SetTargets(nameTangram);
    }

    /// <summary>
    /// Initialize Tangram Board.
    /// </summary>
    void InitTangramManager()
    {
        piecesController = new List<PieceController>();
        dockedPieces = new List<int>();

        infoPositions.Add(new List<InfoPosition>());
        infoPositions.Add(new List<InfoPosition>());
        infoPositions.Add(new List<InfoPosition>());
        infoPositions.Add(new List<InfoPosition>());
        infoPositions.Add(new List<InfoPosition>());

        int i = 0;
        foreach (Transform trans in transform)
        {
            PieceController pieceController = trans.GetComponent<PieceController>();
            if (pieceController != null)
            {
                pieceController.Id = i;
                pieceController.SetOrderInLayer(i);
                piecesController.Add(pieceController);

                if (trans.name == "Pl")
                    pieceController.CanFlip = true;

                i++;
            }
        }

        InitTangramReferencePositions();
        ResetPieces();
    }

    /// <summary>
    /// Deselect the selected piece (clickout)
    /// </summary>
    public void DeselectPiece()
    {
        if (selectedPiece == null)
            return;

        selectedPiece.SelectPiece(false);
        selectedPiece = null;
    }

    /// <summary>
    /// Set Tangram type/shape. Load sprite-shape and load solution data
    /// </summary>
    /// <param name="type">tipo/forma do Tangram</param>
    public void SetTargets(string type)
    {
        StartCoroutine(LoadDataTangramAsync(type));
        StartCoroutine(LoadImageTangramAsync(type));
    }

    private void InitTangramReferencePositions()
    {
        // Proportion information used to position background and calculate offsets
        // %Horizontal blue area (beige) => 0.38875 (0.61125)
        // %Vertical Rotation Area (BG) => 0.188 (0.812)

        Camera cam = Camera.main;

        float ratio = (float)Screen.width / Screen.height;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * ratio;

        Vector2 centerCam = cam.transform.position;
        Vector2 offsetCam = new Vector2(halfWidth, halfHeight);
        Vector2 bottomLeft = centerCam - offsetCam;

        Transform piecesBG = transform.Find("PiecesBG");
        Transform tangramModel = transform.Find("Tangram");

        piecesBG.position = new Vector3(bottomLeft.x + 0.194375f * 2 * halfWidth, centerCam.y + 0.2f * halfHeight, 0);
        tangramModel.position = new Vector3(bottomLeft.x + 0.694375f * 2 * halfWidth, centerCam.y, 0);

        offsetTangramModel = tangramModel.localPosition;
        offsetBGPieces = piecesBG.localPosition;
    }

    /// <summary>
    /// Adds a piece to the docked pieces list for a given solution
    /// </summary>
    /// <param name="pieceController">Piec PieceController component</param>
    /// <returns></returns>
    private bool AddPiece(PieceController pieceController)
    {
        if (dockedPieces.Contains(pieceController.Id))
            return false;

        // Add piece. If you already have 7 pieces captured, finish the Tangram
        dockedPieces.Add(pieceController.Id);
        if (dockedPieces.Count == 7)
        {
            targetTangram = null;
            transform.Find("Tangram").GetComponent<SpriteRenderer>().sprite = null;
            dockedPieces.Clear();
            if (selectedPiece != null)
            {
                selectedPiece.SelectPiece(false);
                selectedPiece = null;
            }

            GameManager._Instance.FinishTangramSection();
        }

        return true;
    }

    /// <summary>
    /// Check if a given piece can be docked in a solution location.
    /// </summary>
    /// <param name="piece"></param>
    /// <param name="pos"></param>
    /// <param name="rotz"></param>
    /// <param name="flip"></param>
    /// <returns></returns>
    public bool CheckAvailableSlot(PieceController piece, Vector3 pos, float rotz, bool flip)
    {
        bool flipCheck;
        float posDistance;
        float rotDistance;

        List<InfoPosition> availableInfo = piece.Id switch
        {
            0 => GetAvailableInfoPositions(0, 0),
            1 => GetAvailableInfoPositions(1, 0),
            2 => GetAvailableInfoPositions(2, 1),
            3 => GetAvailableInfoPositions(3, 1),
            4 => GetAvailableInfoPositions(4, 2),
            5 => GetAvailableInfoPositions(5, 3),
            6 => GetAvailableInfoPositions(6, 4),
            _ => new List<InfoPosition>()
        };

        // Check if the trangram piece is approximately in some solution location/condition
        foreach (InfoPosition info in availableInfo)
        {
            flipCheck = info.flip == flip;
            posDistance = Vector2.Distance(info.position, pos);

            float angleZ = ((rotz % 360) + 360) % 360;
            rotDistance = Mathf.Min(Mathf.Abs(info.rotZ - angleZ), 360 - Mathf.Abs(info.rotZ - angleZ));

            if (posDistance < 0.25f && rotDistance < 10f && flipCheck)
            {
                piece.SetInfoPosition(info, Vector2.zero, 0f);
                AddPiece(piece);
                UpdateAvailableSolutions();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns a list of available infoPositions based on the available solutions
    /// </summary>
    /// <param name="indexPiece"></param>
    /// <param name="indexInfoPosition"></param>
    /// <returns></returns>
    private List<InfoPosition> GetAvailableInfoPositions(int indexPiece, int indexInfoPosition)
    {
        List<InfoPosition> info = new List<InfoPosition>();

        foreach (int indexSolution in availableSolutions)
        {
            InfoPosition infoPosition = (infoPositions[indexInfoPosition])[infoSolutions[indexSolution].solutionIndexes[indexPiece]];
            if (info.Any((item => item.position == infoPosition.position)))
                continue;

            if (indexPiece == 0 || indexPiece == 2 || indexPiece == 4)
            {
                info.Add(infoPosition);
            }
            if (indexPiece == 1 || indexPiece == 3)
            {
                infoPosition.rotZ = (((infoPosition.rotZ - 90.0f) % 360) + 360) % 360;
                info.Add(infoPosition);
            }
            else if (indexPiece == 5)
            {
                info.Add(infoPosition);
                infoPosition.rotZ = (infoPosition.rotZ + 90) % 360;
                info.Add(infoPosition);
                infoPosition.rotZ = (infoPosition.rotZ + 90) % 360;
                info.Add(infoPosition);
                infoPosition.rotZ = (infoPosition.rotZ + 90) % 360;
                info.Add(infoPosition);
            }
            else if (indexPiece == 6)
            {
                info.Add(infoPosition);
                infoPosition.rotZ = (infoPosition.rotZ + 180) % 360;
                info.Add(infoPosition);
            }
        }
        return info;
    }

    /// <summary>
    /// When a new piece is docked, updates the availableSolutions
    /// </summary>
    private void UpdateAvailableSolutions()
    {
        availableSolutions.Clear();
        for (int i = 0; i < infoSolutions.Count; i++)
            availableSolutions.Add(i);

        foreach (int pieceIndex in dockedPieces)
        {
            int infoPositionIndex = 0;
            if (pieceIndex == 0 || pieceIndex == 1)
                infoPositionIndex = 0;
            else if (pieceIndex == 2 || pieceIndex == 3)
                infoPositionIndex = 1;
            else
                infoPositionIndex = pieceIndex - 2;

            for (int i = 0; i < (infoPositions[infoPositionIndex]).Count; i++)
            {
                if (((Vector2)piecesController[pieceIndex].transform.position) == (infoPositions[infoPositionIndex])[i].position)
                {
                    for (int j = 0; j < infoSolutions.Count; j++)
                    {
                        if (infoSolutions[j].solutionIndexes[pieceIndex] != i)
                            availableSolutions.Remove(j);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a piece is docked in a solution location, if so, remove it from the docked pieces
    /// </summary>
    /// <param name="pieceController">Piece PieceController</param>
    /// <returns></returns>
    public bool CheckAndRemovePiece(PieceController pieceController)
    {
        if (!dockedPieces.Contains(pieceController.Id))
            return false;

        dockedPieces.Remove(pieceController.Id);
        UpdateAvailableSolutions();

        return true;
    }

    /// <summary>
    /// Select a piece
    /// </summary>
    /// <param name="trans">Piece Transform</param>
    public void SelectPiece(PieceController piece)
    {
        if (selectedPiece == piece)
            return;

        // Deselect previous
        if (selectedPiece != null)
        {
            selectedPiece.SelectPiece(false);
        }

        // Ensures that the selected part is "in front" of the others. And guarantees "historical" order
        foreach (PieceController pc in piecesController)
        {
            if (pc.GetOrderInLayer() == 7)
                pc.SetOrderInLayer(5);
            else if (pc.GetOrderInLayer() > piece.GetOrderInLayer())
                pc.SetOrderInLayer((pc.GetOrderInLayer() - 1));
        }

        selectedPiece = piece;
        selectedPiece.SetOrderInLayer(7);
        selectedPiece.SelectPiece(true);
    }

    /// <summary>
    /// Returns if a specific piece is selected
    /// </summary>
    /// <param name="trans">Piece PieceController</param>
    /// <returns></returns>
    public bool IsPieceSelected(PieceController piece)
    {
        return piece == selectedPiece;
    }

    /// <summary>
    /// Reset (default shape) the pieces positions
    /// </summary>
    void ResetPieces()
    {
        piecesController[0].SetInfoPosition(dataDefault.infoPositionsLT[dataDefault.solutions[0].solutionIndexes[0]], offsetBGPieces, 0f);
        piecesController[1].SetInfoPosition(dataDefault.infoPositionsLT[dataDefault.solutions[0].solutionIndexes[1]], offsetBGPieces, -90f);
        piecesController[2].SetInfoPosition(dataDefault.infoPositionsST[dataDefault.solutions[0].solutionIndexes[2]], offsetBGPieces, 0f);
        piecesController[3].SetInfoPosition(dataDefault.infoPositionsST[dataDefault.solutions[0].solutionIndexes[3]], offsetBGPieces, -90f);
        piecesController[4].SetInfoPosition(dataDefault.infoPositionsMD[dataDefault.solutions[0].solutionIndexes[4]], offsetBGPieces, 0f);
        piecesController[5].SetInfoPosition(dataDefault.infoPositionsSq[dataDefault.solutions[0].solutionIndexes[5]], offsetBGPieces, 0f);
        piecesController[6].SetInfoPosition(dataDefault.infoPositionsPl[dataDefault.solutions[0].solutionIndexes[6]], offsetBGPieces, 0f);
    }

    /// <summary>
    /// Load asynchronously, at runtime, a especific DataTangram Asset
    /// </summary>
    /// <param name="state">Name of tangram shape</param>
    /// <returns></returns>
    IEnumerator LoadDataTangramAsync(string nameShape)
    {
        AsyncOperationHandle<DataTangram> opHandle;
        string path = "Assets/Data/TangramData" + nameShape + ".asset";

        opHandle = Addressables.LoadAssetAsync<DataTangram>(path);
        yield return opHandle;

        if (opHandle.Status == AsyncOperationStatus.Succeeded)
        {
            DataTangram data = opHandle.Result;

            infoPositions[0].Clear();
            foreach (InfoPosition info in data.infoPositionsLT)
            {
                InfoPosition infoAux = info;
                infoAux.position += offsetTangramModel;
                infoAux.rotZ = ((info.rotZ % 360) + 360) % 360;
                infoAux.flip = info.flip;
                infoPositions[0].Add(infoAux);
            }

            infoPositions[1].Clear();
            foreach (InfoPosition info in data.infoPositionsST)
            {
                InfoPosition infoAux = info;
                infoAux.position += offsetTangramModel;
                infoAux.rotZ = ((info.rotZ % 360) + 360) % 360;
                infoAux.flip = info.flip;
                infoPositions[1].Add(infoAux);
            }

            infoPositions[2].Clear();
            foreach (InfoPosition info in data.infoPositionsMD)
            {
                InfoPosition infoAux = info;
                infoAux.position += offsetTangramModel;
                infoAux.rotZ = ((info.rotZ % 360) + 360) % 360;
                infoAux.flip = info.flip;
                infoPositions[2].Add(infoAux);
            }

            infoPositions[3].Clear();
            foreach (InfoPosition info in data.infoPositionsSq)
            {
                InfoPosition infoAux = info;
                infoAux.position += offsetTangramModel;
                infoAux.rotZ = ((info.rotZ % 360) + 360) % 360;
                infoAux.flip = info.flip;
                infoPositions[3].Add(infoAux);
            }

            infoPositions[4].Clear();
            foreach (InfoPosition info in data.infoPositionsPl)
            {
                InfoPosition infoAux = info;
                infoAux.position += offsetTangramModel;
                infoAux.rotZ = ((info.rotZ % 360) + 360) % 360;
                infoAux.flip = info.flip;
                infoPositions[4].Add(infoAux);
            }

            infoSolutions.Clear();
            availableSolutions.Clear();
            infoSolutions.AddRange(data.solutions);
            for (int i = 0; i < infoSolutions.Count; i++)
                availableSolutions.Add(i);
        }

        Addressables.Release(opHandle);
    }

    /// <summary>
    /// Load asynchronously, at runtime, a especific Target Sprite Asset
    /// </summary>
    /// <param name="state">Name of tangram shape</param>
    /// <returns></returns>
    IEnumerator LoadImageTangramAsync(string nameAsset)
    {
        AsyncOperationHandle<Sprite> opHandle;
        string path = "Assets/Textures/tangram-" + nameAsset.ToLower() + ".png";

        opHandle = Addressables.LoadAssetAsync<Sprite>(path);
        yield return opHandle;

        if (opHandle.Status == AsyncOperationStatus.Succeeded)
        {
            targetTangram = opHandle.Result;
            transform.Find("Tangram").GetComponent<SpriteRenderer>().sprite = targetTangram;
        }

        Addressables.Release(opHandle);
    }
}