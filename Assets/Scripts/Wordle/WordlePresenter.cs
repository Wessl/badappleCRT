using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
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

    private Dictionary<string, string> m_words;
    private Dictionary<string,string>.KeyCollection m_keys;

    private TextMeshProUGUI[,] m_grid;
    private Color m_wordleGreen = Color.clear;
    private Color m_wordleOrange = Color.clear;
    private Color m_wordleGrey = Color.clear;
    
    // wordle green: 538D4E
    // wordle yellow: B59F3B
    // wordle grey: 787C7E

    void Start()
    {
        m_grid = new TextMeshProUGUI[m_dimension, m_dimension];
        m_canvasGameObject = GetComponentInChildren<Canvas>().gameObject;
        m_words = FindFirstObjectByType<EnglishDictionary>().GetWordDict();
        m_keys = m_words.Keys;
        
        // le colour
        ColorUtility.TryParseHtmlString("#538D4E",out m_wordleGreen);
        ColorUtility.TryParseHtmlString("#B59F3B",out m_wordleOrange);
        ColorUtility.TryParseHtmlString("#787C7E",out m_wordleGrey);
        
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
    public void CreateRandomWordleSetup(string word)
    {
        bool[] correctPositions = new bool[m_dimension];
        for (int i = 0; i < m_dimension; i++)
        {
            Debug.Log($"this is the word to guess: {word}");
            string guessedWord = GetRandomWord();
            for (int j = 0; j < m_dimension; j++)
            {
                string correctLetter = word[j].ToString().ToUpper();

                string guessedLetter;
                if (correctPositions[j])
                    guessedLetter = correctLetter;
                else
                    guessedLetter = guessedWord[j].ToString().ToUpper();
                    
                Debug.Log($"correct letter here: {correctLetter}, guessedletter: {guessedLetter}");
                m_grid[i, j].text = guessedLetter;
                Image image = m_grid[i, j].transform.parent.GetComponentInChildren<Image>();
                if (correctLetter == guessedLetter)
                {
                    correctPositions[j] = true;
                    image.color = m_wordleGreen;
                }
                else if (word.Contains(guessedLetter))
                    image.color = m_wordleOrange;
                else
                    image.color = m_wordleGrey;
            }   
        }
    }
    
    string m_uppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private string GetRandomLetter()
    {
        return m_uppercaseLetters[Random.Range(0, m_uppercaseLetters.Length - 1)].ToString();
    }

    private string GetRandomWord()
    {
        int wordIndex = Random.Range(0, m_keys.Count - 1);
        return m_keys.ToList()[wordIndex];
    }
}
