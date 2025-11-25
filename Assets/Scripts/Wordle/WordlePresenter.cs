using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class WordlePresenter : MonoBehaviour
{
    // todo - basically, i am thinking of having a 20x20 display like wordle
    // and every frame that's displayed has to be a legit word, that's filled in
    // legitimately. now that I think about it, the word itself probably doesn't matter too much... 
    // although it would be cool if it was solved at the end of each frame...? 
    // and for extra credit, use the different colors somehow? like... either use the colors as a form of aliasing
    // OH! once we know we will have a completely green line below us, only then should we use green, and then keep
    // using green downwards, because in a real game of wordle, you would never (well, i wouldn't at least) try a different
    // letter there. but for everything that is not a complete line down, like a far edge of a circle shape, that would 
    // have to be yellow.  
    // does it make sense to require you to have a yellow occurance of the letter somewhere before it being green? no, 
    // not really. 
    // we also need to find a bigass library of words of the size that we decide to use. 
    public int Frames { get; set; }
    public int Dimension => m_dimension;
    private int m_dimension = 12;

    private int m_gridSpacing = 10;
    
    private GameObject m_canvasGameObject;
    [SerializeField] private GameObject m_letterBox;

    private TextMeshProUGUI[,] m_grid;
    
    // wordle green: 538D4E
    // wordle yellow: B59F3B

    void Start()
    {
        m_grid = new TextMeshProUGUI[m_dimension, m_dimension];
        m_canvasGameObject = GetComponentInChildren<Canvas>().gameObject;
        
        // set up grid?
        Rect canvasRect = m_canvasGameObject.GetComponent<RectTransform>().rect;
        Rect letterBoxRect = m_letterBox.GetComponent<RectTransform>().rect;
        int startX = (int)(- canvasRect.width / 2 + letterBoxRect.width / 2 + m_gridSpacing);
        Debug.Log($"startX: {startX}");
        int x = startX;
        int startY = (int)(canvasRect.height / 2 - letterBoxRect.height / 2 - m_gridSpacing);
        int y = startY;
        for (int i = 0; i < m_dimension; i++)
        {
            for (int j = 0; j < m_dimension; j++)
            {
                GameObject o = Instantiate(m_letterBox, new Vector3(x, y, 0), quaternion.identity);
                o.transform.SetParent(m_canvasGameObject.transform, false);
                x += (int)letterBoxRect.width + m_gridSpacing;
                
                m_grid[i, j] = o.GetComponentInChildren<TextMeshProUGUI>();
                m_grid[i, j].text = Random.Range(0, 9).ToString();
            }

            x = startX;
            y -= (int)letterBoxRect.height + m_gridSpacing;
        }
    }

    public void GenerateWordleFrame(NativeArray<float> newPixels, int currentFrame)
    {
        // todo - do stuff with this
    }

    // just for testing
    public void CreateRandomWordleSetup()
    {
        
    }
}
