//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2015 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_WP_8_1 || UNITY_BLACKBERRY)
#define MOBILE
#endif

#if !UNITY_IOS

// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9 || UNITY_4_10
#define UNITY_4
#endif
#if UNITY_ANDROID
#define UNITY_ANROID_KEYBOARD
#endif
#endif
// ---------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.Text;

// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if MOBILE && UNITY_ANDROID
using Input = AndroidKeyboard.Input;
using TouchScreenKeyboard = AndroidKeyboard.TouchScreenKeyboard;
#endif
#endif
// ---------------------------------------------------------------

/// <summary>
/// Input field makes it possible to enter custom information within the UI.
/// </summary>

[AddComponentMenu("NGUI/UI/Input Field")]
public class UIInput : MonoBehaviour
{
	public enum InputType
	{
		Standard,
		AutoCorrect,
		Password,
	}

	public enum Validation
	{
		None,
		Integer,
		Float,
		Alphanumeric,
		Username,
		Name,
	}

	public enum KeyboardType
	{
		Default = 0,
		ASCIICapable = 1,
		NumbersAndPunctuation = 2,
		URL = 3,
		NumberPad = 4,
		PhonePad = 5,
		NamePhonePad = 6,
		EmailAddress = 7,
	}

	public enum OnReturnKey
	{
		Default,
		Submit,
		NewLine,
	}

	public delegate char OnValidate (string text, int charIndex, char addedChar);

	/// <summary>
	/// Currently active input field. Only valid during callbacks.
	/// </summary>

	static public UIInput current;

	/// <summary>
	/// Currently selected input field, if any.
	/// </summary>

	static public UIInput selection;

	/// <summary>
	/// Text label used to display the input's value.
	/// </summary>

	public UILabel label;

	/// <summary>
	/// Type of data expected by the input field.
	/// </summary>

	public InputType inputType = InputType.Standard;

	/// <summary>
	/// What to do when the Return key is pressed on the keyboard.
	/// </summary>

	public OnReturnKey onReturnKey = OnReturnKey.Default;

	/// <summary>
	/// Keyboard type applies to mobile keyboards that get shown.
	/// </summary>

	public KeyboardType keyboardType = KeyboardType.Default;

	/// <summary>
	/// Whether the input will be hidden on mobile platforms.
	/// </summary>

	public bool hideInput = false;

	// ---------------------------------------------------------------
#if USING_AndroidKeyboard
	// Whether the keyboard will keep on mobile platforms.
	public bool keepKeyboardOn = false;
#endif
	// ---------------------------------------------------------------

	/// <summary>
	/// Whether all text will be selected when the input field gains focus.
	/// </summary>

	[System.NonSerialized]
	public bool selectAllTextOnFocus = true;

	/// <summary>
	/// What kind of validation to use with the input field's data.
	/// </summary>

	public Validation validation = Validation.None;

	/// <summary>
	/// Maximum number of characters allowed before input no longer works.
	/// </summary>

	public int characterLimit = 0;

	/// <summary>
	/// Field in player prefs used to automatically save the value.
	/// </summary>

	public string savedAs;

	/// <summary>
	/// Don't use this anymore. Attach UIKeyNavigation instead.
	/// </summary>

	[HideInInspector][SerializeField] GameObject selectOnTab;

	/// <summary>
	/// Color of the label when the input field has focus.
	/// </summary>

	public Color activeTextColor = Color.white;

	/// <summary>
	/// Color used by the caret symbol.
	/// </summary>

	public Color caretColor = new Color(1f, 1f, 1f, 0.8f);

	/// <summary>
	/// Color used by the selection rectangle.
	/// </summary>

	public Color selectionColor = new Color(1f, 223f / 255f, 141f / 255f, 0.5f);

	/// <summary>
	/// Event delegates triggered when the input field submits its data.
	/// </summary>

	public List<EventDelegate> onSubmit = new List<EventDelegate>();

	/// <summary>
	/// Event delegates triggered when the input field's text changes for any reason.
	/// </summary>

	public List<EventDelegate> onChange = new List<EventDelegate>();

	/// <summary>
	/// Custom validation callback.
	/// </summary>

	public OnValidate onValidate;

	/// <summary>
	/// Input field's value.
	/// </summary>

	[SerializeField][HideInInspector] protected string mValue;

	[System.NonSerialized] protected string mDefaultText = "";
	[System.NonSerialized] protected Color mDefaultColor = Color.white;
	[System.NonSerialized] protected float mPosition = 0f;
	[System.NonSerialized] protected bool mDoInit = true;
	[System.NonSerialized] protected UIWidget.Pivot mPivot = UIWidget.Pivot.TopLeft;
	[System.NonSerialized] protected bool mLoadSavedValue = true;

	static protected int mDrawStart = 0;
	static protected string mLastIME = "";

#if MOBILE
	// Unity fails to compile if the touch screen keyboard is used on a non-mobile device
	static protected TouchScreenKeyboard mKeyboard;
	static bool mWaitForKeyboard = false;
#endif
	[System.NonSerialized] protected int mSelectionStart = 0;
	[System.NonSerialized] protected int mSelectionEnd = 0;
	[System.NonSerialized] protected UITexture mHighlight = null;
	[System.NonSerialized] protected UITexture mCaret = null;
	[System.NonSerialized] protected Texture2D mBlankTex = null;
	[System.NonSerialized] protected float mNextBlink = 0f;
	[System.NonSerialized] protected float mLastAlpha = 0f;
	[System.NonSerialized] protected string mCached = "";
	[System.NonSerialized] protected int mSelectMe = -1;

	/// <summary>
	/// Default text used by the input's label.
	/// </summary>

	public string defaultText
	{
		get
		{
			if (mDoInit) Init();
			return mDefaultText;
		}
		set
		{
			if (mDoInit) Init();
			mDefaultText = value;
			UpdateLabel();
		}
	}

	/// <summary>
	/// Should the input be hidden?
	/// </summary>

	public bool inputShouldBeHidden
	{
		get
		{
#if UNITY_METRO
return true;
#else
			return hideInput && label != null && !label.multiLine && inputType != InputType.Password;
#endif
		}
	}

	[System.Obsolete("Use UIInput.value instead")]
	public string text { get { return this.value; } set { this.value = value; } }

	/// <summary>
	/// Input field's current text value.
	/// </summary>

	public string value
	{
		get
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return "";
#endif
			if (mDoInit) Init();
			return mValue;
		}
		set
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			if (mDoInit) Init();
			mDrawStart = 0;

			// BB10's implementation has a bug in Unity
#if UNITY_4_3
if (Application.platform == RuntimePlatform.BB10Player)
#else
			if (Application.platform == RuntimePlatform.BlackBerryPlayer)
#endif
			value = value.Replace("\\b", "\b");

			// Validate all input
			value = Validate(value);
#if MOBILE
			if (isSelected && mKeyboard != null && mCached != value)
			{
				mKeyboard.text = value;
				mCached = value;
			}
#endif
			if (mValue != value)
			{
				mValue = value;
				mLoadSavedValue = false;

				if (isSelected)
				{
					if (string.IsNullOrEmpty(value))
					{
						mSelectionStart = 0;
						mSelectionEnd = 0;
					}
					else
					{
						mSelectionStart = value.Length;
						mSelectionEnd = mSelectionStart;
					}
				}
				else SaveToPlayerPrefs(value);

				UpdateLabel();
				ExecuteOnChange();
			}
		}
	}

	[System.Obsolete("Use UIInput.isSelected instead")]
	public bool selected { get { return isSelected; } set { isSelected = value; } }

	/// <summary>
	/// Whether the input is currently selected.
	/// </summary>

	public bool isSelected
	{
		get
		{
			return selection == this;
		}
		set
		{
			// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if MOBILE && UNITY_ANDROID
			if (TouchScreenKeyboard.instance != null && TouchScreenKeyboard.instance.OnReturnKey && !value)
				return;
#endif
#endif
			// ---------------------------------------------------------------

			if (!value) { if (isSelected) UICamera.selectedObject = null; }
			else UICamera.selectedObject = gameObject;
		}
	}

	/// <summary>
	/// Current position of the cursor.
	/// </summary>

	public int cursorPosition
	{
		get
		{
#if MOBILE
			// ---------------------------------------------------------------
#if UNITY_ANROID_KEYBOARD //USING_AndroidKeyboard
if (mKeyboard != null)
{
if (inputShouldBeHidden)
{
if (isSelected)
{
return mSelectionEnd;
}
else
{
return value.Length;
}
}
else
{
return value.Length;
}
}
#else
			if (mKeyboard != null && !inputShouldBeHidden) return value.Length;
#endif
			// ---------------------------------------------------------------
#endif
			return isSelected ? mSelectionEnd : value.Length;
		}
		set
		{
			if (isSelected)
			{
#if MOBILE
				if (mKeyboard != null && !inputShouldBeHidden) return;
#endif
				mSelectionEnd = value;
				UpdateLabel();
			}
		}
	}

	/// <summary>
	/// Index of the character where selection begins.
	/// </summary>

	public int selectionStart
	{
		get
		{
#if MOBILE
			if (mKeyboard != null && !inputShouldBeHidden) return 0;
#endif
			return isSelected ? mSelectionStart : value.Length;
		}
		set
		{
			if (isSelected)
			{
#if MOBILE
				if (mKeyboard != null && !inputShouldBeHidden) return;

				// ---------------------------------------------------------------
#if UNITY_ANROID_KEYBOARD //USING_AndroidKeyboard
if (!string.IsNullOrEmpty(Input.compositionString))
{
var compo = Input.compositionString;
Input.compositionString = "";
Insert(compo);

AndroidKeyboardManager.ClearComposition();
}
#endif
				// ---------------------------------------------------------------
#endif
				mSelectionStart = value;
				UpdateLabel();
			}
		}
	}

	/// <summary>
	/// Index of the character where selection ends.
	/// </summary>

	public int selectionEnd
	{
		get
		{
#if MOBILE
			if (mKeyboard != null && !inputShouldBeHidden) return value.Length;
#endif
			return isSelected ? mSelectionEnd : value.Length;
		}
		set
		{
			if (isSelected)
			{
#if MOBILE
				if (mKeyboard != null && !inputShouldBeHidden) return;
#endif
				mSelectionEnd = value;
				UpdateLabel();
			}
		}
	}

	/// <summary>
	/// Caret, in case it's needed.
	/// </summary>

	public UITexture caret { get { return mCaret; } }

	/// <summary>
	/// Validate the specified text, returning the validated version.
	/// </summary>

	public string Validate (string val)
	{
		if (string.IsNullOrEmpty(val)) return "";

		StringBuilder sb = new StringBuilder(val.Length);

		for (int i = 0; i < val.Length; ++i)
		{
			char c = val[i];
			if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
			else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
			if (c != 0) sb.Append(c);
		}

		if (characterLimit > 0 && sb.Length > characterLimit)
			return sb.ToString(0, characterLimit);
		return sb.ToString();
	}

	/// <summary>
	/// Automatically set the value by loading it from player prefs if possible.
	/// </summary>

	void Start ()
	{
		if (selectOnTab != null)
		{
			UIKeyNavigation nav = GetComponent<UIKeyNavigation>();

			if (nav == null)
			{
				nav = gameObject.AddComponent<UIKeyNavigation>();
				nav.onDown = selectOnTab;
			}
			selectOnTab = null;
			NGUITools.SetDirty(this);
		}

		if (mLoadSavedValue && !string.IsNullOrEmpty(savedAs)) LoadValue();
		else value = mValue.Replace("\\n", "\n");
	}

	/// <summary>
	/// Labels used for input shouldn't support rich text.
	/// </summary>

	protected void Init ()
	{
		if (mDoInit && label != null)
		{
			mDoInit = false;
			mDefaultText = label.text;
			mDefaultColor = label.color;
			label.supportEncoding = false;

			if (label.alignment == NGUIText.Alignment.Justified)
			{
				label.alignment = NGUIText.Alignment.Left;
				Debug.LogWarning("Input fields using labels with justified alignment are not supported at this time", this);
			}

			mPivot = label.pivot;
			mPosition = label.cachedTransform.localPosition.x;
			UpdateLabel();
		}
	}

	/// <summary>
	/// Save the specified value to player prefs.
	/// </summary>

	protected void SaveToPlayerPrefs (string val)
	{
		if (!string.IsNullOrEmpty(savedAs))
		{
			if (string.IsNullOrEmpty(val)) PlayerPrefs.DeleteKey(savedAs);
			else PlayerPrefs.SetString(savedAs, val);
		}
	}

