﻿using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using KMHelper;
using System.Collections;

public class functionality : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable patchLeft;
    public KMSelectable patchRight;
    public TextMesh textLeft, textRight;
    public MeshRenderer Buttons;

    private static readonly int[] colorInv = { 7, 4, 5, 6, 1, 2, 3, 0 };
    private static readonly Color[] colors = { new Color32(0, 0, 0, 255), new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255), new Color32(0, 255, 255, 255), new Color32(255, 0, 255, 255), new Color32(255, 255, 0, 255), new Color32(255, 255, 255, 255) };
    private static readonly string[] names = { "TELEPHONE", "MAVERICK", "ADLER", "CHIP", "MAJIRA", "FLUKE" };
    private static readonly int[,] colorTable = { { -1, 10, 3, 9, 8, 4, 5, 11 }, { 4, -1, 7, 12, 6, 2, 2, 7 }, { 7, 5, -1, 3, 3, 3, 8, 3 }, { 4, 4, 0, -1, 0, 6, 0, 5 }, { 3, 2, 3, 13, 0, 3, 5, 5 }, { 7, 2, 8, 4, 5, -1, 7, 7 }, { 1, 7, 3, 1, 2, 0, -1, 8 }, { 5, 14, 4, 7, 15, 6, 6, -1 } };
    private string[] labelNames = { "UWU", "OWO", "AWOO", "RAWR", "MAWS", "PAWS" };
    private string[] labelCodes = { "UVW", "012", "ABC", "XYZ", "ESY", "NOU" };
    private string[] colorNames = { "black", "red", "green", "blue", "cyan", "magenta", "yellow", "white" };
    private bool _isSolved, _lightsOn, _inv;
    private byte input;
    private int inputCnt;
    private int leftColor, rightColor, leftLabel, rightLabel;
    private int goalId, answer;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    //Bomb generation (loading screen)
    void Start()
    {
        leftColor = Random.Range(0, 8);
        rightColor = Random.Range(0, 7);
        if(rightColor >= leftColor) { rightColor = rightColor + 1; };
        leftLabel = Random.Range(0, 6);
        rightLabel = Random.Range(0, 5);
        if(rightLabel >= leftLabel) { rightLabel = rightLabel + 1; };
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
        Debug.LogFormat("[Popufur #{0}] Left color is {1}, label is {2}. Right color is {3}, label is {4}.", _moduleId, colorNames[leftColor], labelNames[leftLabel], colorNames[rightColor], labelNames[rightLabel]);

        //button rendering
        Buttons.material.SetColor("_ColorL", colors[leftColor]);
        Buttons.material.SetColor("_ColorR", colors[rightColor]);

        textLeft.text = labelNames[leftLabel];
        textRight.text = labelNames[rightLabel];
        textLeft.color = colors[colorInv[leftColor]];
        textRight.color = colors[colorInv[rightColor]];

        string sn = Info.GetSerialNumber();
        string indc = string.Join("", Info.GetIndicators().ToArray());

        if(_inv = sn.Any(labelCodes[rightLabel].Contains))
            Debug.LogFormat("[Popufur #{0}] Left button is 1.", _moduleId);
        else
            Debug.LogFormat("[Popufur #{0}] Right button is 1.", _moduleId);

        goalId = colorTable[rightColor, leftColor];
        if(goalId == -1)
        {
            Module.HandlePass();
            _isSolved = true;
            Debug.LogFormat("[Popufur #{0}] Error during generation, solving module.", _moduleId);
        }
        else if(goalId > 9)
        {
            int nameId = goalId - 10;
            int total = 0;
            foreach(char c in names[nameId])
            {
                total = total + indc.Count(f => (f == c));
            }
            goalId = total % 10;
        }
        Debug.LogFormat("[Popufur #{0}] Key number is {1}.", _moduleId, goalId);

        answer = (Info.GetBatteryCount() + 1) * (goalId + 1);
    }

    //room shown (lights off)
    private void Awake()
    {
        patchLeft.OnInteract += delegate ()
        {
            handleLeft();
            return false;
        };
        patchRight.OnInteract += delegate ()
        {
            handleRight();
            return false;
        };
    }

    //timer starts (lights on)
    void Activate()
    {
        Init();
        _lightsOn = true;
    }

    //initialization for starts
    void Init()
    {
        //var reset
        inputCnt = 0;
        input = 0;
    }

    //left button handling
    void handleLeft()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, patchLeft.transform);
        patchLeft.AddInteractionPunch();

        Debug.LogFormat("[Popufur #{0}] Left button pressed.", _moduleId);

        if(!_lightsOn || _isSolved) return;

        input = (byte)((input << 1) | (_inv ? 1 : 0));
        inputCnt = inputCnt + 1;
        Debug.LogFormat("[Popufur #{0}] Input #{1} received on left button.", _moduleId, inputCnt);
        Debug.LogFormat("[Popufur #{0}] Your submission is currently {1}.", _moduleId, input << (8 - inputCnt), 2);
        if(inputCnt == 8)
            check();
    }

    //right button handling
    void handleRight()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, patchRight.transform);
        patchRight.AddInteractionPunch();

        Debug.LogFormat("[Popufur #{0}] Right button pressed.", _moduleId);

        if(!_lightsOn || _isSolved) return;

        input = (byte)(input << 1 | (_inv ? 0 : 1));
        inputCnt = inputCnt + 1;
        Debug.LogFormat("[Popufur #{0}] Input #{1} received on right button.", _moduleId, inputCnt);
        Debug.LogFormat("[Popufur #{0}] Your submission is currently {1}.", _moduleId, input << (8 - inputCnt));
        if(inputCnt == 8)
            check();
    }

    //handle answer checking
    void check()
    {
        Debug.LogFormat("[Popufur #{0}] Expected answer {1}, you input {2}.", _moduleId, answer, input);

        if(input == answer)
        {
            Module.HandlePass();
            Debug.LogFormat("[Popufur #{0}] Module solved!", _moduleId);
            _isSolved = true;
        }
        else
        {
            Debug.LogFormat("[Popufur #{0}] Module strike!", _moduleId);
            Module.HandleStrike();
            Init();
        }
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "Use \"!{0} press 'label'\" to press the patch of fur with that label. You can chain multiple presses with \"!{0} press 'label1' 'label2' ...\". The word \"press\" is optional.";
#pragma warning restore 0414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        Debug.LogFormat("[Popufur #{0}] TP command received.", _moduleId);

        command = command.ToLowerInvariant().Trim();
        if (!command.StartsWith("press"))
            command = " " + command;

        if (Regex.IsMatch(command, @"^(press)?(\s+((" + labelNames[leftLabel].ToLowerInvariant() + ")|(" + labelNames[rightLabel].ToLowerInvariant() + ")))+$"))
        {
            Debug.LogFormat("[Popufur #{0}] TP command valid.", _moduleId);
            if (!command.StartsWith("press"))
                command = command.Trim();

            string[] commandBits = Regex.Split(command, @"\s+");

            if(commandBits[0] == "press")
                commandBits = commandBits.Skip(1).ToArray();

            KMSelectable[] TPOutput = new KMSelectable[commandBits.Length];

            for(int i = 0; i < commandBits.Length; i++)
            {
                if(commandBits[i] == labelNames[leftLabel].ToLowerInvariant())
                    TPOutput[i] = patchLeft;
                else
                    TPOutput[i] = patchRight;
            }
            return TPOutput;
        }

        return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Popufur #{0}] Module force solved.", _moduleId);
        if(input << (8 - inputCnt) != (answer & (65280 >> inputCnt))) // 0b1111_1111_0000_0000
        {
            Debug.LogFormat("[Popufur #{0}] Previous inputs were incorrect; forcing the module into a solved state.", _moduleId);
            Module.HandlePass();
            _isSolved = true;
            yield break;
        }

        while(!_isSolved)
        {
            ((((1 << (7 - inputCnt)) & answer) != 0) ^ _inv ? patchRight : patchLeft).OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}