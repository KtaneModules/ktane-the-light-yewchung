using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TheCuriousLight : MonoBehaviour {

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
   private bool needsHolds = true;
	 private bool hoverEnabled = false;


	 private int[] colorOrder = {0, 3, 4, 1};
   private int current;
	 private int[] colorDisplay;
	 private int[] controls = new int[4];
	 private bool flashed = false;

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
     //StartCoroutine(LightCycle());
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
     current = Rnd.Range(0, 60);
		 colorDisplay = new int[3];
		 colorDisplay[0] = colorOrder[current / 16];
		 colorDisplay[1] = colorOrder[(current % 16) / 4];
		 colorDisplay[2] = colorOrder[current % 4];
		 DebugMessage("Starting number is " + current.ToString());
		 DebugMessage("Initial display colors are " + colornames[colorDisplay[0]] + ", " + colornames[colorDisplay[1]] + ", " + colornames[colorDisplay[2]]);
		 int[] temp = {12, 15, 20, 30};
		 var choices = new List<int>(temp);
		 for (int i = 0; i < 4; i++) {
			 controls[i] = choices[Rnd.Range(0, choices.Count)];
			 choices.Remove(controls[i]);
		 }
		 DebugMessage("Tap associations are: ");
		 DebugMessage("    1: +" + controls[0].ToString());
		 DebugMessage("    2: +" + controls[1].ToString());
		 DebugMessage("    3: +" + controls[2].ToString());
		 DebugMessage("    4: +" + controls[3].ToString());
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
     hoveredColor = 0;
     StartCoroutine(HoverCycle());
   }

   protected void ButtonUnhovered() {
     hovered = false;
     StopAllCoroutines();
     StartCoroutine(LightCycle());
   }

   protected void ButtonInteract1(int i = 0) {
		 if (!flashed) {
			 StartCoroutine(ColorFlashes(colorDisplay));
			 flashed = true;
			 DebugMessage("Colors displayed, color display disabled");
		 } else {
			 if (tapped > 0 && tapped < 5) {
				 DebugMessage("Submitted " + tapped.ToString() + " taps, corresponding to +" + controls[tapped-1].ToString());
			   current += controls[tapped - 1];
				 if (current >= 60) {
				 	 DebugMessage("Current number exceeded upper bound. Flashing " + colornames[colorOrder[current%4]]);
					 StartCoroutine(ColorFlash(colorOrder[current%4]));
					 current -= 60;
				 }
				 DebugMessage("New value is " + current.ToString());
			 }
		 }
   }

   protected void ButtonInteract2(int i = 0) {
		 DebugMessage("Submitting answer as " + current.ToString());
		 if (current == 0) {
			 DebugMessage("0 submitted, module solved!");
			 GetComponent<KMBombModule>().HandlePass();
			 ModuleSolved = true;
			 hoverEnabled = false;
			 color = 0;
		 } else {
			 DebugMessage("Incorrect submission, strike issued");
			 GetComponent<KMBombModule>().HandleStrike();
		 }
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

	 IEnumerator ColorFlashes(int[] cs) {
	 	 hovered = true;
	 	 hoveredColor = cs[0];
		 yield return new WaitForSeconds(0.5f);
		 for (int i = 1; i < cs.Length; i++) {
			 hoveredColor = -1;
			 yield return new WaitForSeconds(0.2f);
			 hoveredColor = cs[i];
			 yield return new WaitForSeconds(0.5f);
		 }
		 hovered = false;
	 }

	 IEnumerator ColorFlash(int c) {
	 	 hoveredColor = c;
		 hovered = true;
		 yield return new WaitForSeconds(0.5f);
		 hovered = false;
	 }

	 void removeAll(System.Collections.Generic.List<int> list, System.Collections.Generic.IEnumerable<int> collection) {
		 for (int i = 0; i < collection.Count(); i++) {
			 list.Remove(collection.ElementAt(i));
		 }
	 }

   void DebugMessage(string message) {
		 Debug.LogFormat("[The Curious Light #{0}] {1}", ModuleId, message);
	 }
}