#if !MOBILE
[System.NonSerialized] UIInputOnGUI mOnGUI;
#endif
	/// <summary>
	/// Selection event, sent by the EventSystem.
	/// </summary>

	protected virtual void OnSelect (bool isSelected)
	{
		if (isSelected)
		{
#if !MOBILE
if (mOnGUI == null)
mOnGUI = gameObject.AddComponent<UIInputOnGUI>();
#endif
			OnSelectEvent();
		}
		else
		{
#if !MOBILE
if (mOnGUI != null)
{
Destroy(mOnGUI);
mOnGUI = null;
}
#endif
			OnDeselectEvent();
		}
	}

	// ---------------------------------------------------------------
#if USING_AndroidKeyboard
	static int selectedUIInputID = 0;
#endif
	// ---------------------------------------------------------------

	/// <summary>
	/// Notification of the input field gaining selection.
	/// </summary>

	protected void OnSelectEvent ()
	{
		selection = this;
		// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if MOBILE && UNITY_ANDROID
		selectedUIInputID = this.GetInstanceID();
		OnSelectEvent_Android();
#endif
#endif
		// ---------------------------------------------------------------

		if (mDoInit) Init();

		// Unity has issues bringing up the keyboard properly if it's in "hideInput" mode and you happen
		// to select one input in the same Update as de-selecting another.
		if (label != null && NGUITools.GetActive(this)) mSelectMe = Time.frameCount;
	}

	public static void CloseKeyboardImmediate()
	{
#if USING_AndroidKeyboard
#if MOBILE && UNITY_ANDROID
		mWaitForKeyboard = false;
		if(mKeyboard != null)
		{
			mKeyboard.active = false;
			mKeyboard = null;
		}
#endif
#endif
	}

	/// <summary>
	/// Notification of the input field losing selection.
	/// </summary>

	protected void OnDeselectEvent ()
	{
		if (mDoInit) Init();

		if (label != null && NGUITools.GetActive(this))
		{
			mValue = value;
#if MOBILE
			if (mKeyboard != null)
			{
				// ---------------------------------------------------------------
#if UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
if(string.IsNullOrEmpty(Input.compositionString) == false)
Insert(Input.compositionString);

Input.compositionString = "";

if (AndroidKeyboard.AdditionalOptions.keepKeyboardOn == false)
{
mWaitForKeyboard = false;
mKeyboard.active = false;
mKeyboard = null;
}
#else
				mWaitForKeyboard = false;
				mKeyboard.active = false;
				mKeyboard = null;
#endif
				// ---------------------------------------------------------------
			}
#endif
			if (string.IsNullOrEmpty(mValue))
			{
				label.text = mDefaultText;
				label.color = mDefaultColor;
			}
			else label.text = mValue;

			Input.imeCompositionMode = IMECompositionMode.Auto;
			RestoreLabelPivot();
		}

		selection = null;
		UpdateLabel();
	}

	/// <summary>
	/// Update the text based on input.
	/// </summary>

	protected virtual void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif

		// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if MOBILE && UNITY_ANDROID
		if (mKeyboard == null || mKeyboard.active == false || AndroidKeyboard.AdditionalOptions.keepKeyboardOn == false)
		{
			if(!isSelected)
				return;
		}

		if(selectedUIInputID != this.GetInstanceID())
			return;
#else
if (!isSelected) 
return;
#endif
#else
if (isSelected)
#endif
		// ---------------------------------------------------------------
		{
			if (mDoInit) Init();
#if MOBILE
			// Wait for the keyboard to open. Apparently mKeyboard.active will return 'false' for a while in some cases.
			if (mWaitForKeyboard)
			{
				if (mKeyboard != null && !mKeyboard.active) return;
				mWaitForKeyboard = false;
			}
#endif
			// Unity has issues bringing up the keyboard properly if it's in "hideInput" mode and you happen
			// to select one input in the same Update as de-selecting another.
			if (mSelectMe != -1 && mSelectMe != Time.frameCount)
			{
				mSelectMe = -1;
				mSelectionEnd = string.IsNullOrEmpty(mValue) ? 0 : mValue.Length;
				mDrawStart = 0;
				mSelectionStart = selectAllTextOnFocus ? 0 : mSelectionEnd;
				label.color = activeTextColor;
#if MOBILE

				// ---------------------------------------------------------------
#if UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
AndroidKeyboardManager.SetCursorPosition(mSelectionStart, mSelectionEnd);
#endif
				// ---------------------------------------------------------------

				RuntimePlatform pf = Application.platform;
				if (pf == RuntimePlatform.IPhonePlayer
				    || pf == RuntimePlatform.Android
				    || pf == RuntimePlatform.WP8Player
#if UNITY_4_3
|| pf == RuntimePlatform.BB10Player
#else
				    || pf == RuntimePlatform.BlackBerryPlayer
				    || pf == RuntimePlatform.MetroPlayerARM
				    || pf == RuntimePlatform.MetroPlayerX64
				    || pf == RuntimePlatform.MetroPlayerX86
#endif
				   )
				{
					// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if !UNITY_ANDROID
string val;
TouchScreenKeyboardType kt;

if (inputShouldBeHidden)
{
TouchScreenKeyboard.hideInput = true;
kt = (TouchScreenKeyboardType)((int)keyboardType);
#if UNITY_METRO
val = "";
#else
val = "|";
#endif
				}
				else if (inputType == InputType.Password)
				{
					TouchScreenKeyboard.hideInput = false;
					kt = TouchScreenKeyboardType.Default;
					val = mValue;
					mSelectionStart = mSelectionEnd;
				}
				else
				{
					TouchScreenKeyboard.hideInput = false;
					kt = (TouchScreenKeyboardType)((int)keyboardType);
					val = mValue;
					mSelectionStart = mSelectionEnd;
				}

				mWaitForKeyboard = true;
				mKeyboard = (inputType == InputType.Password) ?
					TouchScreenKeyboard.Open(val, kt, false, false, true) :
					TouchScreenKeyboard.Open(val, kt, !inputShouldBeHidden && inputType == InputType.AutoCorrect,
					                             label.multiLine && !hideInput, false, false, defaultText);

#endif
#endif
				// ---------------------------------------------------------------
#if UNITY_METRO
mKeyboard.active = true;
#endif
			}
			else
#endif // MOBILE
			{
				Vector2 pos = (UICamera.current != null && UICamera.current.cachedCamera != null) ?
					UICamera.current.cachedCamera.WorldToScreenPoint(label.worldCorners[0]) :
					label.worldCorners[0];
				pos.y = Screen.height - pos.y;
				Input.imeCompositionMode = IMECompositionMode.On;
				Input.compositionCursorPos = pos;
			}

			UpdateLabel();
			if (string.IsNullOrEmpty(Input.inputString)) return;
		}
#if MOBILE
		if (mKeyboard != null)
		{
			// ---------------------------------------------------------------
#if UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
Update_AndroidKeyboard(false);
#else

#if UNITY_METRO
string text = Input.inputString;
if (!string.IsNullOrEmpty(text)) Insert(text);
#else
			string text = mKeyboard.text;

			if (inputShouldBeHidden)
			{
				if (text != "|")
				{
					if (!string.IsNullOrEmpty(text))
					{
						Insert(text.Substring(1));
					}
					else DoBackspace();

					mKeyboard.text = "|";
				}
			}
			else if (mCached != text)
			{
				mCached = text;
				value = text;
			}
#endif // UNITY_METRO
			if (mKeyboard.done || !mKeyboard.active)
			{
				if (!mKeyboard.wasCanceled) Submit();
				mKeyboard = null;
				isSelected = false;
				mCached = "";
			}
#endif  
			//---------------------------------------------------------------
		}
		else
#endif // MOBILE
		{
			string ime = Input.compositionString;

			// There seems to be an inconsistency between IME on Windows, and IME on OSX.
			// On Windows, Input.inputString is always empty while IME is active. On the OSX it is not.
			// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
if (!string.IsNullOrEmpty(Input.inputString))
#else
			// Unity NGUI 4.3 UIInput에서 한글짤림현상수정 by.moonjh 2015.04.23
			//if (string.IsNullOrEmpty(ime) && !string.IsNullOrEmpty(Input.inputString))
			if (!string.IsNullOrEmpty(Input.inputString)) 
#endif
			// ---------------------------------------------------------------
			{
				// Process input ignoring non-printable characters as they are not consistent.
				// Windows has them, OSX may not. They get handled inside OnGUI() instead.
				string s = Input.inputString;

				for (int i = 0; i < s.Length; ++i)
				{
					char ch = s[i];
					if (ch < ' ') continue;

					// OSX inserts these characters for arrow keys
					if (ch == '\uF700') continue;
					if (ch == '\uF701') continue;
					if (ch == '\uF702') continue;
					if (ch == '\uF703') continue;

					Insert(ch.ToString());
				}

				// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
Input.inputString = "";
#endif
				// ---------------------------------------------------------------
			}

			// Append IME composition
			if (mLastIME != ime)
			{
				// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
//mSelectionEnd = string.IsNullOrEmpty(ime) ? mSelectionStart : mValue.Length + ime.Length;
mLastIME = ime;
#else
				mSelectionEnd = string.IsNullOrEmpty(ime) ? mSelectionStart : mValue.Length + ime.Length;
				mLastIME = ime;
#endif
				// ---------------------------------------------------------------

				UpdateLabel();
				ExecuteOnChange();
			}
		}

		// Blink the caret
		if (mCaret != null && mNextBlink < RealTime.time)
		{
			mNextBlink = RealTime.time + 0.5f;
			mCaret.enabled = !mCaret.enabled;
		}

		// If the label's final alpha changes, we need to update the drawn geometry,
		// or the highlight widgets (which have their geometry set manually) won't update.
		// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
// 			if (isSelected && mLastAlpha != label.finalAlpha)
// 				UpdateLabel();
#else
		if (isSelected && mLastAlpha != label.finalAlpha)
			UpdateLabel();
#endif
		// ---------------------------------------------------------------
		// Having this in OnGUI causes issues because Input.inputString gets updated *after* OnGUI, apparently...
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			bool newLine = (onReturnKey == OnReturnKey.NewLine) ||
				(onReturnKey == OnReturnKey.Default &&
				     label.multiLine && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
				     label.overflowMethod != UILabel.Overflow.ClampContent &&
				     validation == Validation.None);

			if (newLine)
			{
				Insert("\n");
			}
			else
			{
				UICamera.currentScheme = UICamera.ControlScheme.Controller;
				UICamera.currentKey = KeyCode.Return;
				Submit();
				UICamera.currentKey = KeyCode.None;
			}
		}
	}
}

