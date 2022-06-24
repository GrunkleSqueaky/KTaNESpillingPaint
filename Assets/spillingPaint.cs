using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class spillingPaint : MonoBehaviour
{

    enum Colors { white, red, orange, yellow, green, blue, purple }
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable[] fieldButtons;
    public KMSelectable[] colorSelector;
    public KMSelectable resetButton;
    public Material[] colorMats;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    Colors selectedColor;
    private List<int[]> initialPositions = new List<int[]>();
    private List<int[]> pressedPositions = new List<int[]>();
    private List<string> initialPosStr = new List<string>();
    private List<string> pressedPosStr = new List<string>();


    int spillCount = 0;

    int[,] field = new int[,]
    {
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0}
    };
    int[,] puzzle = new int[,]
    {
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0},
        {0,0,0,0,0,0}
    };

    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 36; i++)
        {
            int ix = i;
            fieldButtons[ix].OnInteract += delegate () { FieldPress(ix); Audio.PlaySoundAtTransform("fieldSelectSound", fieldButtons[ix].transform); return false; };
        }
        for (int i = 0; i < 6; i++)
        {
            int ix = i;
            colorSelector[ix].OnInteract += delegate () { SelectorPress(ix); return false; };
        }
        resetButton.OnInteract += delegate () { ResetPress(); return false; };
    }

    void FieldPress(int index)
    {
        spillCount++;
        pressedPositions.Add(new int[2] { index, (int)selectedColor });
        pressedPosStr.Add((int)selectedColor + "," + index);
        int y = index / 6;
        int x = index % 6;
        for (int y1 = -1; y1 < 2; y1++)
        {
            for (int x1 = -1; x1 < 2; x1++)
            {
                if ((y + y1 < 0) || (y + y1 > 5) || (x + x1 < 0) || (x + x1 > 5)) { continue; }
                FieldColor(y + y1, x + x1);
            }
        }
        if (spillCount == 7)
        {
            CheckSolve();
        }
    }

    void FieldColor(int y, int x)
    {
        field[y, x] += (int)selectedColor;
        field[y, x] %= 7;
        fieldButtons[y * 6 + x].GetComponent<MeshRenderer>().material = colorMats[field[y, x]];
    }

    void CheckSolve()
    {
        for (int i = 0; i < 36; i++)
        {
            if (field[i / 6, i % 6] != 0)
            {
                ResetPress();
                return;
            }
        }
        Module.HandlePass();
    }

    void SelectorPress(int index)
    {
        colorSelector[index].AddInteractionPunch(0.25f);
        if ((int)selectedColor != index + 1)
        { Audio.PlaySoundAtTransform("colorSelectorSelectSound", colorSelector[index].transform); }
        selectedColor = (Colors)(index + 1);
        resetButton.GetComponent<MeshRenderer>().material = colorMats[index + 1];
    }

    void ResetPress()
    {
        pressedPositions = new List<int[]>();
        pressedPosStr = new List<string>();
        Audio.PlaySoundAtTransform("resetSelectSound", resetButton.transform);
        resetButton.AddInteractionPunch(0.5f);
        spillCount = 0;
        for (int i = 0; i < 36; i++)
        {
            field[i / 6, i % 6] = puzzle[i / 6, i % 6];
            fieldButtons[i].GetComponent<MeshRenderer>().material = colorMats[field[i / 6, i % 6]];
        }
    }

    void Start()
    {
        int[] chosenPositions = Enumerable.Range(0, 36).ToArray().Shuffle().Take(7).ToArray();
        foreach (int pos in chosenPositions)
        {
            selectedColor = (Colors)UnityEngine.Random.Range(1, 7);
            initialPositions.Add(new int[2] { pos, 7 - ((int)selectedColor) });
            initialPosStr.Add((7 - (int)selectedColor) + "," + pos);
            FieldPress(pos);
            Debug.LogFormat("[Spilling Paint #{0}] Spilled {1} at position {2} in reading order.", moduleId, selectedColor.ToString(), pos + 1);
            spillCount = 0;
        }
        for (int i = 0; i < 36; i++)
        {
            puzzle[i / 6, i % 6] = field[i / 6, i % 6];
        }
        resetButton.GetComponent<MeshRenderer>().material = colorMats[0];
        pressedPositions = new List<int[]>();
        pressedPosStr = new List<string>();
    }

    private static readonly Regex tpRegex = new Regex("^((([abcdef][123456])|[roygbp]|red|orange|yellow|green|blue|purple)( |$))+$");

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} red> to change colors (red/orange/yellow/green/blue/purple) , <!{0} A1> to spill the selected color in that cell (A1-F6, letters for columns), and <!{0} reset> to reset. Commands can be chained with spaces, for example <!{0} red A1 A2 blue D3>";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        if (command == "reset")
        {
            yield return null;
            resetButton.OnInteract();
            yield break;
        }
        var m = tpRegex.Match(command);
        if (m.Success)
        {
            yield return null;
            var pieces = m.Groups[0].ToString().Split(' ');
            var selectables = new List<KMSelectable>();

            foreach (var piece in pieces)
            {
                if (piece.Length == 2)
                {
                    fieldButtons[(int.Parse(piece[1].ToString()) - 1) * 6 + "abcdef".IndexOf(piece[0])].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    colorSelector["roygbp".IndexOf(piece[0])].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield break;
        }
        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < pressedPosStr.Count; i++)
        {
            if (!initialPosStr.Contains(pressedPosStr[i]))
            {
                resetButton.OnInteract();
                yield return new WaitForSeconds(0.2f);
                break;
            }
        }
        for (int i = 0; i < 7; i++)
        {
            if (!pressedPosStr.Contains(initialPosStr[i]))
            {
                var a = initialPositions[i][1];
                colorSelector[a - 1].OnInteract();
                yield return new WaitForSeconds(0.1f);
                var b = initialPositions[i][0];
                fieldButtons[b].OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}
