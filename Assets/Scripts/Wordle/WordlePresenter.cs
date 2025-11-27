using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
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
    private int m_dimension = 8;

    private int m_gridSpacing = 10;
    
    private GameObject m_canvasGameObject;
    [SerializeField] private GameObject m_letterBox;

    private Dictionary<string, string> m_words;
    private List<string> m_keys;

    private TextMeshProUGUI[,] m_grid;
    private Color m_wordleGreen = Color.clear;
    private Color m_wordleOrange = Color.clear;
    private Color m_wordleGrey = Color.clear;
    
    // wordle green: 538D4E
    // wordle yellow: B59F3B
    // wordle grey: 787C7E

    void Start()
    {
        m_grid = new TextMeshProUGUI[m_dimension+1, m_dimension];
        m_canvasGameObject = GetComponentInChildren<Canvas>().gameObject;
        m_words = FindFirstObjectByType<EnglishDictionary>().GetWordDict();
        m_keys = m_words.Keys.ToList();
        
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
        for (int i = 0; i < m_grid.GetLength(0); i++)
        {
            for (int j = 0; j < m_grid.GetLength(1); j++)
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
        // todo - configurable word size, make everything adapt to word size, e.g. put stuff in middle
        // todo - we have a funny wordle solver now, which is kinda cool
        // todo - but what we want is to make a bad apple thingy instead. which should not be too hard
        // todo - we can use similar concepts... either find words that are yellow in the spots that we care about, in order to paint the picture
        // reason why we dont use green is because then we would need to use different words on different lines, which we can't do. 
        // actually we can totally do that nevermind i am dumb. but yeah, we should use yellow until we know we can continue all the way down
        // only then should we use green. that should be doable
        
        // TODO 1. don't use the same guess more than once 
        // TODO 2. go through all the words first once, to know all the positions - basically positionsThatMustBeCovered[][] <- 2 dimensions
        // cont - then find out a word that should be the "correct" word - needs to satisfy all the other guesses
        // then start making guesses against the "correct" word that is guaranteed to work with as many words as possible. 
        Profiler.BeginSample("GenerateWordleFrame");
        bool[] positionsThatMustBeCovered = new bool[m_dimension];
        m_shuffledWords = m_keys.OrderBy(_ => rng.Next()).Select(w => w.ToUpper()).ToList();
        
        string correctWord = m_shuffledWords[Random.Range(0, m_shuffledWords.Count - 1)];
        for (int i = 0; i <  m_grid.GetLength(0); i++)
        {
            for (int j = 0; j < m_grid.GetLength(1); j++)
            {
                if (i != m_dimension)
                {
                    if (newPixels[newPixels.Length - 1 - (i * m_dimension + j)] < 0.5f)
                        positionsThatMustBeCovered[j] = true;
                    else
                        positionsThatMustBeCovered[j] = false;
                }
            }
            
            string fittingWord = GetFittingWordleWord(positionsThatMustBeCovered, correctWord);
            
            for (int j = 0; j < m_dimension; j++)
            {
                Image image = m_grid[i, j].transform.parent.GetComponentInChildren<Image>();
                if (i == m_dimension)
                {
                    m_grid[i, j].text = correctWord.ToString()[j].ToString();
                    image.color = m_wordleGreen;
                    continue;
                }
                // todo - don't do GetComponent here, make a class that has both the text and Image accessible directly. 
                m_grid[i, j].text = fittingWord[j].ToString();
                if (positionsThatMustBeCovered[j] && fittingWord[j] == correctWord[j])
                {
                    image.color = m_wordleGreen;
                } else if (positionsThatMustBeCovered[j])
                {
                    image.color = m_wordleOrange;
                }
                else
                {
                    image.color = m_wordleGrey;
                }
            }
            
        }
        Profiler.EndSample();
    }

    private string GetFittingWordleWord(bool[] positionsThatMustBeCovered, string correctWord)
    {
        Profiler.BeginSample("GetFittingWordleWord");
        
        var correctChars = new HashSet<char>(correctWord); 
        for (int i = 0; i < m_shuffledWords.Count; i++)
        {
            string wordToGuess = m_shuffledWords[i];
            if (wordToGuess == correctWord)
                continue; // Word to guess can't be the same as the correct word, unless it's the last one!
            bool wordPasses = true;
            for (int j = 0; j < wordToGuess.Length; j++)
            {
                if (positionsThatMustBeCovered[j])
                {
                    if (!correctChars.Contains(wordToGuess[j]))
                    {
                        wordPasses = false;
                        break;
                    }
                }
                else if(!positionsThatMustBeCovered[j])
                {
                    if (correctChars.Contains(wordToGuess[j]))
                    {
                        wordPasses = false;
                        break;
                    }
                }
            }

            if (wordPasses)
            {
                return wordToGuess;
            }
        }
        
        Profiler.EndSample();

        return "NOWORDSFOUND";
    }
    

    // just for testing
    HashSet<string> m_guessedWords = new HashSet<string>();
    List<string> m_shuffledWords;

    public void CreateRandomWordleSetup(string word)
    {
        m_guessedWords.Clear();
        m_shuffledWords = m_keys.OrderBy(_ => rng.Next()).ToList();
        
        bool[] correctPositions = new bool[m_dimension];
        for (int i = 0; i < m_dimension; i++)
        {
            string guessedWord = GetRandomWordGivenGuesses(word, correctPositions);
            for (int j = 0; j < m_dimension; j++)
            {
                string correctLetter = word[j].ToString().ToUpper();

                string guessedLetter = guessedWord[j].ToString().ToUpper();
                    
                m_grid[i, j].text = guessedLetter;
                Image image = m_grid[i, j].transform.parent.GetComponentInChildren<Image>();
                if (correctLetter == guessedLetter)
                {
                    correctPositions[j] = true;
                    image.color = m_wordleGreen;
                }
                else if (word.Contains(guessedLetter, StringComparison.InvariantCultureIgnoreCase))
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

    private static System.Random rng = new System.Random();
    private string GetRandomWordGivenGuesses(string correctWord, bool[] correctGuesses)
    {
        if (m_guessedWords.Contains(correctWord))
            return correctWord;
        for (int i = 0; i < m_shuffledWords.Count; i++)
        {
            string word = m_shuffledWords[i];
            if (m_guessedWords.Contains(word))
                continue;
            bool allCorrect = true;
            for (int j = 0; j < word.Length; j++)
            {
                if (correctGuesses[j] && word[j] != correctWord[j])
                {
                    allCorrect = false;
                    break;
                }
            }

            if (allCorrect)
            {
                m_guessedWords.Add(word);
                return word;
            }
        }

        return "ballsack";
    }
}