/// <summary>
/// Perform a backspace operation.
/// </summary>

protected void DoBackspace ()
{
	if (!string.IsNullOrEmpty(mValue))
	{
		if (mSelectionStart == mSelectionEnd)
		{
			if (mSelectionStart < 1) return;
			--mSelectionEnd;
		}
		Insert("");
	}
}

#if !MOBILE
/// <summary>
/// Handle the specified event.
/// </summary>

public virtual bool ProcessEvent (Event ev)
{
if (label == null) return false;

RuntimePlatform rp = Application.platform;

bool isMac = (
rp == RuntimePlatform.OSXEditor ||
rp == RuntimePlatform.OSXPlayer ||
rp == RuntimePlatform.OSXWebPlayer);

bool ctrl = isMac ?
((ev.modifiers & EventModifiers.Command) != 0) :
((ev.modifiers & EventModifiers.Control) != 0);

// http://www.tasharen.com/forum/index.php?topic=10780.0
if ((ev.modifiers & EventModifiers.Alt) != 0) ctrl = false;

bool shift = ((ev.modifiers & EventModifiers.Shift) != 0);

switch (ev.keyCode)
{
case KeyCode.Backspace:
{
ev.Use();
DoBackspace();
return true;
}

case KeyCode.Delete:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
if (mSelectionStart == mSelectionEnd)
{
if (mSelectionStart >= mValue.Length) return true;
++mSelectionEnd;
}
Insert("");
}
return true;
}

case KeyCode.LeftArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = Mathf.Max(mSelectionEnd - 1, 0);
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.RightArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = Mathf.Min(mSelectionEnd + 1, mValue.Length);
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.PageUp:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = 0;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.PageDown:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = mValue.Length;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.Home:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
if (label.multiLine)
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.Home);
}
else mSelectionEnd = 0;

if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.End:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
if (label.multiLine)
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.End);
}
else mSelectionEnd = mValue.Length;

if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.UpArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.UpArrow);
if (mSelectionEnd != 0) mSelectionEnd += mDrawStart;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.DownArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.DownArrow);
if (mSelectionEnd != label.processedText.Length) mSelectionEnd += mDrawStart;
else mSelectionEnd = mValue.Length;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

// Select all
case KeyCode.A:
{
if (ctrl)
{
ev.Use();
mSelectionStart = 0;
mSelectionEnd = mValue.Length;
UpdateLabel();
}
return true;
}

// Copy
case KeyCode.C:
{
if (ctrl)
{
ev.Use();
NGUITools.clipboard = GetSelection();
}
return true;
}

// Paste
case KeyCode.V:
{
if (ctrl)
{
ev.Use();
Insert(NGUITools.clipboard);
}
return true;
}

// Cut
case KeyCode.X:
{
if (ctrl)
{
ev.Use();
NGUITools.clipboard = GetSelection();
Insert("");
}
return true;
}
}
return false;
}
#endif

/// <summary>
/// Insert the specified text string into the current input value, respecting selection and validation.
/// </summary>

protected virtual void Insert (string text)
{
	string left = GetLeftText();
	string right = GetRightText();
	int rl = right.Length;

	StringBuilder sb = new StringBuilder(left.Length + right.Length + text.Length);
	sb.Append(left);

	// Append the new text
	for (int i = 0, imax = text.Length; i < imax; ++i)
	{
		// If we have an input validator, validate the input first
		char c = text[i];

		if (c == '\b')
		{
			DoBackspace();
			continue;
		}

		// Can't go past the character limit
		if (characterLimit > 0 && sb.Length + rl >= characterLimit) break;

		if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
		else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);

		// Append the character if it hasn't been invalidated
		if (c != 0) sb.Append(c);
	}

	// Advance the selection
	mSelectionStart = sb.Length;
	mSelectionEnd = mSelectionStart;

	// Append the text that follows it, ensuring that it's also validated after the inserted value
	for (int i = 0, imax = right.Length; i < imax; ++i)
	{
		char c = right[i];
		if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
		else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
		if (c != 0) sb.Append(c);
	}

	mValue = sb.ToString();
	UpdateLabel();
	ExecuteOnChange();
}

/// <summary>
/// Get the text to the left of the selection.
/// </summary>

protected string GetLeftText ()
{
	int min = Mathf.Min(mSelectionStart, mSelectionEnd);
	return (string.IsNullOrEmpty(mValue) || min < 0) ? "" : mValue.Substring(0, min);
}

/// <summary>
/// Get the text to the right of the selection.
/// </summary>

protected string GetRightText ()
{
	int max = Mathf.Max(mSelectionStart, mSelectionEnd);
	return (string.IsNullOrEmpty(mValue) || max >= mValue.Length) ? "" : mValue.Substring(max);
}

/// <summary>
/// Get currently selected text.
/// </summary>

protected string GetSelection ()
{
	if (string.IsNullOrEmpty(mValue) || mSelectionStart == mSelectionEnd)
	{
		return "";
	}
	else
	{
		int min = Mathf.Min(mSelectionStart, mSelectionEnd);
		int max = Mathf.Max(mSelectionStart, mSelectionEnd);
		return mValue.Substring(min, max - min);
	}
}

/// <summary>
/// Helper function that retrieves the index of the character under the mouse.
/// </summary>

protected int GetCharUnderMouse ()
{
	Vector3[] corners = label.worldCorners;
	Ray ray = UICamera.currentRay;
	Plane p = new Plane(corners[0], corners[1], corners[2]);
	float dist;
	return p.Raycast(ray, out dist) ? mDrawStart + label.GetCharacterIndexAtPosition(ray.GetPoint(dist), false) : 0;
}

/// <summary>
/// Move the caret on press.
/// </summary>

protected virtual void OnPress (bool isPressed)
{
	if (isPressed && isSelected && label != null &&
	        (UICamera.currentScheme == UICamera.ControlScheme.Mouse ||
	         UICamera.currentScheme == UICamera.ControlScheme.Touch))
	{
#if !UNITY_EDITOR && (UNITY_WP8 || UNITY_WP_8_1)
if (mKeyboard != null) mKeyboard.active = true;
#endif

		// ---------------------------------------------------------------
#if USING_AndroidKeyboard
#if MOBILE && UNITY_ANDROID
		if (string.IsNullOrEmpty(Input.compositionString) == false)
			Insert(Input.compositionString);

		Input.compositionString = "";

		if(inputShouldBeHidden)
		{
			if(mKeyboard == null)
			{
				OnSelectEvent_Android();
			}
		}
		else
		{
			if(mKeyboard != null)
			{
				mKeyboard.text = "";
				AndroidKeyboardManager.SetText("");
			}
			else
			{
				OnSelectEvent_Android();
			}

		}

		AndroidKeyboardManager.ClearComposition();

		mSelectionStart  = GetCharUnderMouse();
#endif

		selectionEnd = GetCharUnderMouse();

#if MOBILE && UNITY_ANDROID
		AndroidKeyboardManager.SetCursorPosition(mSelectionStart, mSelectionEnd);
#endif

		if (!Input.GetKey(KeyCode.LeftShift) &&
		        !Input.GetKey(KeyCode.RightShift)) selectionStart = mSelectionEnd;
#else
selectionEnd = GetCharUnderMouse();
if (!Input.GetKey(KeyCode.LeftShift) &&
!Input.GetKey(KeyCode.RightShift)) selectionStart = mSelectionEnd;
#endif
		// ---------------------------------------------------------------
	}
}

/// <summary>
/// Drag selection.
/// </summary>

protected virtual void OnDrag (Vector2 delta)
{
	if (label != null &&
	        (UICamera.currentScheme == UICamera.ControlScheme.Mouse ||
	         UICamera.currentScheme == UICamera.ControlScheme.Touch))
	{
		selectionEnd = GetCharUnderMouse();

		// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
AndroidKeyboardManager.SetCursorPosition(mSelectionStart, mSelectionEnd);
#endif
	}
	// ---------------------------------------------------------------
}

