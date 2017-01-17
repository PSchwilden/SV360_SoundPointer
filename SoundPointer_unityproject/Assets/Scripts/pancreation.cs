using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityOSC;
using System.Linq;


//Pan cursor creation and management class
public class pancreation : MonoBehaviour {

	public InputField trackInput; //Track number input field
	public InputField FXInput; //FX position number input field
	public Text rawAngl; //Raw angles monitoring text
	public Text calAngl; //Calculated angles monitoring text
	public Toggle xyz; //Cartesian values toggle
	public Toggle aziele; //Spherical values toggle

	public GameObject togglePrefab, cursorPrefab; //Prefabs for the cursor and toggle
	public Transform toggleParent, cursorParent; //Parents for the cursors and toggles
	private int number = 0; //Number to identify each cursor at creation
	public ToggleGroup toggleGroup; //Toggle group to have only one toggle active
	public Dictionary<int, int> trackNumbers = new Dictionary<int, int> (); //Dictionary to link track numbers to corresponding cursors

	public int maxYaw, maxPitch; //Max panning yaw and pitch
	public InputField pitchinput, yawinput; //Input fields for max yaw and max pitch

	private Vector3 camPos;


	// Use this for initialization
	void Start () {

		//Start OSC manager script
		OSCHandler.Instance.Init ();

		//Set Default values for max pitch and max yaw
		yawinput.text = "180";
		pitchinput.text = "180";
		maxYaw = 180;
		maxPitch = 180;


	}
	
	// Update is called once per frame
	void Update () {
		//Update OSC logs
		OSCHandler.Instance.UpdateLogs ();

		//Manage camera position and size for different resolutions
		camPos.x = Screen.width/2;
		camPos.y = Screen.height/2;
		camPos.z = -500;
		Camera.main.orthographicSize = Screen.height / 2;
		Camera.main.transform.position = camPos;
	}

	//Creation of a new pan cursor
	public void CreateNewPan (){
		//Instantiate a new toggle
		GameObject toggle = Instantiate (togglePrefab);
		toggle.transform.SetParent (toggleParent);
		//Instantiate a new cursor
		GameObject cursor = Instantiate (cursorPrefab);
		cursor.transform.SetParent (cursorParent);

		//Place the toggle on the screen
		toggle.transform.localPosition = new Vector3 (-10 + (60 * (number%12)), 26.6f - (30 *(Mathf.Floor(number/12))), 0.0f);
		//Place the cursor in the middle of the screen
		cursor.transform.position = new Vector3 (Screen.width / 2, Screen.height / 2, 0.0f);

		//Get toggle component in the gameobject 
		Toggle _toggle = toggle.GetComponent<Toggle> ();
		//Assign toggle to its group (only one active at a time)
		_toggle.group = toggleGroup;

		//Get cursor control script
		panpointmovement _cursor = cursor.GetComponent<panpointmovement> ();

		//Assign a increasing number to recognise the cursor
		_cursor.order = number;

		//Check if default track number is available
		if (trackNumbers.ContainsValue (number + 1)) {//if not available
			//Find the highest track number and assign the next one
			_cursor.trackNumber = (Mathf.Max (trackNumbers.Values.Max ()))+1;	
		} else {//if available
			//Assign the default track number to the cursor
			_cursor.trackNumber = number + 1;
		}
		//Add the new cursor in the cursor dictionary
		trackNumbers.Add (_cursor.order, _cursor.trackNumber);

		//Assign all the necessary game objects to the new cursor
		_cursor.toggle = toggle;
		_cursor.trackInput = this.trackInput;
		_cursor.FXInput = this.FXInput;
		_cursor.manager = this;
		_cursor.rawAngl = this.rawAngl;
		_cursor.calAngl = this.calAngl;
		_cursor.xyz = this.xyz;
		_cursor.aziele = this.aziele;

		//Activate the newly created cursor
		StartCoroutine (ActivateNewPan (_toggle));

		//Next cursor created will have another indetification number
		number++;
	}

	//Call when max pitch Input Field changed value
	public void ChangePitch (string value){
		int tempnum;
		if (int.TryParse (value, out tempnum)) {//Try to transform the value entered in a integer number
			if (tempnum >= 0 && tempnum <= 180) {//Check that the number is between acceptable values
				//Change the max pitch value
				maxPitch = tempnum;
			} else {//if value is out of bounds reset to current max pitch
				pitchinput.text = maxPitch.ToString();
			}
		} else {//if parse wasn't succesful reset to current max pitch
			pitchinput.text = maxPitch.ToString();
		}
	}

	//Call when max yaw Input Field changed value
	public void ChangeYaw (string value){
		int tempnum;
		if (int.TryParse (value, out tempnum)) {//Try to transform the value entered in a integer number
			if (tempnum >= 0 && tempnum <= 360) {//Check that the number is between acceptable values
				//Change the max pitch value
				maxYaw = tempnum;
			} else {//if value is out of bounds reset to current max pitch
				yawinput.text = maxYaw.ToString();
			}
		} else {//if parse wasn't succesful reset to current max pitch
			yawinput.text = maxYaw.ToString();
		}
	}

	//Wait for end of frame to activate new cursor
	IEnumerator ActivateNewPan(Toggle toggle){
		yield return new WaitForEndOfFrame ();
		toggle.isOn = true;
	}
}
