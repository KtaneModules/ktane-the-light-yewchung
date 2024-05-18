using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ThePsychicLight : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
	 public KMColorblindMode Colorblind;
	 public KMSelectable lightSelectable;
	 public Light Light;
	 public MeshRenderer lightRenderer;
   public TextMesh colorBlindText;
	 private static readonly string[] colornames = {"White", "Blue", "Yellow", "Red", "Green", "Cyan", "Magenta", "Black"};
   private static readonly Color32 offColor = new Color32(225, 221, 202, 140);
   private static readonly Color32[] colors = {new Color32(235, 235, 235, 180), new Color32(55, 55, 235, 180), new Color32(235, 235, 55, 180),
     new Color32(235, 55, 55, 180), new Color32(55, 235, 55, 180), new Color32(55, 235, 235, 180), new Color32(235, 55, 235, 180),
     new Color32(55, 55, 55, 180) };

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   private int color = -1;
   private int hoveredColor = -1;
   private bool hovered = false;
   private DateTime timer = DateTime.MinValue;
   private System.Timers.Timer tapTimer = new System.Timers.Timer(1000);
   private int taps = 0;
   private int tapped = -1;

   private int[] cycle = {};
   private int[] hoverCycle = {};

   private bool needsMultitaps = true;
   private bool needsHolds = false;
	 private bool hoverEnabled = true;


   private int stagenum = 0;
	 private int[,] prevAnswers = new int[5,3];
	 private int[] colorOrder = {3, 4, 1, 0};
	 private int swapnum = 0;
	 private int[][] swaps = {
		 new int[] {1,4,2,3,2},
		 new int[] {1,3},
		 new int[] {4,1,2,3},
		 new int[] {3,4},
		 new int[] {1,2},
		 new int[] {1,3,2,4,2},
		 new int[] {2,3},
		 new int[] {1,4,3,2}
	 };
	 private string[] colorLetters = {"W", "B", "Y", "R", "G", "C", "M", "K"};
	 private System.Random rand = new System.Random();


   void Awake () {
      ModuleId = ModuleIdCounter++;
      /*
      foreach (KMSelectable object in keypad) {
          object.OnInteract += delegate () { keypadPress(object); return false; };
      }
      */

      //button.OnInteract += delegate () { buttonPress(); return false; };

   }

   void Start () {
		 float scalar = transform.lossyScale.x;
		 Light.range *= scalar;
		 Light.intensity = 10;
		 Light.enabled = false;
		 lightSelectable.OnInteract += delegate ()
		 {
				 if (!ModuleSolved){
             tapTimer.Enabled = false;
						 timer = DateTime.UtcNow;
				 }
         return false;
		 };
		 lightSelectable.OnInteractEnded += delegate ()
		 {
          Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, lightSelectable.transform);
          if (!ModuleSolved){
             TimeSpan interval = DateTime.UtcNow - timer;
             if (!needsHolds || interval.TotalMilliseconds < 300) {
						    ButtonTap();
             } else {
                ButtonHeld();
            }
				 }
		 };
     lightSelectable.OnHighlight += delegate ()
     {
       if (!ModuleSolved && hoverEnabled) {
         ButtonHovered();
       }
     };
     lightSelectable.OnHighlightEnded += delegate ()
     {
       if (!ModuleSolved && hoverEnabled) {
         ButtonUnhovered();
       }
     };
     SetupModule();
     StartCoroutine(LightCycle());
   }

   void Update() {
     if (tapped >= 0) {
       ButtonInteract1();
       tapped = -1;
     }
     if(hovered) {
       LightColorChange(hoveredColor);
     } else {
       LightColorChange(color);
     }
   }

   void SetupModule() {
		 swapnum = (Bomb.GetPortPlateCount() + Bomb.GetBatteryHolderCount()) % 8;
		 int times = Bomb.GetSerialNumberNumbers().Count();
		 performSwaps(colorOrder, swaps[swapnum]);
		 for (int i = 1; i < times; i++) {
			 swapnum = (swapnum + 1) % 8;
			 performSwaps(colorOrder, swaps[swapnum]);
		 }
		 int temp = rand.Next(4);
		 hoveredColor = colorOrder[temp];
		 DebugMessage("Ordering for stage " + (stagenum + 1).ToString() + " is " + colorLetters[colorOrder[0]] + colorLetters[colorOrder[1]] + colorLetters[colorOrder[2]] + colorLetters[colorOrder[3]]);
		 DebugMessage("Display color for stage " + (stagenum + 1).ToString() + " is " + colornames[hoveredColor]);
   }

	 protected void ButtonTap() {
     if (needsMultitaps) {
       taps += 1;
       tapTimer.Dispose();
       tapTimer = new System.Timers.Timer(300);
       tapTimer.Elapsed += TapTimerFinished;
       tapTimer.AutoReset = false;
       tapTimer.Enabled = true;
     } else {
       ButtonInteract1();
     }
	 }

   protected void ButtonHeld() {
     ButtonInteract2();
   }

   protected void ButtonHovered() {
     hovered = true;
     //StartCoroutine(HoverCycle());
   }

   protected void ButtonUnhovered() {
     hovered = false;
     //StartCoroutine(LightCycle());
   }

   protected void ButtonInteract1(int i = 0) {
		 int correct = 0;
		 switch(stagenum) {
			 case 0:
			 	switch(hoveredColor) {
					case 3: correct = Array.IndexOf(colorOrder, 4) + 1; break;
					case 4: correct = Array.IndexOf(colorOrder, 1) + 1; break;
					case 1: correct = Array.IndexOf(colorOrder, 0) + 1; break;
					case 0: correct = Array.IndexOf(colorOrder, 3) + 1; break;
				}
			 break;
			 case 1:
			 	switch(hoveredColor) {
					case 3: correct = Array.IndexOf(colorOrder, 3) + 1; break;
					case 4: correct = Array.IndexOf(colorOrder, prevAnswers[0,1]) + 1; break;
					case 1: correct = 3; break;
					case 0: correct = prevAnswers[0,2]; break;
				}
			 break;
			 case 2:
			 	switch(hoveredColor) {
					case 3: correct = Array.IndexOf(colorOrder, prevAnswers[1,1]) + 1; break;
					case 4: correct = Array.IndexOf(colorOrder, prevAnswers[0,0]) + 1; break;
					case 1: correct = Array.IndexOf(colorOrder, prevAnswers[0,1]) + 1; break;
					case 0: correct = Array.IndexOf(colorOrder, 0) + 1; break;
				}
			 break;
			 case 3:
			 	switch(hoveredColor) {
					case 3: correct = prevAnswers[1,2]; break;
					case 4: correct = prevAnswers[0,2]; break;
					case 1: correct = Array.IndexOf(colorOrder, prevAnswers[1,0]) + 1; break;
					case 0: correct = Array.IndexOf(colorOrder, 3) + 1; break;
				}
			 break;
			 case 4:
			 	switch(hoveredColor) {
					case 3: correct = Array.IndexOf(colorOrder, prevAnswers[3,1]) + 1; break;
					case 4: correct = Array.IndexOf(colorOrder, prevAnswers[2,1]) + 1; break;
					case 1: correct = Array.IndexOf(colorOrder, prevAnswers[1,1]) + 1; break;
					case 0: correct = prevAnswers[3,2]; break;
				}
			 break;
		 }
		 DebugMessage("Stage " + (stagenum + 1).ToString() + ":");
		 DebugMessage("Inputted value is " + tapped.ToString() + ", corresponding with " + colorLetters[colorOrder[tapped - 1]]);
		 DebugMessage("Correct answer is " + correct.ToString() + ", corresponding with " + colorLetters[colorOrder[correct - 1]]);
		 if (tapped == correct) {
			 DebugMessage("Staged passed.");
			 StartCoroutine(GreenFlash());
			 if (stagenum == 4) {
				 DebugMessage("Module solved!");
				 GetComponent<KMBombModule>().HandlePass();
				 ModuleSolved = true;
				 hoverEnabled = false;
			 } else {
				 prevAnswers[stagenum,0] = hoveredColor;
				 prevAnswers[stagenum,1] = colorOrder[correct - 1];
				 prevAnswers[stagenum,2] = correct;
				 stagenum += 1;
				 swapnum = (swapnum + correct) % 8;
				 performSwaps(colorOrder, swaps[swapnum]);
				 int temp = rand.Next(4);
				 hoveredColor = colorOrder[temp];
				 DebugMessage("Ordering for stage " + (stagenum + 1).ToString() + " is " + colorLetters[colorOrder[0]] + colorLetters[colorOrder[1]] + colorLetters[colorOrder[2]] + colorLetters[colorOrder[3]]);
				 DebugMessage("Display color for stage " + (stagenum + 1).ToString() + " is " + colornames[hoveredColor]);
			 }
		 } else {
			 DebugMessage("Incorrect answer, strike.");
			 StartCoroutine(RedFlash());
			 GetComponent<KMBombModule>().HandleStrike();
		 }
   }

   protected void ButtonInteract2(int i = 0) {
   }

   void TapTimerFinished(System.Object source, System.Timers.ElapsedEventArgs e) {
     tapped = taps;
     taps = 0;
   }

   protected void LightColorChange(int i) {
     if (i >= 0) {
       Light.enabled = true;
       Light.color = colors[i];
       lightRenderer.material.color = colors[i];
       if (Colorblind) {
         colorBlindText.text = colornames[i];
       }
     } else {
       LightOff();
     }
   }

   void LightOff() {
     Light.enabled = false;
     lightRenderer.material.color = offColor;
     colorBlindText.text = "";
   }

   IEnumerator LightCycle() {
     if (cycle.Length > 0) {
        int flashnum = 0;
 			  while (!ModuleSolved) {
          color = cycle[flashnum];
          yield return new WaitForSeconds(0.6f);
          flashnum += 1;
          flashnum %= cycle.Length;
        }
     }
   }

   IEnumerator HoverCycle() {
     if (hoverCycle.Length > 0) {
        int flashnum = 0;
 			  while (!ModuleSolved) {
          hoveredColor = hoverCycle[flashnum];
          yield return new WaitForSeconds(0.6f);
          flashnum += 1;
          flashnum %= hoverCycle.Length;
        }
     }
   }

	 IEnumerator GreenFlash() {
		 hoverEnabled = false;
		 hovered = false;
		 color = -1;
		 yield return new WaitForSeconds(0.1f);
		 color = 4;
		 yield return new WaitForSeconds(0.4f);
		 hoverEnabled = true;
		 color = -1;
	 }

	 IEnumerator RedFlash() {
		 hoverEnabled = false;
		 hovered = false;
		 color = -1;
		 yield return new WaitForSeconds(0.1f);
		 color = 3;
		 yield return new WaitForSeconds(0.4f);
		 hoverEnabled = true;
		 color = -1;
	 }

	 void performSwaps(int[] a, int[] swaps) {
		 int initial = swaps[0];
		 int holder = a[swaps[0]-1];
		 int temp = 0;
		 for (int i = 1; i < swaps.Length; i++) {
			 temp = a[swaps[i]-1];
			 a[swaps[i]-1] = holder;
			 holder = temp;
		 }
		 a[initial-1] = holder;
	 }

	 void removeAll(System.Collections.Generic.List<int> list, System.Collections.Generic.IEnumerable<int> collection) {
		 for (int i = 0; i < collection.Count(); i++) {
			 list.Remove(collection.ElementAt(i));
		 }
	 }

   void DebugMessage(string message) {
		 Debug.LogFormat("[The Psychic Light #{0}] {1}", ModuleId, message);
	 }
}