/// <summary>
/// Ensure we've released the dynamically created resources.
/// </summary>

void OnDisable () { Cleanup(); }

/// <summary>
/// Cleanup.
/// </summary>

protected virtual void Cleanup ()
{
	if (mHighlight) mHighlight.enabled = false;
	if (mCaret) mCaret.enabled = false;

	if (mBlankTex)
	{
		NGUITools.Destroy(mBlankTex);
		mBlankTex = null;
	}
}

/// <summary>
/// Submit the input field's text.
/// </summary>

public void Submit ()
{
	if (NGUITools.GetActive(this))
	{
		mValue = value;

		if (current == null)
		{
			current = this;
			EventDelegate.Execute(onSubmit);
			current = null;
		}
		SaveToPlayerPrefs(mValue);
	}
}

/// <summary>
/// Update the visual text label.
/// </summary>

public void UpdateLabel ()
{
	if (label != null)
	{
		if (mDoInit) Init();
		bool selected = isSelected;
		// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
Compsitionstring = Input.compositionString;

if (mKeyboard != null && mKeyboard.active && AndroidKeyboard.AdditionalOptions.keepKeyboardOn)
selected = true;
#endif
		// ---------------------------------------------------------------

		string fullText = value;
		bool isEmpty = string.IsNullOrEmpty(fullText) && string.IsNullOrEmpty(Input.compositionString);
		label.color = (isEmpty && !selected) ? mDefaultColor : activeTextColor;
		string processed;

		if (isEmpty)
		{
			processed = selected ? "" : mDefaultText;
			RestoreLabelPivot();
		}
		else
		{
			if (inputType == InputType.Password)
			{
				processed = "";

				string asterisk = "*";

				if (label.bitmapFont != null && label.bitmapFont.bmFont != null &&
				        label.bitmapFont.bmFont.GetGlyph('*') == null) asterisk = "x";

				for (int i = 0, imax = fullText.Length; i < imax; ++i) processed += asterisk;
			}
			else processed = fullText;

			// Start with text leading up to the selection
			int selPos = selected ? Mathf.Min(processed.Length, cursorPosition) : 0;
			string left = processed.Substring(0, selPos);

			// Append the composition string and the cursor character
			if (selected) left += Input.compositionString;

			// Append the text from the selection onwards
			processed = left + processed.Substring(selPos, processed.Length - selPos);

			// Clamped content needs to be adjusted further
			if (selected && label.overflowMethod == UILabel.Overflow.ClampContent && label.maxLineCount == 1)
			{
				// Determine what will actually fit into the given line
				int offset = label.CalculateOffsetToFit(processed);

				if (offset == 0)
				{
					mDrawStart = 0;
					RestoreLabelPivot();
				}
				else if (selPos < mDrawStart)
				{
					mDrawStart = selPos;
					SetPivotToLeft();
				}
				else if (offset < mDrawStart)
				{
					mDrawStart = offset;
					SetPivotToLeft();
				}
				else
				{
					offset = label.CalculateOffsetToFit(processed.Substring(0, selPos));

					if (offset > mDrawStart)
					{
						mDrawStart = offset;
						SetPivotToRight();
					}
				}

				// If necessary, trim the front
				if (mDrawStart != 0)
					processed = processed.Substring(mDrawStart, processed.Length - mDrawStart);
			}
			else
			{
				mDrawStart = 0;
				RestoreLabelPivot();
			}
		}

		label.text = processed;
#if MOBILE
		if (selected && (mKeyboard == null || inputShouldBeHidden))
#else
if (selected)
#endif
		{
			// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard
if (Input.compositionString == null)
Input.compositionString = "";

int start = mSelectionStart + Input.compositionString.Length - mDrawStart;
int end = mSelectionEnd + Input.compositionString.Length - mDrawStart;
#else
			int start = mSelectionStart - mDrawStart;
			int end = mSelectionEnd - mDrawStart;
#endif
			// ---------------------------------------------------------------

			// Blank texture used by selection and caret
			if (mBlankTex == null)
			{
				mBlankTex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				for (int y = 0; y < 2; ++y)
					for (int x = 0; x < 2; ++x)
						mBlankTex.SetPixel(x, y, Color.white);
				mBlankTex.Apply();
			}

			// Create the selection highlight
			if (start != end)
			{
				if (mHighlight == null)
				{
					mHighlight = NGUITools.AddWidget<UITexture>(label.cachedGameObject);
					mHighlight.name = "Input Highlight";
					mHighlight.mainTexture = mBlankTex;
					mHighlight.fillGeometry = false;
					mHighlight.pivot = label.pivot;
					mHighlight.SetAnchor(label.cachedTransform);
				}
				else
				{
					mHighlight.pivot = label.pivot;
					mHighlight.mainTexture = mBlankTex;
					mHighlight.MarkAsChanged();
					mHighlight.enabled = true;
				}
			}

			// Create the carter
			if (mCaret == null)
			{
				mCaret = NGUITools.AddWidget<UITexture>(label.cachedGameObject);
				mCaret.name = "Input Caret";
				mCaret.mainTexture = mBlankTex;
				mCaret.fillGeometry = false;
				mCaret.pivot = label.pivot;
				mCaret.SetAnchor(label.cachedTransform);
			}
			else
			{
				mCaret.pivot = label.pivot;
				mCaret.mainTexture = mBlankTex;
				mCaret.MarkAsChanged();
				mCaret.enabled = true;
			}

			if (start != end)
			{
				label.PrintOverlay(start, end, mCaret.geometry, mHighlight.geometry, caretColor, selectionColor);
				mHighlight.enabled = mHighlight.geometry.hasVertices;
			}
			else
			{
				label.PrintOverlay(start, end, mCaret.geometry, null, caretColor, selectionColor);
				if (mHighlight != null) mHighlight.enabled = false;
			}

			// Reset the blinking time
			mNextBlink = RealTime.time + 0.5f;
			mLastAlpha = label.finalAlpha;
		}
		else Cleanup();
	}
}

/// <summary>
/// Set the label's pivot to the left.
/// </summary>

protected void SetPivotToLeft ()
{
	Vector2 po = NGUIMath.GetPivotOffset(mPivot);
	po.x = 0f;
	label.pivot = NGUIMath.GetPivot(po);
}

/// <summary>
/// Set the label's pivot to the right.
/// </summary>

protected void SetPivotToRight ()
{
	Vector2 po = NGUIMath.GetPivotOffset(mPivot);
	po.x = 1f;
	label.pivot = NGUIMath.GetPivot(po);
}

/// <summary>
/// Restore the input label's pivot point.
/// </summary>

protected void RestoreLabelPivot ()
{
	if (label != null && label.pivot != mPivot)
		label.pivot = mPivot;
}

/// <summary>
/// Validate the specified input.
/// </summary>

protected char Validate (string text, int pos, char ch)
{
	// Validation is disabled
	if (validation == Validation.None || !enabled) return ch;

	if (validation == Validation.Integer)
	{
		// Integer number validation
		if (ch >= '0' && ch <= '9') return ch;
		if (ch == '-' && pos == 0 && !text.Contains("-")) return ch;
	}
	else if (validation == Validation.Float)
	{
		// Floating-point number
		if (ch >= '0' && ch <= '9') return ch;
		if (ch == '-' && pos == 0 && !text.Contains("-")) return ch;
		if (ch == '.' && !text.Contains(".")) return ch;
	}
	else if (validation == Validation.Alphanumeric)
	{
		// All alphanumeric characters
		if (ch >= 'A' && ch <= 'Z') return ch;
		if (ch >= 'a' && ch <= 'z') return ch;
		if (ch >= '0' && ch <= '9') return ch;
	}
	else if (validation == Validation.Username)
	{
		// Lowercase and numbers
		if (ch >= 'A' && ch <= 'Z') return (char)(ch - 'A' + 'a');
		if (ch >= 'a' && ch <= 'z') return ch;
		if (ch >= '0' && ch <= '9') return ch;
	}
	else if (validation == Validation.Name)
	{
		char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
		char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';

		if (ch >= 'a' && ch <= 'z')
		{
			// Space followed by a letter -- make sure it's capitalized
			if (lastChar == ' ') return (char)(ch - 'a' + 'A');
			return ch;
		}
		else if (ch >= 'A' && ch <= 'Z')
		{
			// Uppercase letters are only allowed after spaces (and apostrophes)
			if (lastChar != ' ' && lastChar != '\'') return (char)(ch - 'A' + 'a');
			return ch;
		}
		else if (ch == '\'')
		{
			// Don't allow more than one apostrophe
			if (lastChar != ' ' && lastChar != '\'' && nextChar != '\'' && !text.Contains("'")) return ch;
		}
		else if (ch == ' ')
		{
			// Don't allow more than one space in a row
			if (lastChar != ' ' && lastChar != '\'' && nextChar != ' ' && nextChar != '\'') return ch;
		}
	}
	return (char)0;
}

/// <summary>
/// Execute the OnChange callback.
/// </summary>

protected void ExecuteOnChange ()
{
	if (current == null && EventDelegate.IsValid(onChange))
	{
		current = this;
		EventDelegate.Execute(onChange);
		current = null;
	}
}

/// <summary>
/// Convenience function to be used as a callback that will clear the input field's focus.
/// </summary>

public void RemoveFocus () { isSelected = false; }

/// <summary>
/// Convenience function that can be used as a callback for On Change notification.
/// </summary>

public void SaveValue () { SaveToPlayerPrefs(mValue); }

/// <summary>
/// Convenience function that can forcefully reset the input field's value to what was saved earlier.
/// </summary>

public void LoadValue ()
{
	if (!string.IsNullOrEmpty(savedAs))
	{
		string val = mValue.Replace("\\n", "\n");
		mValue = "";
		value = PlayerPrefs.HasKey(savedAs) ? PlayerPrefs.GetString(savedAs) : val;
	}
}

// ---------------------------------------------------------------
#if MOBILE && UNITY_ANROID_KEYBOARD // USING_AndroidKeyboard

public void DeleteSelection()
{
string left = GetLeftText();
string right = GetRightText();
int rl = right.Length;

StringBuilder sb = new StringBuilder(left.Length + right.Length);

sb.Append(left);

// Advance the selection
mSelectionStart = sb.Length;
mSelectionEnd = mSelectionStart;

// Append the text that follows it, ensuring that it's also validated after the inserted value
for (int i = 0, imax = right.Length; i < imax; ++i)
{
char c = right[i];
if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
if (c != 0) sb.Append(c);
}

mValue = sb.ToString();
}


