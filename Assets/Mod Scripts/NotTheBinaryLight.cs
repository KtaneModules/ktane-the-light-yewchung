using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class NotTheBinaryLight : MonoBehaviour {

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

	 private int binary = 0;
   private int number = 1;
	 private int goal;
	 private int swap = 0;
	 private string[] lightfunctions = {"-1", "x2"};

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
		 	if (binary == 1) {
				color = 7;
			} else if (binary == 0){
				color = -1;
			}
       LightColorChange(color);
     }
   }

   void SetupModule() {
		 goal = Bomb.GetSerialNumberNumbers().Aggregate(0, (x, y) => (x * 10) + y);
		 DebugMessage("Goal number is " + goal.ToString());
		 DebugMessage("Light off is " + lightfunctions[swap] + ", Light on is " + lightfunctions[(swap+1)%2]);
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
     //StartCoroutine(HoverCycle());
   }

   protected void ButtonUnhovered() {
     hovered = false;
     //StartCoroutine(LightCycle());
   }

   protected void ButtonInteract1(int i = 0) {
     if (binary == 0) {
			 binary = 1;
		 } else {
			 binary = 0;
     }
   }

   protected void ButtonInteract2(int i = 0) {
		 if((swap == 0 && binary == 0) || (swap == 1 && binary == 1)) {
			 number -= 1;
		 } else if ((swap == 0 && binary == 1) || (swap == 1 && binary == 0)) {
			 number *= 2;
		 }
		 if ((number < 1 && goal != 0) || number > (goal + 1)) {
			 DebugMessage("Number left allowed bounds, strike incurred.");
			 DebugMessage("Number reset to 1, light states returned to default functions");
			 GetComponent<KMBombModule>().HandleStrike();
			 StartCoroutine(RedFlash());
			 number = 1;
			 swap = 0;
			 binary = 0;
		 } else {
			 DebugMessage("Number changed to " + number.ToString());
			 var numberstr = (number * number).ToString();
			 int a = (numberstr.Length / 2);
			 int n = (numberstr.ToArray()[a]) - '0';
			 if (n % 2 == 1) {
				 swap = (swap + 1) % 2;
				 DebugMessage("Light roles swapped.");
				 DebugMessage("Light off is " + lightfunctions[swap] + ", Light on is " + lightfunctions[(swap+1)%2]);
			 }
			 if(number == goal) {
				 DebugMessage("Goal number reached, module solved!");
				 GetComponent<KMBombModule>().HandlePass();
				 binary = 2;
				 color = 4;
				 ModuleSolved = true;
			 } else {
				 StartCoroutine(GreenFlash());
			 }
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

	 IEnumerator GreenFlash() {
	 	 hoveredColor = 4;
		 hovered = true;
		 yield return new WaitForSeconds(0.5f);
		 hovered = false;
	 }

	 IEnumerator RedFlash() {
	 	 hoveredColor = 3;
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
		 Debug.LogFormat("[Not The Binary Light #{0}] {1}", ModuleId, message);
	 }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} tap/t [Taps the light] | !{0} long-press/lp [Long-presses the light] | Commands are chainable with spaces";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].ToLowerInvariant().EqualsAny("tap", "t", "long-press", "lp"))
                yield break;
        }
        yield return null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ToLowerInvariant().EqualsAny("tap", "t"))
            {
                lightSelectable.OnInteract();
                lightSelectable.OnInteractEnded();
            }
            else
            {
                lightSelectable.OnInteract();
                while ((DateTime.UtcNow - timer).TotalMilliseconds < 300) yield return null;
                lightSelectable.OnInteractEnded();
            }
            yield return new WaitForSeconds(.1f);
        }
    }
}
