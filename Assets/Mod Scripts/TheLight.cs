using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TheLight : MonoBehaviour {

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
	 private bool hoverEnabled = false;


   private int[] position = new int[2];
   private int[] goal = new int[2];
   private int facing = 0;
   private int maze;

   private int[,,] mazes = {
     {{3, 10, 9}, {7, 8, 5}, {4, 2, 12}},
     {{2, 9, 1}, {2, 13, 5}, {2, 14, 12}},
     {{2, 11, 8}, {3, 14, 9}, {6, 8, 4}}
   };

   String[] directions = {"North", "East", "South", "West"};

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
       //LightColorChange(hoveredColor);
     } else {
       //LightColorChange(color);
     }
   }

   void SetupModule() {
     var serialNumNums = Bomb.GetSerialNumberNumbers();
     int tempnum = 0;
     foreach (int i in serialNumNums) {
       tempnum += i;
     }
     maze = tempnum % 3;
     DebugMessage("Chosen maze is #" + maze.ToString());

     position[0] = Bomb.GetOffIndicators().Count() % 3;
     position[1] = Bomb.GetOnIndicators().Count() % 3;
     DebugMessage("Starting position is (" + position[0].ToString() + "," + position[1].ToString() + ")");

     goal[0] = Bomb.GetPortPlateCount() % 3;
     goal[1] = Bomb.GetBatteryHolderCount() % 3;
     if (position[0] == goal[0] && position[1] == goal[1]) {
       goal[0] = (goal[0] + 1) % 3;
       goal[1] = (goal[1] + 1) % 3;
     }
     DebugMessage("Goal position is (" + goal[0].ToString() + "," + goal[1].ToString() + ")");
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
     if (tapped == 1) {
       facing = (facing + 1) % 4;
       DebugMessage("Rotated clockwise to face " + directions[facing]);
     } else if (tapped == 2) {
       int pass = mazes[maze, position[1], position[0]];
       int check = (pass >> facing) & 1;
       if (check == 1) {
         switch (facing) {
           case 0:
             position[1] += 1;
             break;
           case 1:
             position[0] += 1;
             break;
           case 2:
             position[1] -= 1;
             break;
           case 3:
             position[0] -= 1;
             break;
         }
         DebugMessage("Moved " + directions[facing] + " to position (" + position[0].ToString() + "," + position[1].ToString() + ")");

         if (position[0] == goal[0] && position[1] == goal[1]) {
           LightColorChange(0);
           DebugMessage("Goal position reached, module solved!");
    			 GetComponent<KMBombModule>().HandlePass();
    			 ModuleSolved = true;
         }
       }
     } else if (tapped == 4) {
       DebugMessage("Striking to reset to starting conditions.");
       GetComponent<KMBombModule>().HandleStrike();
       SetupModule();
       facing = 0;
     }
   }

   protected void ButtonInteract2(int i = 0) {
     /*if (color >= 0) {
       color -= 1;
     } else {
       color = 6;
     }*/
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

	 void removeAll(System.Collections.Generic.List<int> list, System.Collections.Generic.IEnumerable<int> collection) {
		 for (int i = 0; i < collection.Count(); i++) {
			 list.Remove(collection.ElementAt(i));
		 }
	 }

   void DebugMessage(string message) {
		 Debug.LogFormat("[The Light #{0}] {1}", ModuleId, message);
	 }
}