string compsitionstring = "";
string Compsitionstring
{
get { return compsitionstring; }
set
{
if (compsitionstring != value)
{
compsitionstring = value;
DeleteSelection();
}
}
}

void OnSelectEvent_Android()
{
if (keepKeyboardOn)
{
if(AndroidKeyboardManager.IsOpen())
return;
}

AndroidKeyboard.AdditionalOptions.selectAllTextOnFocus = selectAllTextOnFocus;

mSelectMe = -1;
mSelectionEnd = string.IsNullOrEmpty(mValue) ? 0 : mValue.Length;
mDrawStart = 0;
mSelectionStart = selectAllTextOnFocus ? 0 : mSelectionEnd;
label.color = activeTextColor;

string val;
TouchScreenKeyboardType kt;

if (inputShouldBeHidden)
{
TouchScreenKeyboard.hideInput = true;
kt = (TouchScreenKeyboardType)((int)keyboardType);
val = mValue;
}
else if (inputType == InputType.Password)
{
TouchScreenKeyboard.hideInput = false;
kt = TouchScreenKeyboardType.Default;
val = mValue;
mSelectionStart = mSelectionEnd;
}
else
{
TouchScreenKeyboard.hideInput = false;
kt = (TouchScreenKeyboardType)((int)keyboardType);
val = mValue;
mSelectionStart = mSelectionEnd;
}

mWaitForKeyboard = true;
Input.inputString = "";

StartCoroutine(OpenSoftKeyboard(val, kt));
}

System.Collections.IEnumerator OpenSoftKeyboard(string val, TouchScreenKeyboardType kt)
{
while (AndroidKeyboardManager.IsOpen())
{
yield return null;
}

if (mKeyboard == null || mKeyboard.active == false)
{
mKeyboard = TouchScreenKeyboard.Open(val, kt, !inputShouldBeHidden && inputType == InputType.AutoCorrect,
label.multiLine && !hideInput, inputType == InputType.Password, false, defaultText);

KeyboardMessageReceiver.instance.actionUpdate = Update_AndroidKeyboard;
KeyboardMessageReceiver.instance.actionCursorChanged = OnCursorChanged;

AndroidKeyboard.AdditionalOptions.keepKeyboardOn = this.keepKeyboardOn;

}

while (AndroidKeyboardManager.IsOpen() == false)
{
yield return null;
}

bool needTouchhandler = false;

#if UNITY_4
needTouchhandler = true;
#else
if(inputShouldBeHidden == false && AndroidKeyboard.AdditionalOptions.softInputMode == AndroidKeyboard.InputAdjustType.SOFT_INPUT_ADJUST_PAN)
needTouchhandler = true;
#endif

if(needTouchhandler)
{
UICamera.GetInputTouchCount = GetTouchCount;
UICamera.GetInputTouch = GetTouch;
}
else
{
UICamera.GetInputTouchCount = null;
UICamera.GetInputTouch = null;
}

KeyboardMessageReceiver.instance.onKeyboardClosed = OnKeyboardClosed;
}

void OnKeyboardClosed()
{
for(int i = 10; i >= 0; i--)
{
UICamera.RemoveTouch(i);
}

UICamera.GetInputTouchCount = null;
UICamera.GetInputTouch = null;
KeyboardMessageReceiver.instance.onKeyboardClosed = null;

ExecuteOnChange();
}

void Update_AndroidKeyboard(bool executeOnChange)
{
if(mKeyboard == null)
return;

string text = mKeyboard.text;

if (inputShouldBeHidden)
{
mCached = mKeyboard.text;
value = mKeyboard.text;

mSelectionStart = TouchScreenKeyboard.CursorPositionStart;
mSelectionEnd = TouchScreenKeyboard.CursorPositionEnd;
}
else if (mCached != text)
{
mCached = text;
value = text;
}

if (mKeyboard.done || !mKeyboard.active)
{
if (!mKeyboard.wasCanceled) Submit();
mKeyboard = null;
isSelected = false;
mCached = "";
return;
}

string ime = Input.compositionString;

// Append IME composition
if (mLastIME != ime)
{
//mSelectionEnd = string.IsNullOrEmpty(ime) ? mSelectionStart : mValue.Length + ime.Length;
mLastIME = ime;
UpdateLabel();
if (executeOnChange == false)
ExecuteOnChange();
}

if (executeOnChange == true)
ExecuteOnChange();

if (mKeyboard.active && AndroidKeyboard.AdditionalOptions.keepKeyboardOn && TouchScreenKeyboard.instance.OnReturnKey)
{
if (!mKeyboard.done && mKeyboard.active)
{
Submit();
isSelected = true;
}

TouchScreenKeyboard.instance.OnReturnKey = false;
}
}

public int GetTouchCount()
{
return AndroidTouch.instance.touchCount;
}

//yksachi -----------------------------------------------------------------------------
// yksachi : 현재버전 3.7.6 인데 plugin 버전은 3.8.0인 관계로
//public UICamera.Touch GetTouch(int index)
public UICamera.SettableTouch GetTouch(int index)
{
var srcTouch = AndroidTouch.instance.GetTouch(index);
return ConvertTouch(srcTouch);
}

//static UICamera.Touch ConvertTouch(AndroidTouch.Touch src)
static UICamera.SettableTouch ConvertTouch(AndroidTouch.Touch src)
{
//UICamera.Touch target = new UICamera.Touch();
UICamera.SettableTouch target = new UICamera.SettableTouch();
target.fingerId = src.fingerId;
target.phase = src.phase;
target.position = src.position;
target.tapCount = src.tapCount;

return target;
}
//yksachi -----------------------------------------------------------------------------

void OnApplicationPause(bool pauseStatus) 
{
if(pauseStatus)
{
AndroidKeyboardManager.CloseInCode();
OnKeyboardClosed();
AndroidKeyboard.Input.inputString = "";
AndroidKeyboard.Input.compositionString = "";
mKeyboard = null;
StopAllCoroutines();
}
}

void OnCursorChanged(int start, int end)
{
mSelectionStart = start;
mSelectionEnd = end;
UpdateLabel();
}

#endif

public void ClearText()
{
	this.value = "";
	mValue = value;
	mLoadSavedValue = false;

	mSelectionStart = 0;
	mSelectionEnd = 0;

#if MOBILE && UNITY_ANDROID
	AndroidKeyboard.Input.inputString = "";
	AndroidKeyboard.Input.compositionString = "";

	AndroidKeyboardManager.ClearText();
#endif

	UpdateLabel();

	label.text = "";
}
}

#else // #if !UNITY_IOS

using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Input field makes it possible to enter custom information within the UI.
/// </summary>

[AddComponentMenu("NGUI/UI/Input Field")]
public class UIInput : MonoBehaviour
{
public enum InputType
{
Standard,
AutoCorrect,
Password,
}

public enum Validation
{
None,
Integer,
Float,
Alphanumeric,
Username,
Name,
}

public enum KeyboardType
{
Default = 0,
ASCIICapable = 1,
NumbersAndPunctuation = 2,
URL = 3,
NumberPad = 4,
PhonePad = 5,
NamePhonePad = 6,
EmailAddress = 7,
}

public enum OnReturnKey
{
Default,
Submit,
NewLine,
}

public delegate char OnValidate (string text, int charIndex, char addedChar);

/// <summary>
/// Currently active input field. Only valid during callbacks.
/// </summary>

static public UIInput current;

/// <summary>
/// Currently selected input field, if any.
/// </summary>

static public UIInput selection;

/// <summary>
/// Text label used to display the input's value.
/// </summary>

public UILabel label;

/// <summary>
/// Type of data expected by the input field.
/// </summary>

public InputType inputType = InputType.Standard;

/// <summary>
/// What to do when the Return key is pressed on the keyboard.
/// </summary>

public OnReturnKey onReturnKey = OnReturnKey.Default;

/// <summary>
/// Keyboard type applies to mobile keyboards that get shown.
/// </summary>

public KeyboardType keyboardType = KeyboardType.Default;

/// <summary>
/// Whether the input will be hidden on mobile platforms.
/// </summary>

public bool hideInput = false;

/// <summary>
/// Whether all text will be selected when the input field gains focus.
/// </summary>

[System.NonSerialized]
public bool selectAllTextOnFocus = true;

/// <summary>
/// What kind of validation to use with the input field's data.
/// </summary>

public Validation validation = Validation.None;

/// <summary>
/// Maximum number of characters allowed before input no longer works.
/// </summary>

public int characterLimit = 0;

/// <summary>
/// Field in player prefs used to automatically save the value.
/// </summary>

public string savedAs;

/// <summary>
/// Don't use this anymore. Attach UIKeyNavigation instead.
/// </summary>

[HideInInspector][SerializeField] GameObject selectOnTab;

/// <summary>
/// Color of the label when the input field has focus.
/// </summary>

public Color activeTextColor = Color.white;

/// <summary>
/// Color used by the caret symbol.
/// </summary>

public Color caretColor = new Color(1f, 1f, 1f, 0.8f);

/// <summary>
/// Color used by the selection rectangle.
/// </summary>

public Color selectionColor = new Color(1f, 223f / 255f, 141f / 255f, 0.5f);

/// <summary>
/// Event delegates triggered when the input field submits its data.
/// </summary>

public List<EventDelegate> onSubmit = new List<EventDelegate>();

/// <summary>
/// Event delegates triggered when the input field's text changes for any reason.
/// </summary>

public List<EventDelegate> onChange = new List<EventDelegate>();

/// <summary>
/// Custom validation callback.
/// </summary>

public OnValidate onValidate;

/// <summary>
/// Input field's value.
/// </summary>

[SerializeField][HideInInspector] protected string mValue;

[System.NonSerialized] protected string mDefaultText = "";
[System.NonSerialized] protected Color mDefaultColor = Color.white;
[System.NonSerialized] protected float mPosition = 0f;
[System.NonSerialized] protected bool mDoInit = true;
[System.NonSerialized] protected UIWidget.Pivot mPivot = UIWidget.Pivot.TopLeft;
[System.NonSerialized] protected bool mLoadSavedValue = true;

static protected int mDrawStart = 0;
static protected string mLastIME = "";

#if MOBILE
// Unity fails to compile if the touch screen keyboard is used on a non-mobile device
static protected TouchScreenKeyboard mKeyboard;
static bool mWaitForKeyboard = false;
#endif
[System.NonSerialized] protected int mSelectionStart = 0;
[System.NonSerialized] protected int mSelectionEnd = 0;
[System.NonSerialized] protected UITexture mHighlight = null;
[System.NonSerialized] protected UITexture mCaret = null;
[System.NonSerialized] protected Texture2D mBlankTex = null;
[System.NonSerialized] protected float mNextBlink = 0f;
[System.NonSerialized] protected float mLastAlpha = 0f;
[System.NonSerialized] protected string mCached = "";
[System.NonSerialized] protected int mSelectMe = -1;

/// <summary>
/// Default text used by the input's label.
/// </summary>

public string defaultText
{
	get
	{
		if (mDoInit) Init();
		return mDefaultText;
	}
	set
	{
		if (mDoInit) Init();
		mDefaultText = value;
		UpdateLabel();
	}
}

/// <summary>
/// Should the input be hidden?
/// </summary>

public bool inputShouldBeHidden
{
	get
	{
#if UNITY_METRO
return true;
#else
		return hideInput && label != null && !label.multiLine && inputType != InputType.Password;
#endif
	}
}

[System.Obsolete("Use UIInput.value instead")]
public string text { get { return this.value; } set { this.value = value; } }

/// <summary>
/// Input field's current text value.
/// </summary>

public string value
{
	get
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return "";
#endif
		if (mDoInit) Init();
		return mValue;
	}
	set
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (mDoInit) Init();
		mDrawStart = 0;

