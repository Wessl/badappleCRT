using Unity.Collections;
using UnityEngine;

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
    public int Dimension { get; }

    void Start()
    {
        
    }

    public void GenerateWordleFrame(NativeArray<float> newPixels, int currentFrame)
    {
        // todo - do stuff with this
    }

    void Update()
    {
        
    }
}
