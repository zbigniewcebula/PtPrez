using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SomeComponent))]
public class CustomInspector : Editor
{
	private GUIStyle coloredAlignedText;
	private bool overdraw = false;

	private float axisX;
	private float axisY;

	private Texture2D xIcon;
	private Texture2D checkIcon;

	private bool[] foldout = new bool[4];

	private Color bg;
	private Color clr;

	void OnEnable() {
		coloredAlignedText = new GUIStyle(EditorStyles.label);
		coloredAlignedText.alignment = TextAnchor.MiddleRight;

		xIcon = (Texture2D)EditorGUIUtility.Load("x.png");
		checkIcon = (Texture2D)EditorGUIUtility.Load("check.png");

		bg = GUI.backgroundColor;
		clr = GUI.color;

		Init();
	}

	public override void OnInspectorGUI() {
		SomeComponent obj = target as SomeComponent;
		Section4();

		if(!foldout[3]) {
			DrawDefaultInspector();
			Separator();
			Custom();
		}

		Separator();
		foldout[3] = EditorGUILayout.Foldout(foldout[3], "Section 4");
	}

	void Custom() {
		foldout[0] = EditorGUILayout.Foldout(foldout[0], "Section 1");
		if(foldout[0])
			Section1();

		Separator();
		foldout[1] = EditorGUILayout.Foldout(foldout[1], "Section 2");
		if(foldout[1])
			Section2();

		Separator();
		foldout[2] = EditorGUILayout.Foldout(foldout[2], "Section 3");
		if(foldout[2])
			Section3();
	}

	void Section1() {
		SomeComponent obj = target as SomeComponent;

		coloredAlignedText.normal.textColor = obj.someColor;

		EditorGUILayout.LabelField("Some text on right alligment", coloredAlignedText);
		
		Texture2D tex = AssetPreview.GetAssetPreview(obj.someSprite);

		EditorGUILayout.BeginHorizontal();

		obj.someSprite = EditorGUILayout.ObjectField(
			obj.someSprite, typeof(Sprite), false, WH(100)
		) as Sprite;

		GUI.color = obj.someColor;
		EditorGUILayout.LabelField(new GUIContent(xIcon), WH(100));
		GUI.color = clr;

		EditorGUILayout.LabelField(new GUIContent(tex), WH(100));

		EditorGUILayout.EndHorizontal();
	}

	void Section2() {
		SomeComponent obj = target as SomeComponent;

		EditorGUILayout.BeginHorizontal();
		overdraw = EditorGUILayout.Toggle("Overdraw?", overdraw);
		if(overdraw)
			GUI.color = Color.red;
		if(GUILayout.Button(new GUIContent(overdraw ? xIcon : checkIcon), WH(100))) {
			overdraw = !overdraw;
		}
		GUI.color = clr;
		EditorGUILayout.EndVertical();

		if(overdraw)
			Graphics.DrawTexture(new Rect(axisX, axisY, 256, 256), obj.someSprite.texture);
	}

	void Section3() {
		EditorGUILayout.BeginVertical(EditorStyles.helpBox, WH(200));
		EditorGUILayout.LabelField(string.Empty, WH(200));
		EditorGUILayout.EndVertical();
		Rect rect = GUILayoutUtility.GetLastRect();
		Rect rect2 = GUILayoutUtility.GetLastRect();

		rect2.y += axisY * 1.9f;
		axisX = GUI.HorizontalSlider(rect2, axisX, 0, 100);
		rect.x += axisX * 1.9f;
		axisY = GUI.VerticalSlider(rect, axisY, 0, 100);
	}

	private string[] tabs = new string[] {
		"None", "Default", "Custom", "Both"
	};
	private int currentTab = 0;
	private System.Action[] tabAction = new System.Action[4];
	void Init() {
		tabAction[0] = () => {};
		tabAction[1] = () => { DrawDefaultInspector(); };
		tabAction[2] = () => { Custom(); };
		tabAction[3] = () => { DrawDefaultInspector(); Custom(); };
	}
	void Section4() {
		if(foldout[3]) {
			EditorGUILayout.BeginHorizontal();
			for(int i = 0; i < tabs.Length; ++i) {
				if(currentTab == i)
					EditorGUI.BeginDisabledGroup(true);
				if(GUILayout.Button(tabs[i], EditorStyles.miniButtonMid)) {
					currentTab = i;
				}
				if(currentTab == i) {
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();
			Separator();
			tabAction[currentTab]?.Invoke();
		}
	}

	private GUILayoutOption H(float h) => GUILayout.Height(h);
	private GUILayoutOption W(float w) => GUILayout.Width(w);
	private GUILayoutOption[] WH(float wh) => WH(wh, wh);
	private GUILayoutOption[] WH(float w, float h) {
		return new GUILayoutOption[2] { W(w), H(h) };
	}

	private void Separator() {
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, H(0));
		EditorGUILayout.LabelField(string.Empty, H(0));
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();
	}
}