		// BB10's implementation has a bug in Unity
#if UNITY_4_3
if (Application.platform == RuntimePlatform.BB10Player)
#else
		if (Application.platform == RuntimePlatform.BlackBerryPlayer)
#endif
		value = value.Replace("\\b", "\b");

		// Validate all input
		value = Validate(value);
#if MOBILE
		if (isSelected && mKeyboard != null && mCached != value)
		{
			mKeyboard.text = value;
			mCached = value;
		}
#endif
		if (mValue != value)
		{
			mValue = value;
			mLoadSavedValue = false;

			if (isSelected)
			{
				if (string.IsNullOrEmpty(value))
				{
					mSelectionStart = 0;
					mSelectionEnd = 0;
				}
				else
				{
					mSelectionStart = value.Length;
					mSelectionEnd = mSelectionStart;
				}
			}
			else SaveToPlayerPrefs(value);

			UpdateLabel();
			ExecuteOnChange();
		}
	}
}

[System.Obsolete("Use UIInput.isSelected instead")]
public bool selected { get { return isSelected; } set { isSelected = value; } }

/// <summary>
/// Whether the input is currently selected.
/// </summary>

public bool isSelected
{
	get
	{
		return selection == this;
	}
	set
	{
		if (!value) { if (isSelected) UICamera.selectedObject = null; }
		else UICamera.selectedObject = gameObject;
	}
}

/// <summary>
/// Current position of the cursor.
/// </summary>

public int cursorPosition
{
	get
	{
#if MOBILE
		if (mKeyboard != null && !inputShouldBeHidden) return value.Length;
#endif
		return isSelected ? mSelectionEnd : value.Length;
	}
	set
	{
		if (isSelected)
		{
#if MOBILE
			if (mKeyboard != null && !inputShouldBeHidden) return;
#endif
			mSelectionEnd = value;
			UpdateLabel();
		}
	}
}

/// <summary>
/// Index of the character where selection begins.
/// </summary>

public int selectionStart
{
	get
	{
#if MOBILE
		if (mKeyboard != null && !inputShouldBeHidden) return 0;
#endif
		return isSelected ? mSelectionStart : value.Length;
	}
	set
	{
		if (isSelected)
		{
#if MOBILE
			if (mKeyboard != null && !inputShouldBeHidden) return;
#endif
			mSelectionStart = value;
			UpdateLabel();
		}
	}
}

/// <summary>
/// Index of the character where selection ends.
/// </summary>

public int selectionEnd
{
	get
	{
#if MOBILE
		if (mKeyboard != null && !inputShouldBeHidden) return value.Length;
#endif
		return isSelected ? mSelectionEnd : value.Length;
	}
	set
	{
		if (isSelected)
		{
#if MOBILE
			if (mKeyboard != null && !inputShouldBeHidden) return;
#endif
			mSelectionEnd = value;
			UpdateLabel();
		}
	}
}

/// <summary>
/// Caret, in case it's needed.
/// </summary>

public UITexture caret { get { return mCaret; } }

/// <summary>
/// Validate the specified text, returning the validated version.
/// </summary>

public string Validate (string val)
{
	if (string.IsNullOrEmpty(val)) return "";

	StringBuilder sb = new StringBuilder(val.Length);

	for (int i = 0; i < val.Length; ++i)
	{
		char c = val[i];
		if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
		else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
		if (c != 0) sb.Append(c);
	}

	if (characterLimit > 0 && sb.Length > characterLimit)
		return sb.ToString(0, characterLimit);
	return sb.ToString();
}

/// <summary>
/// Automatically set the value by loading it from player prefs if possible.
/// </summary>

void Start ()
{
	if (selectOnTab != null)
	{
		UIKeyNavigation nav = GetComponent<UIKeyNavigation>();

		if (nav == null)
		{
			nav = gameObject.AddComponent<UIKeyNavigation>();
			nav.onDown = selectOnTab;
		}
		selectOnTab = null;
		NGUITools.SetDirty(this);
	}

	if (mLoadSavedValue && !string.IsNullOrEmpty(savedAs)) LoadValue();
	else value = mValue.Replace("\\n", "\n");
}

/// <summary>
/// Labels used for input shouldn't support rich text.
/// </summary>

protected void Init ()
{
	if (mDoInit && label != null)
	{
		mDoInit = false;
		mDefaultText = label.text;
		mDefaultColor = label.color;
		label.supportEncoding = false;

		if (label.alignment == NGUIText.Alignment.Justified)
		{
			label.alignment = NGUIText.Alignment.Left;
			Debug.LogWarning("Input fields using labels with justified alignment are not supported at this time", this);
		}

		mPivot = label.pivot;
		mPosition = label.cachedTransform.localPosition.x;
		UpdateLabel();
	}
}

/// <summary>
/// Save the specified value to player prefs.
/// </summary>

protected void SaveToPlayerPrefs (string val)
{
	if (!string.IsNullOrEmpty(savedAs))
	{
		if (string.IsNullOrEmpty(val)) PlayerPrefs.DeleteKey(savedAs);
		else PlayerPrefs.SetString(savedAs, val);
	}
}

#if !MOBILE
[System.NonSerialized] UIInputOnGUI mOnGUI;
#endif
/// <summary>
/// Selection event, sent by the EventSystem.
/// </summary>

protected virtual void OnSelect (bool isSelected)
{
	if (isSelected)
	{
#if !MOBILE
if (mOnGUI == null)
mOnGUI = gameObject.AddComponent<UIInputOnGUI>();
#endif
		OnSelectEvent();
	}
	else
	{
#if !MOBILE
if (mOnGUI != null)
{
Destroy(mOnGUI);
mOnGUI = null;
}
#endif
		OnDeselectEvent();
	}
}

/// <summary>
/// Notification of the input field gaining selection.
/// </summary>

protected void OnSelectEvent ()
{
	selection = this;
	if (mDoInit) Init();

	// Unity has issues bringing up the keyboard properly if it's in "hideInput" mode and you happen
	// to select one input in the same Update as de-selecting another.
	if (label != null && NGUITools.GetActive(this)) mSelectMe = Time.frameCount;
}

/// <summary>
/// Notification of the input field losing selection.
/// </summary>

protected void OnDeselectEvent ()
{
	if (mDoInit) Init();

	if (label != null && NGUITools.GetActive(this))
	{
		mValue = value;
#if MOBILE
		if (mKeyboard != null)
		{
			mWaitForKeyboard = false;
			mKeyboard.active = false;
			mKeyboard = null;
		}
#endif
		if (string.IsNullOrEmpty(mValue))
		{
			label.text = mDefaultText;
			label.color = mDefaultColor;
		}
		else label.text = mValue;

		Input.imeCompositionMode = IMECompositionMode.Auto;
		RestoreLabelPivot();
	}

	selection = null;
	UpdateLabel();
}

/// <summary>
/// Update the text based on input.
/// </summary>

