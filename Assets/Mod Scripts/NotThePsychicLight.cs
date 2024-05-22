using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class NotThePsychicLight : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
	 public KMColorblindMode Colorblind;
	 public KMSelectable lightSelectable;
	 public Light Light;
	 public MeshRenderer lightRenderer;
   public TextMesh colorBlindText;
   public bool TwitchCBMode = false;
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

	 private int[] colorOrder;
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
	 private int correct = 0;


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
		 colorOrder = new int[]{5, 6, 2, 7};
		 swapnum = (Bomb.GetPortPlateCount() + Bomb.GetBatteryHolderCount()) % 8;
		 int times = Bomb.GetSerialNumberNumbers().Count();
		 performSwaps(colorOrder, swaps[swapnum]);
		 for (int i = 1; i < times; i++) {
			 swapnum = (swapnum + 1) % 8;
			 performSwaps(colorOrder, swaps[swapnum]);
		 }
		 int temp = Rnd.Range(0,4);
		 hoveredColor = colorOrder[temp];
		 DebugMessage("Displayed color is " + colornames[hoveredColor]);
		 DebugMessage("Initial ordering is " + colorLetters[colorOrder[0]] + colorLetters[colorOrder[1]] + colorLetters[colorOrder[2]] + colorLetters[colorOrder[3]]);

		 correct = 0;
		 if (Bomb.IsIndicatorOn(Indicator.BOB)) {
			 correct = Array.IndexOf(colorOrder, hoveredColor) + 1;
		 } else if (colorOrder[0] == 5 && colorOrder[1] == 6 && colorOrder[2] == 2 && colorOrder[3] == 7) {
			 correct = 1;
		 } else {
			 performSwaps(colorOrder, swaps[Array.IndexOf(colorOrder, hoveredColor) + 1]);
			 if (colorOrder[0] == hoveredColor) {
				 correct = Array.IndexOf(colorOrder, 2) + 1;
			 } else if (Array.IndexOf(colorOrder, 5) == (Array.IndexOf(colorOrder, 6) + 1)) {
				 correct = Array.IndexOf(colorOrder, 7) + 1;
			 } else {
				 while(colorOrder[0] != 5) {
					 performSwaps(colorOrder, swaps[7]);
				 }
				 if (colorOrder[1] == 7 || Bomb.IsIndicatorPresent(Indicator.FRQ)) {
					 correct = Array.IndexOf(colorOrder, hoveredColor) + 1;
				 } else {
					 int[] initialOrder = new int[]{5, 6, 2, 7};
					 correct = Array.IndexOf(colorOrder, initialOrder[(Array.IndexOf(initialOrder, hoveredColor) + 1) % 4]) + 1;
				 }
			 }
		 }
		 DebugMessage("Final initial ordering is " + colorLetters[colorOrder[0]] + colorLetters[colorOrder[1]] + colorLetters[colorOrder[2]] + colorLetters[colorOrder[3]]);
		 DebugMessage("Correct answer is " + correct.ToString() + ", corresponding with " + colorLetters[colorOrder[correct - 1]]);
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
		 DebugMessage("Inputted value is " + tapped.ToString() + ", corresponding with " + colorLetters[colorOrder[tapped - 1]]);
		 if (tapped == correct) {
			 DebugMessage("Module Solved!");
			 GetComponent<KMBombModule>().HandlePass();
			 ModuleSolved = true;
			 hoverEnabled = false;
		 } else {
			 DebugMessage("Incorrect answer, strike. Resetting module.");
			 StartCoroutine(RedFlash());
			 GetComponent<KMBombModule>().HandleStrike();
			 SetupModule();
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
       if (Colorblind.ColorblindModeActive || TwitchCBMode) {
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
		 Debug.LogFormat("[Not The Psychic Light #{0}] {1}", ModuleId, message);
	 }
}
