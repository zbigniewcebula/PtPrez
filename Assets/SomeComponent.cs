using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SomeComponent : MonoBehaviour {
	public int someNumber;
	public string someString;
	[Header("Some Header")]
	public Sprite someSprite;

	public Color someColor = Color.white;
}