protected virtual void Update ()
{
#if UNITY_EDITOR
	if (!Application.isPlaying) return;
#endif
	if (isSelected)
	{
		if (mDoInit) Init();
#if MOBILE
		// Wait for the keyboard to open. Apparently mKeyboard.active will return 'false' for a while in some cases.
		if (mWaitForKeyboard)
		{
			if (mKeyboard != null && !mKeyboard.active) return;
			mWaitForKeyboard = false;
		}
#endif
		// Unity has issues bringing up the keyboard properly if it's in "hideInput" mode and you happen
		// to select one input in the same Update as de-selecting another.
		if (mSelectMe != -1 && mSelectMe != Time.frameCount)
		{
			mSelectMe = -1;
			mSelectionEnd = string.IsNullOrEmpty(mValue) ? 0 : mValue.Length;
			mDrawStart = 0;
			mSelectionStart = selectAllTextOnFocus ? 0 : mSelectionEnd;
			label.color = activeTextColor;
#if MOBILE
			if (Application.platform == RuntimePlatform.IPhonePlayer
			        || Application.platform == RuntimePlatform.Android
			        || Application.platform == RuntimePlatform.WP8Player
#if UNITY_4_3
|| Application.platform == RuntimePlatform.BB10Player
#else
			        || Application.platform == RuntimePlatform.BlackBerryPlayer
			        || Application.platform == RuntimePlatform.MetroPlayerARM
			        || Application.platform == RuntimePlatform.MetroPlayerX64
			        || Application.platform == RuntimePlatform.MetroPlayerX86
#endif
			       )
			{
				string val;
				TouchScreenKeyboardType kt;

				if (inputShouldBeHidden)
				{
					TouchScreenKeyboard.hideInput = true;
					kt = (TouchScreenKeyboardType)((int)keyboardType);
#if UNITY_METRO
val = "";
#else
					val = "|";
#endif
				}
				else if (inputType == InputType.Password)
				{
					TouchScreenKeyboard.hideInput = false;
					kt = TouchScreenKeyboardType.Default;
					val = mValue;
					mSelectionStart = mSelectionEnd;
				}
				else
				{
					TouchScreenKeyboard.hideInput = false;
					kt = (TouchScreenKeyboardType)((int)keyboardType);
					val = mValue;
					mSelectionStart = mSelectionEnd;
				}

				mWaitForKeyboard = true;
				mKeyboard = (inputType == InputType.Password) ?
					TouchScreenKeyboard.Open(val, kt, false, false, true) :
					TouchScreenKeyboard.Open(val, kt, !inputShouldBeHidden && inputType == InputType.AutoCorrect,
					                             label.multiLine && !hideInput, false, false, defaultText);
#if UNITY_METRO
mKeyboard.active = true;
#endif
			}
			else
#endif // MOBILE
			{
				Vector2 pos = (UICamera.current != null && UICamera.current.cachedCamera != null) ?
					UICamera.current.cachedCamera.WorldToScreenPoint(label.worldCorners[0]) :
					label.worldCorners[0];
				pos.y = Screen.height - pos.y;
				Input.imeCompositionMode = IMECompositionMode.On;
				Input.compositionCursorPos = pos;
			}

			UpdateLabel();
			if (string.IsNullOrEmpty(Input.inputString)) return;
		}
#if MOBILE
		if (mKeyboard != null)
		{
#if UNITY_METRO
string text = Input.inputString;
if (!string.IsNullOrEmpty(text)) Insert(text);
#else
			string text = mKeyboard.text;

			if (inputShouldBeHidden)
			{
				if (text != "|")
				{
					if (!string.IsNullOrEmpty(text))
					{
						Insert(text.Substring(1));
					}
					else DoBackspace();

					mKeyboard.text = "|";
				}
			}
			else if (mCached != text)
			{
				mCached = text;
				value = text;
			}
#endif // UNITY_METRO
			if (mKeyboard.done || !mKeyboard.active)
			{
				if (!mKeyboard.wasCanceled) Submit();
				mKeyboard = null;
				isSelected = false;
				mCached = "";
			}
		}
		else
#endif // MOBILE
		{
			string ime = Input.compositionString;

			// There seems to be an inconsistency between IME on Windows, and IME on OSX.
			// On Windows, Input.inputString is always empty while IME is active. On the OSX it is not.

			// Unity NGUI 4.3 UIInput에서 한글짤림현상수정 by.moonjh 2015.04.23
			//if (string.IsNullOrEmpty(ime) && !string.IsNullOrEmpty(Input.inputString))
			if (!string.IsNullOrEmpty(Input.inputString)) 
			{
				// Process input ignoring non-printable characters as they are not consistent.
				// Windows has them, OSX may not. They get handled inside OnGUI() instead.
				string s = Input.inputString;

				for (int i = 0; i < s.Length; ++i)
				{
					char ch = s[i];
					if (ch < ' ') continue;

					// OSX inserts these characters for arrow keys
					if (ch == '\uF700') continue;
					if (ch == '\uF701') continue;
					if (ch == '\uF702') continue;
					if (ch == '\uF703') continue;

					Insert(ch.ToString());
				}
			}

			// Append IME composition
			if (mLastIME != ime)
			{
				mSelectionEnd = string.IsNullOrEmpty(ime) ? mSelectionStart : mValue.Length + ime.Length;
				mLastIME = ime;
				UpdateLabel();
				ExecuteOnChange();
			}
		}

		// Blink the caret
		if (mCaret != null && mNextBlink < RealTime.time)
		{
			mNextBlink = RealTime.time + 0.5f;
			mCaret.enabled = !mCaret.enabled;
		}

		// If the label's final alpha changes, we need to update the drawn geometry,
		// or the highlight widgets (which have their geometry set manually) won't update.
		if (isSelected && mLastAlpha != label.finalAlpha)
			UpdateLabel();

		// Having this in OnGUI causes issues because Input.inputString gets updated *after* OnGUI, apparently...
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			bool newLine = (onReturnKey == OnReturnKey.NewLine) ||
				(onReturnKey == OnReturnKey.Default &&
				     label.multiLine && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
				     label.overflowMethod != UILabel.Overflow.ClampContent &&
				     validation == Validation.None);

			if (newLine)
			{
				Insert("\n");
			}
			else
			{
				UICamera.currentScheme = UICamera.ControlScheme.Controller;
				UICamera.currentKey = KeyCode.Return;
				Submit();
				UICamera.currentKey = KeyCode.None;
			}
		}
	}
}

/// <summary>
/// Perform a backspace operation.
/// </summary>

protected void DoBackspace ()
{
	if (!string.IsNullOrEmpty(mValue))
	{
		if (mSelectionStart == mSelectionEnd)
		{
			if (mSelectionStart < 1) return;
			--mSelectionEnd;
		}
		Insert("");
	}
}

#if !MOBILE
/// <summary>
/// Handle the specified event.
/// </summary>

public virtual bool ProcessEvent (Event ev)
{
if (label == null) return false;

RuntimePlatform rp = Application.platform;

bool isMac = (
rp == RuntimePlatform.OSXEditor ||
rp == RuntimePlatform.OSXPlayer ||
rp == RuntimePlatform.OSXWebPlayer);

bool ctrl = isMac ?
((ev.modifiers & EventModifiers.Command) != 0) :
((ev.modifiers & EventModifiers.Control) != 0);

// http://www.tasharen.com/forum/index.php?topic=10780.0
if ((ev.modifiers & EventModifiers.Alt) != 0) ctrl = false;

bool shift = ((ev.modifiers & EventModifiers.Shift) != 0);

switch (ev.keyCode)
{
case KeyCode.Backspace:
{
ev.Use();
DoBackspace();
return true;
}

case KeyCode.Delete:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
if (mSelectionStart == mSelectionEnd)
{
if (mSelectionStart >= mValue.Length) return true;
++mSelectionEnd;
}
Insert("");
}
return true;
}

case KeyCode.LeftArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = Mathf.Max(mSelectionEnd - 1, 0);
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.RightArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = Mathf.Min(mSelectionEnd + 1, mValue.Length);
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.PageUp:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = 0;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.PageDown:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = mValue.Length;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.Home:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
if (label.multiLine)
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.Home);
}
else mSelectionEnd = 0;

if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.End:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
if (label.multiLine)
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.End);
}
else mSelectionEnd = mValue.Length;

if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.UpArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.UpArrow);
if (mSelectionEnd != 0) mSelectionEnd += mDrawStart;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

case KeyCode.DownArrow:
{
ev.Use();

if (!string.IsNullOrEmpty(mValue))
{
mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.DownArrow);
if (mSelectionEnd != label.processedText.Length) mSelectionEnd += mDrawStart;
else mSelectionEnd = mValue.Length;
if (!shift) mSelectionStart = mSelectionEnd;
UpdateLabel();
}
return true;
}

// Select all
case KeyCode.A:
{
if (ctrl)
{
ev.Use();
mSelectionStart = 0;
mSelectionEnd = mValue.Length;
UpdateLabel();
}
return true;
}

// Copy
case KeyCode.C:
{
if (ctrl)
{
ev.Use();
NGUITools.clipboard = GetSelection();
}
return true;
}

// Paste
case KeyCode.V:
{
if (ctrl)
{
ev.Use();
Insert(NGUITools.clipboard);
}
return true;
}

// Cut
case KeyCode.X:
{
if (ctrl)
{
ev.Use();
NGUITools.clipboard = GetSelection();
Insert("");
}
return true;
}
}
return false;
}
#endif

/// <summary>
/// Insert the specified text string into the current input value, respecting selection and validation.
/// </summary>

protected virtual void Insert (string text)
{
	string left = GetLeftText();
	string right = GetRightText();
	int rl = right.Length;

	StringBuilder sb = new StringBuilder(left.Length + right.Length + text.Length);
	sb.Append(left);

	// Append the new text
	for (int i = 0, imax = text.Length; i < imax; ++i)
	{
		// If we have an input validator, validate the input first
		char c = text[i];

		if (c == '\b')
		{
			DoBackspace();
			continue;
		}

		// Can't go past the character limit
		if (characterLimit > 0 && sb.Length + rl >= characterLimit) break;

		if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
		else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);

		// Append the character if it hasn't been invalidated
		if (c != 0) sb.Append(c);
	}

	// Advance the selection
	mSelectionStart = sb.Length;
	mSelectionEnd = mSelectionStart;

	// Append the text that follows it, ensuring that it's also validated after the inserted value
	for (int i = 0, imax = right.Length; i < imax; ++i)
	{
		char c = right[i];
		if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
		else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
		if (c != 0) sb.Append(c);
	}

	mValue = sb.ToString();
	UpdateLabel();
	ExecuteOnChange();
}

/// <summary>
/// Get the text to the left of the selection.
/// </summary>

protected string GetLeftText ()
{
	int min = Mathf.Min(mSelectionStart, mSelectionEnd);
	return (string.IsNullOrEmpty(mValue) || min < 0) ? "" : mValue.Substring(0, min);
}

/// <summary>
/// Get the text to the right of the selection.
/// </summary>

protected string GetRightText ()
{
	int max = Mathf.Max(mSelectionStart, mSelectionEnd);
	return (string.IsNullOrEmpty(mValue) || max >= mValue.Length) ? "" : mValue.Substring(max);
}

/// <summary>
/// Get currently selected text.
/// </summary>

protected string GetSelection ()
{
	if (string.IsNullOrEmpty(mValue) || mSelectionStart == mSelectionEnd)
	{
		return "";
	}
	else
	{
		int min = Mathf.Min(mSelectionStart, mSelectionEnd);
		int max = Mathf.Max(mSelectionStart, mSelectionEnd);
		return mValue.Substring(min, max - min);
	}
}

/// <summary>
/// Helper function that retrieves the index of the character under the mouse.
/// </summary>

protected int GetCharUnderMouse ()
{
	Vector3[] corners = label.worldCorners;
	Ray ray = UICamera.currentRay;
	Plane p = new Plane(corners[0], corners[1], corners[2]);
	float dist;
	return p.Raycast(ray, out dist) ? mDrawStart + label.GetCharacterIndexAtPosition(ray.GetPoint(dist), false) : 0;
}

/// <summary>
/// Move the caret on press.
/// </summary>

