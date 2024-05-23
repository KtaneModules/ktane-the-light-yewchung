using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class NotTheCuriousLight : MonoBehaviour {

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

   private bool needsMultitaps = false;
   private bool needsHolds = true;
	 private bool hoverEnabled = false;


	 private int[] colorOrder = {5, 6, 2, 7};
	 private int[,] lightColors = new int[4, 3];
   private int[,] details = new int[4, 6];
	 private int[] active = new int[6];
	 private bool[] shouldCut = new bool[4];
	 private bool[] cut = new bool[4];
	 private int current = -1;
	 private string[] conditions = {
		 "Sea Level", "Lunar Phase", "Sawdust Percent", "Zodiac Sign", "Baking Time", "Light Color"
	 };

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
			int[] temp = {1, 2, 3};
	 		var slots = new List<int>(temp);
	    if(Bomb.GetSerialNumberNumbers().Last() % 2 == 0 || Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count()) {
				active[0] = slots[0];
				slots.Remove(active[0]);
			} else if (Bomb.GetBatteryCount(Battery.D) > 0) {
				active[1] = slots[1];
				slots.Remove(active[1]);
			} else {
				active[2] = slots[2];
				slots.Remove(active[2]);
			}
			if (Bomb.IsPortPresent(Port.StereoRCA) || Bomb.IsPortPresent(Port.RJ45) || Bomb.IsPortPresent(Port.PS2)) {
				active[3] = slots[0];
				slots.Remove(active[3]);
			} else {
				active[4] = slots[1];
				slots.Remove(active[4]);
			}
			active[5] = slots[0];
			slots.Remove(active[5]);
			DebugMessage("Selected conditions are: " + conditions[Array.IndexOf(active, 1)] + ", " +
					conditions[Array.IndexOf(active, 2)] + ", " + conditions[Array.IndexOf(active, 3)]);

			for(int i = 0; i < 4; i++) {
				int[] colorTemp = new int[3]{Rnd.Range(0, 4), Rnd.Range(0, 4), Rnd.Range(0, 4)};
				lightColors[i, 0] = colorOrder[colorTemp[0]];
				lightColors[i, 1] = colorOrder[colorTemp[1]];
				lightColors[i, 2] = colorOrder[colorTemp[2]];
				details[i, Array.IndexOf(active, 1)] = colorTemp[0] + 1;
				details[i, Array.IndexOf(active, 2)] = colorTemp[1] + 1;
				details[i, Array.IndexOf(active, 3)] = colorTemp[2] + 1;
				DebugMessage("Wire " + (i + 1).ToString() + " colors are " + colornames[lightColors[i,0]] + ", " +
						colornames[lightColors[i,1]] + ", " + colornames[lightColors[i,2]]);
				shouldCut[i] = ShouldWireCut(i);
			}
			while(!shouldCut.Contains(true)) {
				DebugMessage("No cuttable wires, rerolling Wire 4.");
				int[] colorTemp = new int[3]{Rnd.Range(0, 4), Rnd.Range(0, 4), Rnd.Range(0, 4)};
				lightColors[3, 0] = colorOrder[colorTemp[0]];
				lightColors[3, 1] = colorOrder[colorTemp[1]];
				lightColors[3, 2] = colorOrder[colorTemp[2]];
				details[3, Array.IndexOf(active, 1)] = colorTemp[0] + 1;
				details[3, Array.IndexOf(active, 2)] = colorTemp[1] + 1;
				details[3, Array.IndexOf(active, 3)] = colorTemp[2] + 1;
				DebugMessage("Wire 4 colors are " + colornames[lightColors[3,0]] + ", " +
						colornames[lightColors[3,1]] + ", " + colornames[lightColors[3,2]]);
				shouldCut[3] = ShouldWireCut(3);
			}
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
		 do {
		 	 current = (current + 1) % 4;
		 } while (cut[current]);
		 int[] colorsFlash = new int[3];
		 colorsFlash[0] = lightColors[current, 0];
		 colorsFlash[1] = lightColors[current, 1];
		 colorsFlash[2] = lightColors[current, 2];
		 StartCoroutine(ColorFlashes(colorsFlash));
   }

   protected void ButtonInteract2(int i = 0) {
		 if (current == -1) {
			 return;
		 }
		 DebugMessage("Attempting to cut wire " + (current + 1).ToString());
		 if (shouldCut[current]) {
			 DebugMessage("Wire " + (current+1).ToString() + " successfully cut");
			 cut[current] = true;
			 if (cut.Count(c => c == true) == shouldCut.Count(c => c == true)) {
				 DebugMessage("All correct wires cut, module solved!");
				 GetComponent<KMBombModule>().HandlePass();
				 ModuleSolved = true;
				 hoverEnabled = false;
			 } else {
				 ButtonInteract1();
			 }
		 } else {
			 DebugMessage("Incorrect wire cut, issuing strike.");
			 GetComponent<KMBombModule>().HandleStrike();
			 shouldCut[current] = true;
			 cut[current] = true;
			 ButtonInteract1();
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

	 bool ShouldWireCut(int wirenum) {
		 if ((details[wirenum,0] == 4 || details[wirenum,2] == 3) && details[wirenum,4] == 2) {
			 DebugMessage("Wire " + (wirenum + 1).ToString() + " meets condition 1: Any two values are exactly 20.");
			 return true;
		 }
		 if ((new int[]{2,3}.Contains(details[wirenum,3]) || new int[]{3, 4}.Contains(details[wirenum,4])) && !new int[]{2, 3}.Contains(details[wirenum,0]) && !new int[]{3, 4}.Contains(details[wirenum,2])) {
			 DebugMessage("Wire " + (wirenum + 1).ToString() + " meets condition 2: Member of Chinese Zodiac or Overcooked, unless Dry or Inedible.");
			 return true;
		 }
		 if (new int[]{1,4}.Contains(details[wirenum,0]) && !new int[]{2,3}.Contains(details[wirenum,5])) {
			 DebugMessage("Wire " + (wirenum + 1).ToString() + " meets condition 3: High tide, unless Light Color contains Red.");
			 return true;
		 }
		 if (new int[]{1,2}.Contains(details[wirenum,5]) && !new int[]{1,3}.Contains(details[wirenum,3]) && details[wirenum,1] != 4) {
			 DebugMessage("Wire " + (wirenum + 1).ToString() + " meets condition 4: Light color contains Blue, unless Zodiac is Aquatic or New Moon.");
			 return true;
		 }
		 DebugMessage("Wire " + (wirenum + 1).ToString() + " meets no conditions.");
		 return false;
	 }

	 void removeAll(System.Collections.Generic.List<int> list, System.Collections.Generic.IEnumerable<int> collection) {
		 for (int i = 0; i < collection.Count(); i++) {
			 list.Remove(collection.ElementAt(i));
		 }
	 }

   void DebugMessage(string message) {
		 Debug.LogFormat("[Not The Curious Light #{0}] {1}", ModuleId, message);
	 }
}
