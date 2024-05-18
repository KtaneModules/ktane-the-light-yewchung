using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TheBinaryLight : MonoBehaviour {

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
	 private int[] position = new int[2];
	 private int[] answers = new int[7];
	 private int questions = 0;


	 private String[,] questionsText = {
		 	{"Serial number contains A, C, or D.", "RCA port is present.", "BOB indicator is present.", "More battery holders than strikes."},
			{"One or more AA batteries.", "Indicator containing R, C, or A.", "Product of all digits in serial number is even.", "Bomb has lit CAR, NSA, or DVI port."},
			{"Parallel port is present.", "Serial number contains R, J, 4, or 5.", "Number of vowels in all indicator labels is even.", "Have answered at least 4 questions."},
			{"More port plates than D batteries.", "Sum of serial number digits is a multiple of number of battery holders.", "Answered yes to the question before last.", "More yes answers so far than no."}
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
     StartCoroutine(LightCycle());
   }

   void Update() {
     if (tapped >= 0) {
       ButtonInteract1();
       tapped = -1;
     }
     //if(hovered) {
       //LightColorChange(hoveredColor);
     //} else {
       LightColorChange(color);
     //}
   }

   void SetupModule() {
		 if (Bomb.GetBatteryCount() % 2 == 1) {
			 position[0] = 0;
		 } else if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count()) {
			 position[0] = 1;
		 } else if (Bomb.GetPortCount() > Bomb.GetPortPlateCount()) {
			 position[0] = 2;
		 } else if (Bomb.GetTime() >= 0){
			 position[0] = 3;
		 } else {
			 position[0] = 4;
		 }
		 if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0) {
			 position[1] = 0;
		 } else if (Bomb.GetSerialNumber().Any(ch => "AEIOU".Contains(ch))) {
			 position[1] = 1;
		 } else if (Bomb.GetSerialNumberNumbers().Count() >= 2) {
			 position[1] = 2;
		 } else if (Bomb.GetSerialNumber().Any(ch => "Y".Contains(ch))) {
			 position[1] = 3;
		 } else {
			 position[1] = 4;
		 }
		 if(position[0] == 4 || position[1] == 4) {
			 DebugMessage("Final starting conditions failed somehow, any answer will solve module.");
		 } else {
			 DebugMessage("Initial column is " + (position[0] + 1).ToString() + ", intial row is " + (position[1] + 1).ToString());
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
     //StartCoroutine(HoverCycle());
   }

   protected void ButtonUnhovered() {
     hovered = false;
     //StartCoroutine(LightCycle());
   }

   protected void ButtonInteract1(int i = 0) {
     if (binary == 0) {
			 binary = 1;
			 color = 0;
		 } else {
			 binary = 0;
			 color = -1;
     }
   }

   protected void ButtonInteract2(int i = 0) {
		 if (position[0] == 4 || position[1] == 4) {
			 color = 4;
			 GetComponent<KMBombModule>().HandlePass();
			 DebugMessage("Module solved... somehow.");
		 }
     DebugMessage("Current question is \"" + questionsText[position[1], position[0]] + "\"");
		 bool answer = false;
		 switch(position[1]) {
			 case 0:
			 		switch(position[0]) {
						case 0:
							answer = Bomb.GetSerialNumber().Any(ch => "ACD".Contains(ch));
						break;
						case 1:
							answer = Bomb.IsPortPresent(Port.StereoRCA);
						break;
						case 2:
							answer = Bomb.IsIndicatorPresent(Indicator.BOB);
						break;
						case 3:
							answer = Bomb.GetBatteryHolderCount() > Bomb.GetStrikes();
						break;
					}
			 break;
			 case 1:
			 		switch(position[0]) {
						case 0:
							answer = Bomb.GetBatteryCount(Battery.AA) + Bomb.GetBatteryCount(Battery.AAx3) + Bomb.GetBatteryCount(Battery.AAx4) >= 1;
						break;
						case 1:
							answer = Bomb.GetIndicators().Any(str => "RCA".ToArray().Intersect(str.ToArray()).Count() > 0);
						break;
						case 2:
							answer = Bomb.GetSerialNumberNumbers().Aggregate(1, (x,y) => x * y) % 2 == 0;
						break;
						case 3:
							answer = Bomb.IsIndicatorOn(Indicator.CAR) || Bomb.IsIndicatorOn(Indicator.NSA) || Bomb.IsPortPresent(Port.DVI);
						break;
					}
			 break;
			 case 2:
			 		switch(position[0]) {
						case 0:
							answer = Bomb.IsPortPresent(Port.Parallel);
						break;
						case 1:
							answer = Bomb.GetSerialNumber().Any(ch => "RJ45".Contains(ch));
						break;
						case 2:
							answer = Bomb.GetIndicators().Aggregate(0, (x, y) => x + "AEIOU".ToArray().Intersect(y.ToArray()).Count()) % 2 == 0;
						break;
						case 3:
							answer = questions >= 4;
						break;
					}
			 break;
			 case 3:
			 		switch(position[0]) {
						case 0:
							answer = Bomb.GetPortPlateCount() > Bomb.GetBatteryCount(Battery.D);
						break;
						case 1:
							answer = Bomb.GetBatteryCount() > 0 && Bomb.GetSerialNumberNumbers().Sum() % Bomb.GetBatteryHolderCount() == 0;
						break;
						case 2:
							answer = questions > 1 && answers[questions - 2] == 1;
						break;
						case 3:
							answer = answers.Sum() > (questions / 2);
						break;
					}
					break;
				}
			 String[] truthiness = {"False", "True"};
			 int answerInt = answer ? 1 : 0;
			 DebugMessage("Correct answer should be " + truthiness[answerInt]);
			 DebugMessage("Submitted answer is " + truthiness[binary]);

			 if (binary == answerInt) {
				 if (position[0] == 3 && position[1] == 3) {
					 color = 4;
					 GetComponent<KMBombModule>().HandlePass();
					 DebugMessage("Module solved!");
				 } else {
					 StartCoroutine(GreenFlash());
					 if ((answerInt == 0 && position[0] < 3) || position[1] == 3) {
						 position[0] += 1;
					 } else if ((answerInt == 1 && position[1] < 3) || position[0] == 3) {
						 position[1] += 1;
					 }
					 answers[questions] = answerInt;
					 questions += 1;
					 DebugMessage("Answered correctly, moved to question " + (position[0] + 1).ToString() + ", " + (position[1] + 1).ToString());
					 binary = 0;
				 }
			 } else {
				 DebugMessage("Answered incorrectly. Strike incurred.");
				 GetComponent<KMBombModule>().HandleStrike();
				 StartCoroutine(RedFlash());
				 binary = 0;
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
		 color = 4;
		 yield return new WaitForSeconds(0.5f);
		 color = -1;
	 }

	 IEnumerator RedFlash() {
		 color = 3;
		 yield return new WaitForSeconds(0.5f);
		 color = -1;
	 }

	 void removeAll(System.Collections.Generic.List<int> list, System.Collections.Generic.IEnumerable<int> collection) {
		 for (int i = 0; i < collection.Count(); i++) {
			 list.Remove(collection.ElementAt(i));
		 }
	 }

   void DebugMessage(string message) {
		 Debug.LogFormat("[The Binary Light #{0}] {1}", ModuleId, message);
	 }
}