protected virtual void OnPress (bool isPressed)
{
	if (isPressed && isSelected && label != null &&
	        (UICamera.currentScheme == UICamera.ControlScheme.Mouse ||
	         UICamera.currentScheme == UICamera.ControlScheme.Touch))
	{
#if !UNITY_EDITOR && (UNITY_WP8 || UNITY_WP_8_1)
if (mKeyboard != null) mKeyboard.active = true;
#endif
		selectionEnd = GetCharUnderMouse();
		if (!Input.GetKey(KeyCode.LeftShift) &&
		        !Input.GetKey(KeyCode.RightShift)) selectionStart = mSelectionEnd;
	}
}

/// <summary>
/// Drag selection.
/// </summary>

protected virtual void OnDrag (Vector2 delta)
{
	if (label != null &&
	        (UICamera.currentScheme == UICamera.ControlScheme.Mouse ||
	         UICamera.currentScheme == UICamera.ControlScheme.Touch))
	{
		selectionEnd = GetCharUnderMouse();
	}
}

/// <summary>
/// Ensure we've released the dynamically created resources.
/// </summary>

void OnDisable () { Cleanup(); }

/// <summary>
/// Cleanup.
/// </summary>

protected virtual void Cleanup ()
{
	if (mHighlight) mHighlight.enabled = false;
	if (mCaret) mCaret.enabled = false;

	if (mBlankTex)
	{
		NGUITools.Destroy(mBlankTex);
		mBlankTex = null;
	}
}

/// <summary>
/// Submit the input field's text.
/// </summary>

public void Submit ()
{
	if (NGUITools.GetActive(this))
	{
		mValue = value;

		if (current == null)
		{
			current = this;
			EventDelegate.Execute(onSubmit);
			current = null;
		}
		SaveToPlayerPrefs(mValue);
	}
}

/// <summary>
/// Update the visual text label.
/// </summary>

public void UpdateLabel ()
{
	if (label != null)
	{
		if (mDoInit) Init();
		bool selected = isSelected;
		string fullText = value;
		bool isEmpty = string.IsNullOrEmpty(fullText) && string.IsNullOrEmpty(Input.compositionString);
		label.color = (isEmpty && !selected) ? mDefaultColor : activeTextColor;
		string processed;

		if (isEmpty)
		{
			processed = selected ? "" : mDefaultText;
			RestoreLabelPivot();
		}
		else
		{
			if (inputType == InputType.Password)
			{
				processed = "";

				string asterisk = "*";

				if (label.bitmapFont != null && label.bitmapFont.bmFont != null &&
				        label.bitmapFont.bmFont.GetGlyph('*') == null) asterisk = "x";

				for (int i = 0, imax = fullText.Length; i < imax; ++i) processed += asterisk;
			}
			else processed = fullText;

			// Start with text leading up to the selection
			int selPos = selected ? Mathf.Min(processed.Length, cursorPosition) : 0;
			string left = processed.Substring(0, selPos);

			// Append the composition string and the cursor character
			if (selected) left += Input.compositionString;

			// Append the text from the selection onwards
			processed = left + processed.Substring(selPos, processed.Length - selPos);

			// Clamped content needs to be adjusted further
			if (selected && label.overflowMethod == UILabel.Overflow.ClampContent && label.maxLineCount == 1)
			{
				// Determine what will actually fit into the given line
				int offset = label.CalculateOffsetToFit(processed);

				if (offset == 0)
				{
					mDrawStart = 0;
					RestoreLabelPivot();
				}
				else if (selPos < mDrawStart)
				{
					mDrawStart = selPos;
					SetPivotToLeft();
				}
				else if (offset < mDrawStart)
				{
					mDrawStart = offset;
					SetPivotToLeft();
				}
				else
				{
					offset = label.CalculateOffsetToFit(processed.Substring(0, selPos));

					if (offset > mDrawStart)
					{
						mDrawStart = offset;
						SetPivotToRight();
					}
				}

				// If necessary, trim the front
				if (mDrawStart != 0)
					processed = processed.Substring(mDrawStart, processed.Length - mDrawStart);
			}
			else
			{
				mDrawStart = 0;
				RestoreLabelPivot();
			}
		}

		label.text = processed;
#if MOBILE
		if (selected && (mKeyboard == null || inputShouldBeHidden))
#else
if (selected)
#endif
		{
			int start = mSelectionStart - mDrawStart;
			int end = mSelectionEnd - mDrawStart;

			// Blank texture used by selection and caret
			if (mBlankTex == null)
			{
				mBlankTex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				for (int y = 0; y < 2; ++y)
					for (int x = 0; x < 2; ++x)
						mBlankTex.SetPixel(x, y, Color.white);
				mBlankTex.Apply();
			}

			// Create the selection highlight
			if (start != end)
			{
				if (mHighlight == null)
				{
					mHighlight = NGUITools.AddWidget<UITexture>(label.cachedGameObject);
					mHighlight.name = "Input Highlight";
					mHighlight.mainTexture = mBlankTex;
					mHighlight.fillGeometry = false;
					mHighlight.pivot = label.pivot;
					mHighlight.SetAnchor(label.cachedTransform);
				}
				else
				{
					mHighlight.pivot = label.pivot;
					mHighlight.mainTexture = mBlankTex;
					mHighlight.MarkAsChanged();
					mHighlight.enabled = true;
				}
			}

			// Create the carter
			if (mCaret == null)
			{
				mCaret = NGUITools.AddWidget<UITexture>(label.cachedGameObject);
				mCaret.name = "Input Caret";
				mCaret.mainTexture = mBlankTex;
				mCaret.fillGeometry = false;
				mCaret.pivot = label.pivot;
				mCaret.SetAnchor(label.cachedTransform);
			}
			else
			{
				mCaret.pivot = label.pivot;
				mCaret.mainTexture = mBlankTex;
				mCaret.MarkAsChanged();
				mCaret.enabled = true;
			}

			if (start != end)
			{
				label.PrintOverlay(start, end, mCaret.geometry, mHighlight.geometry, caretColor, selectionColor);
				mHighlight.enabled = mHighlight.geometry.hasVertices;
			}
			else
			{
				label.PrintOverlay(start, end, mCaret.geometry, null, caretColor, selectionColor);
				if (mHighlight != null) mHighlight.enabled = false;
			}

			// Reset the blinking time
			mNextBlink = RealTime.time + 0.5f;
			mLastAlpha = label.finalAlpha;
		}
		else Cleanup();
	}
}

/// <summary>
/// Set the label's pivot to the left.
/// </summary>

protected void SetPivotToLeft ()
{
	Vector2 po = NGUIMath.GetPivotOffset(mPivot);
	po.x = 0f;
	label.pivot = NGUIMath.GetPivot(po);
}

/// <summary>
/// Set the label's pivot to the right.
/// </summary>

protected void SetPivotToRight ()
{
	Vector2 po = NGUIMath.GetPivotOffset(mPivot);
	po.x = 1f;
	label.pivot = NGUIMath.GetPivot(po);
}

/// <summary>
/// Restore the input label's pivot point.
/// </summary>

protected void RestoreLabelPivot ()
{
	if (label != null && label.pivot != mPivot)
		label.pivot = mPivot;
}

/// <summary>
/// Validate the specified input.
/// </summary>

protected char Validate (string text, int pos, char ch)
{
	// Validation is disabled
	if (validation == Validation.None || !enabled) return ch;

	if (validation == Validation.Integer)
	{
		// Integer number validation
		if (ch >= '0' && ch <= '9') return ch;
		if (ch == '-' && pos == 0 && !text.Contains("-")) return ch;
	}
	else if (validation == Validation.Float)
	{
		// Floating-point number
		if (ch >= '0' && ch <= '9') return ch;
		if (ch == '-' && pos == 0 && !text.Contains("-")) return ch;
		if (ch == '.' && !text.Contains(".")) return ch;
	}
	else if (validation == Validation.Alphanumeric)
	{
		// All alphanumeric characters
		if (ch >= 'A' && ch <= 'Z') return ch;
		if (ch >= 'a' && ch <= 'z') return ch;
		if (ch >= '0' && ch <= '9') return ch;
	}
	else if (validation == Validation.Username)
	{
		// Lowercase and numbers
		if (ch >= 'A' && ch <= 'Z') return (char)(ch - 'A' + 'a');
		if (ch >= 'a' && ch <= 'z') return ch;
		if (ch >= '0' && ch <= '9') return ch;
	}
	else if (validation == Validation.Name)
	{
		char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
		char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';

		if (ch >= 'a' && ch <= 'z')
		{
			// Space followed by a letter -- make sure it's capitalized
			if (lastChar == ' ') return (char)(ch - 'a' + 'A');
			return ch;
		}
		else if (ch >= 'A' && ch <= 'Z')
		{
			// Uppercase letters are only allowed after spaces (and apostrophes)
			if (lastChar != ' ' && lastChar != '\'') return (char)(ch - 'A' + 'a');
			return ch;
		}
		else if (ch == '\'')
		{
			// Don't allow more than one apostrophe
			if (lastChar != ' ' && lastChar != '\'' && nextChar != '\'' && !text.Contains("'")) return ch;
		}
		else if (ch == ' ')
		{
			// Don't allow more than one space in a row
			if (lastChar != ' ' && lastChar != '\'' && nextChar != ' ' && nextChar != '\'') return ch;
		}
	}
	return (char)0;
}

/// <summary>
/// Execute the OnChange callback.
/// </summary>

protected void ExecuteOnChange ()
{
	if (current == null && EventDelegate.IsValid(onChange))
	{
		current = this;
		EventDelegate.Execute(onChange);
		current = null;
	}
}

/// <summary>
/// Convenience function to be used as a callback that will clear the input field's focus.
/// </summary>

public void RemoveFocus () { isSelected = false; }

/// <summary>
/// Convenience function that can be used as a callback for On Change notification.
/// </summary>

public void SaveValue () { SaveToPlayerPrefs(mValue); }

/// <summary>
/// Convenience function that can forcefully reset the input field's value to what was saved earlier.
/// </summary>

public void LoadValue ()
{
	if (!string.IsNullOrEmpty(savedAs))
	{
		string val = mValue.Replace("\\n", "\n");
		mValue = "";
		value = PlayerPrefs.HasKey(savedAs) ? PlayerPrefs.GetString(savedAs) : val;
	}
}
}

#endif