using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class EnglishDictionary : MonoBehaviour
{
    Dictionary<string,string> m_wordDict = new Dictionary<string, string>();
    public WordlePresenter m_presenter;

    public Dictionary<string, string> GetWordDict()
    {
        return m_wordDict;
    }

    private void Awake()
    {
        PrepareWords(Resources.Load<TextAsset>("englishDictionary"));
    }

    private void PrepareWords(TextAsset englishDictionary)
    {
        Profiler.BeginSample("EnglishDict.Read");
        using (System.IO.StringReader reader = new System.IO.StringReader(englishDictionary.text))
        {
            string rawLine;
            while ((rawLine = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(rawLine))
                    continue;
                
                string[] rawLineParts = rawLine.Split(',', 3);
                if (rawLineParts.Length < 3 || rawLineParts[0] is null ||rawLineParts[2] is null)
                    continue;

                if (rawLineParts[0].Length != m_presenter.Dimension)
                    continue;
                
                if (!rawLineParts[0].All(c => char.IsLetterOrDigit(c)))
                    continue;
                
                if (m_wordDict.ContainsKey(rawLineParts[0].ToUpper()))
                    continue; 
                
                m_wordDict.Add(rawLineParts[0].ToUpper(), rawLineParts[2]);
            }
        }

        Debug.Log($"Amount of long words: {m_wordDict.Count}");
        foreach (var longWordDefinitions in m_wordDict)
        {
            Debug.Log($"{longWordDefinitions.Key} : {longWordDefinitions.Value}");
        }

        Profiler.EndSample();
    }
}
