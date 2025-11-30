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

    private bool[] m_positionsThatMustBeCovered;
    
    // wordle green: 538D4E
    // wordle yellow: B59F3B
    // wordle grey: 787C7E

    void Start()
    {
        m_grid = new TextMeshProUGUI[m_dimension+1, m_dimension];
        m_canvasGameObject = GetComponentInChildren<Canvas>().gameObject;
        m_words = FindFirstObjectByType<EnglishDictionary>().GetWordDict();
        m_keys = m_words.Keys.ToList();
        m_positionsThatMustBeCovered = new bool[m_dimension];
        
        // le colour
        ColorUtility.TryParseHtmlString("#538D4E",out m_wordleGreen);
        ColorUtility.TryParseHtmlString("#B59F3B",out m_wordleOrange);
        ColorUtility.TryParseHtmlString("#787C7E",out m_wordleGrey);
        
        // set up grid?
        Rect canvasRect = m_canvasGameObject.GetComponent<RectTransform>().rect;
        Rect letterBoxRect = m_letterBox.GetComponent<RectTransform>().rect;
        int startX = (int)(- letterBoxRect.width * m_dimension / 2 + m_gridSpacing);
        Debug.Log($"startX: {startX}");
        int x = startX;
        int startY = (int)(letterBoxRect.height * m_dimension / 2 + letterBoxRect.height / 2 - m_gridSpacing);
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
        // TODO 1. don't use the same guess more than once 
        // TODO 2. go through all the words first once, to know all the positions - basically positionsThatMustBeCovered[][] <- 2 dimensions
        // cont - then find out a word that should be the "correct" word - needs to satisfy all the other guesses
        // then start making guesses against the "correct" word that is guaranteed to work with as many words as possible. 
        Profiler.BeginSample("GenerateWordleFrame");
        
        m_shuffledWords = m_keys.OrderBy(_ => rng.Next()).Select(w => w.ToUpper()).ToList();
        
        string correctWord = m_shuffledWords[Random.Range(0, m_shuffledWords.Count - 1)];
        List<string> guessedWords = new List<string>();
        for (int i = 0; i <  m_grid.GetLength(0); i++)
        {
            for (int j = 0; j < m_grid.GetLength(1); j++)
            {
                if (i != m_dimension)
                {
                    if (newPixels[newPixels.Length - 1 - (i * m_dimension + j)] < 0.5f)
                        m_positionsThatMustBeCovered[j] = true;
                    else
                        m_positionsThatMustBeCovered[j] = false;
                }
            }
            
            string fittingWord = GetFittingWordleWord(m_positionsThatMustBeCovered, correctWord, guessedWords);
            guessedWords.Add(fittingWord);
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
                if (m_positionsThatMustBeCovered[j] && fittingWord[j] == correctWord[j])
                {
                    image.color = m_wordleGreen;
                } else if (m_positionsThatMustBeCovered[j])
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

    private string GetFittingWordleWord(bool[] positionsThatMustBeCovered, string correctWord, List<string> guessedWords)
    {
        Profiler.BeginSample("GetFittingWordleWord");
        
        var correctChars = new HashSet<char>(correctWord); 
        for (int i = 0; i < m_shuffledWords.Count; i++)
        {
            string wordToGuess = m_shuffledWords[i];
            if (wordToGuess == correctWord)
                continue; // Word to guess can't be the same as the correct word or one of the already guessed words!
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
