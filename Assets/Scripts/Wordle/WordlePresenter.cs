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

    private WordleSquare[,] m_grid;
    private Color m_wordleGreen = Color.clear;
    private Color m_wordleOrange = Color.clear;
    private Color m_wordleGrey = Color.clear;

    private bool[] m_positionsThatMustBeCovered;
    
    // wordle green: 538D4E
    // wordle yellow: B59F3B
    // wordle grey: 787C7E

    void Start()
    {
        m_grid = new WordleSquare[m_dimension+1, m_dimension];
        m_canvasGameObject = GetComponentInChildren<Canvas>().gameObject;
        m_words = FindFirstObjectByType<EnglishDictionary>().GetWordDict();
        m_keys = m_words.Keys.ToList();
        m_positionsThatMustBeCovered = new bool[m_dimension];
        
        // le colour
        ColorUtility.TryParseHtmlString("#538D4E",out m_wordleGreen);
        ColorUtility.TryParseHtmlString("#B59F3B",out m_wordleOrange);
        ColorUtility.TryParseHtmlString("#787C7E",out m_wordleGrey);
        
        // randomize order of word appearances - could do this more often if pattern is noticable?
        Shuffle(m_keys);
        
        // set up grid
        Rect canvasRect = m_canvasGameObject.GetComponent<RectTransform>().rect;
        Rect letterBoxRect = m_letterBox.GetComponent<RectTransform>().rect;
        int startX = (int)(- letterBoxRect.width * m_dimension / 2 + m_gridSpacing);
        int x = startX;
        int startY = (int)(letterBoxRect.height * m_dimension / 2 + letterBoxRect.height / 2 - m_gridSpacing);
        int y = startY;
        for (int i = 0; i < m_grid.GetLength(0); i++)
        {
            for (int j = 0; j < m_grid.GetLength(1); j++)
            {
                GameObject go = Instantiate(m_letterBox, new Vector3(x, y, 0), quaternion.identity);
                go.transform.SetParent(m_canvasGameObject.transform, false);
                x += (int)letterBoxRect.width + m_gridSpacing;
                
                m_grid[i, j] = go.GetComponent<WordleSquare>();
            }

            x = startX;
            y -= (int)letterBoxRect.height + m_gridSpacing;
        }
    }
    
    public static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public void GenerateWordleFrame(NativeArray<float> newPixels, int currentFrame)
    {
        // TODO 1. don't use the same guess more than once 
        // TODO 2. go through all the words first once, to know all the positions - basically positionsThatMustBeCovered[][] <- 2 dimensions
        // cont - then find out a word that should be the "correct" word - needs to satisfy all the other guesses
        // then start making guesses against the "correct" word that is guaranteed to work with as many words as possible. 
        Profiler.BeginSample("GenerateWordleFrame");
        
        string correctWord = m_keys[currentFrame % m_keys.Count];
        List<string> guessedWords = new List<string>();
        for (int i = 0; i <  m_grid.GetLength(0); i++)
        {
            for (int j = 0; j < m_grid.GetLength(1); j++)
            {
                if (i != m_dimension)
                {
                    if (newPixels[(m_dimension - 1 - i) * m_dimension + j] > 0.5f)
                        m_positionsThatMustBeCovered[j] = true;
                    else
                        m_positionsThatMustBeCovered[j] = false;
                }
            }
            
            string fittingWord = GetFittingWordleWord(m_positionsThatMustBeCovered, correctWord, guessedWords);
            guessedWords.Add(fittingWord);
            for (int j = 0; j < m_dimension; j++)
            {
                if (i == m_dimension)
                {
                    m_grid[i, j].TMPro.text = correctWord[j].ToString();
                    m_grid[i, j].Image.color = m_wordleGreen;
                    continue;
                }

                m_grid[i, j].TMPro.text = fittingWord[j].ToString();
                if (m_positionsThatMustBeCovered[j] && fittingWord[j] == correctWord[j])
                {
                    m_grid[i, j].Image.color = m_wordleGreen;
                } else if (m_positionsThatMustBeCovered[j])
                {
                    m_grid[i, j].Image.color = m_wordleOrange;
                }
                else
                {
                    m_grid[i, j].Image.color = m_wordleGrey;
                }
            }
            
        }
        Profiler.EndSample();
    }

    private int m_noWordsFoundCount;
    public int GetNoWordsFoundCount => m_noWordsFoundCount;

    private string GetFittingWordleWord(bool[] positionsThatMustBeCovered, string correctWord, List<string> guessedWords)
    {
        if (positionsThatMustBeCovered.All(e => e))
            return correctWord;
        
        Profiler.BeginSample("GetFittingWordleWord");
        Dictionary<char, int> numberOfTimesCharIsUsed = new();
        
        var correctChars = new HashSet<char>(correctWord); 
        for (int i = 0; i < m_keys.Count; i++)
        {
            bool wordPasses = true;
            
            numberOfTimesCharIsUsed.Clear();
            foreach (var c in correctWord)
            {
                if (numberOfTimesCharIsUsed.ContainsKey(c))
                    numberOfTimesCharIsUsed[c]++;
                else
                    numberOfTimesCharIsUsed.Add(c,1);
            }
            
            // First check for greens. 
            bool[] theseLettersAreAllGoodBoss = new bool[m_keys[i].Length];
            for (int j = 0; j < m_keys[i].Length; j++)
            {
                if (positionsThatMustBeCovered[j] && correctWord[j] == m_keys[i][j])
                {
                    // Consume one. 
                    numberOfTimesCharIsUsed[correctWord[j]]--;
                    theseLettersAreAllGoodBoss[j] = true;
                }
            }
            
            for (int j = 0; j < m_keys[i].Length; j++)
            {
                if (theseLettersAreAllGoodBoss[j])
                    continue;
                
                if (positionsThatMustBeCovered[j])
                {
                    int v = 1;
                    numberOfTimesCharIsUsed.TryGetValue(m_keys[i][j], out v);
                    if (!correctChars.Contains(m_keys[i][j]))
                    {
                        wordPasses = false;
                        break;
                    }
                    else if (v == 0)
                    {
                        wordPasses = false;
                        break;
                    }
                }
                else
                {
                    numberOfTimesCharIsUsed.TryGetValue(m_keys[i][j], out var v);
                    if (correctChars.Contains(m_keys[i][j]) && v > 0)
                    {
                        wordPasses = false;
                        break;
                    }
                }

                if (numberOfTimesCharIsUsed.ContainsKey(m_keys[i][j]))
                    numberOfTimesCharIsUsed[m_keys[i][j]]--;
            }
            
            if (wordPasses)
            {
                return m_keys[i];
            }
        }
        
        Profiler.EndSample();

        m_noWordsFoundCount++;
        return "NOWORD:(";
    }
    
    // some stats for nowordsfound: 
    // 10 letters:  (12315), occurences
    // 9 letters: (4174),
    // 8: (1727)
    
    // 10 looks a lot better than 9. and 10 looks pretty bad. so that says a lot about 9 and 8. 
    

    // just for testing
    HashSet<string> m_guessedWords = new HashSet<string>();

    public void CreateRandomWordleSetup(string word)
    {
        m_guessedWords.Clear();
        Shuffle(m_keys);
        
        bool[] correctPositions = new bool[m_dimension];
        for (int i = 0; i < m_dimension; i++)
        {
            string guessedWord = GetRandomWordGivenGuesses(word, correctPositions);
            for (int j = 0; j < m_dimension; j++)
            {
                string correctLetter = word[j].ToString().ToUpper();

                string guessedLetter = guessedWord[j].ToString().ToUpper();
                    
                m_grid[i, j].TMPro.text = guessedLetter;
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
        for (int i = 0; i < m_keys.Count; i++)
        {
            string word = m_keys[i];
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
