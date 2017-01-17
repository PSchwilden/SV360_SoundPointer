using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using UnityOSC;


//Pan cursor control class
public class panpointmovement : MonoBehaviour {

	[Header ("Panpot info")]
	public int order; //Cursor number definition
	public int trackNumber; //Target track where the FX controlled by the trigger is placed
	private int fxChainPosition = 1; //Position of the controlled effect on the FX chain of target track

	public InputField trackInput; //Input Field for the track number
	public InputField FXInput; //Input Field for the FX position number

	private bool uiclick = false; //Bool check for UI click detection
	private Vector3 _position; //Calculated position of the cursor
	private Vector3 _borderoffset; //Position offset applied to the cursor to enable movement after the borders
	private Vector3 lastmouseposition; //Position of the mouse on the last frame
	private int overhead; //Number of times the cursor when above or below vertical axis on one click

	private float _depth = 1.0f; //Calculated depth of the cursor
	private Vector3 originalscale; //Initial scale of the cursor at default depth

	private bool activated = false; //Bool check to know which cursor is active

	public GameObject toggle; //Toggle linked to this cursor
	private TextMesh header; //Header of the cursor
	public pancreation manager; //Head manager of the cursors


	[Header ("Positional info")]
	public float ryaw; //Raw yaw value 0 to 1
	public float rpitch; //Raw pitch value 0 to 1
	public float rdepth; //Raw depth value 0 to 10
	public float yaw; //Calculated yaw value in degrees
	public float pitch; //Calculated pitch value in degrees
	public float depth; //Calculated depth value 0 to 10
	public Vector3 coordinates; //Calculated cartesian coordinates

	public Text rawAngl; //Raw position monitoring text
	public Text calAngl; //Calculated position monitoring text

	private string addressYaw; //Target OSC address for the Yaw parameter in Reaper
	private string addressPitch; //Target OSC address for the Pitch parameter in Reaper
	private string addressX; //Target OSC address for the X parameter in Reaper
	private string addressY; //Target OSC address for the Y parameter in Reaper
	private string addressZ; //Target OSC address for the Z parameter in Reaper

	public Toggle xyz; //Toggle of the cartesian values
	public Toggle aziele; //Toggle of the spherical values
	private bool cartesian = false; //Bool check for cartesian or spherical values

	[Header ("Settings")]

	private int xNum, yNum, zNum, aNum, eNum;

	/* TO DO -- Receive data back from Reaper
	public Vector3 reapcoord;
	private bool receivedmsg = false;
	*/

	// Use this for initialization
	void Start () {

		//Listen to the linked toggle value
		toggle.GetComponent<Toggle>().onValueChanged.AddListener (Activate);

		//Find cursor header text
		header = gameObject.GetComponentInChildren<TextMesh> ();

		//Listen to the track number Input Field
		trackInput.onEndEdit.AddListener (ChangeTrackNumber);

		//Listen to the FX position Input Field
		FXInput.onEndEdit.AddListener (ChangeFXNumber);

		//listen to the XYZ toggle
		xyz.onValueChanged.AddListener (UseCartesian);

		//Save original scale
		originalscale = this.transform.localScale;

		//First time update of the Reaper addresses
		UpdateAdresses ();

		/* TO DO -- Receive data back from Reaper
		OSCHandler.Instance.OnReaperEvent += CheckReaperData;
		OSCHandler.Instance._servers["Reaper"].server.PacketReceivedEvent += CheckReaperData;
		*/
	}
	
	// Update is called once per frame
	void Update () {

		//Update the tracknumber on the cursor and the toggle
		header.text = "Pan " + trackNumber.ToString();
		toggle.GetComponentInChildren<Text> ().text = "Pan " + trackNumber.ToString ();

		//Transform on screen mouse position to world position
		Vector3 screenPos = Input.mousePosition;
		Vector3 worldPos = Camera.main.ScreenToWorldPoint (screenPos);

		/* TO DO -- Receive data back from Reaper
		this.transform.position = reapcoord;
		*/

		if (activated) { //The corresponding toggle is active

			this.gameObject.GetComponent<SpriteRenderer> ().color = Color.green;


			/*Position the cursor*/

			//Check if clicked on UI
			if (Input.GetMouseButtonDown (0) && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject () == true) {
				uiclick = true;
			} else if (Input.GetMouseButtonDown (0) && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject () == false) {
				uiclick = false;
				//On click outside UI, reset offset values
				this.transform.position = worldPos;
				_borderoffset = Vector3.zero;
				overhead = 0;
			}

			//Update cursor position on position of the mouse
			if (Input.GetMouseButton (0) && uiclick == false) {

				//Apply cursor offset when mouse reaches border of the screen but is still moving
				if (lastmouseposition.x == Input.mousePosition.x && Input.GetAxis ("Mouse X") != 0.0f) {
					_borderoffset.x += Input.GetAxis ("Mouse X") * 25;
				}
				if (lastmouseposition.y == Input.mousePosition.y && Input.GetAxis ("Mouse Y") != 0.0f) {
					_borderoffset.y += Input.GetAxis ("Mouse Y") * 25;
				}

				//In case of 360° azimuth, when cursor reaches one side of the screen, jump to the other (full rotation)
				if (manager.maxYaw == 360) {
					_position.x = Mathf.Repeat (worldPos.x + _borderoffset.x + (Screen.width / 2) * overhead, Screen.width);
				} else { //else clamp to screen borders
					_position.x = Mathf.Clamp (worldPos.x, 0.0f, Screen.width);
				}
					
				//In case of 360° azimuth and 180° elevation, when cursor reaches top or bottom of the screen, turn 180° and invert movement (overhead rotation)
				if (manager.maxPitch == 180 && manager.maxYaw == 360) {
					_position.y = Mathf.PingPong (worldPos.y + _borderoffset.y, Screen.height);
					overhead = Mathf.FloorToInt ((worldPos.y + _borderoffset.y) / Screen.height);
				} else { //else clamp to screen borders
					_position.y = Mathf.Clamp (worldPos.y, 0.0f, Screen.height);
				}
				
				//Reset mouse movement detection
				lastmouseposition = Input.mousePosition;

				//Apply all movement calculations
				this.transform.position = _position;
			}


			/*Size of the cursor*/

			//Control depth parameter with mouse wheel
			if (Input.GetAxis ("Mouse ScrollWheel") != 0.0f) {
				_depth = Mathf.Clamp (_depth + Input.GetAxis ("Mouse ScrollWheel"), 0.0f, 10.0f);
			}
			//Change size of the cursor depending on the value of depth
			this.transform.localScale = originalscale * (1 / (_depth + 0.05f));



			/*Send OSC Messages*/

			//Transform cursor position values in raw pitch, yaw and depth values from 0 to 1 and clamp to selected max values
			rdepth = _depth;
			ryaw = (((this.transform.position.x / Screen.width)-0.5f)*(manager.maxYaw/360f))+0.5f;
			rpitch = (((this.transform.position.y / Screen.height)-0.5f)*(manager.maxPitch/180f))+0.5f;

			//Transform raw orientation values to degree orientation
			yaw =  Mathf.RoundToInt((ryaw * 360f)-180f);
			pitch = Mathf.RoundToInt((rpitch * 180f)-90f);
			depth = rdepth;

			//Update raw angles monitoring
			rawAngl.text = "yaw : " + ryaw.ToString () + " pitch : " + rpitch.ToString () + " depth : " + rdepth.ToString();

			//Check if the values have to be sent in cartesian or spherical values
			if (cartesian) {//if cartesian
				coordinates = Sph2Cart (ryaw, rpitch, rdepth); //Apply spherical to cartesian conversion
				//Update calculated angles monitoring
				calAngl.text = "Cursor coordinates :" + "\n" + "X = " + coordinates.x.ToString () + "\n" + "Y = " + coordinates.y.ToString () + "\n" + "Z = " + coordinates.z.ToString ();
				//Send positional information to Reaper
				OSCHandler.Instance.SendMessageToClient ("Reaper", addressX, coordinates.x);
				OSCHandler.Instance.SendMessageToClient ("Reaper", addressY, coordinates.y);
				OSCHandler.Instance.SendMessageToClient ("Reaper", addressZ, coordinates.z);

			} else {//if spherical
				//Update calculated angles monitoring
				calAngl.text = "Cursor direction :" + "\n" + "Azimuth = " + yaw.ToString () + "\n" + "Elevation = " + pitch.ToString ();
				//Send positional information to Reaper
				OSCHandler.Instance.SendMessageToClient ("Reaper", addressYaw, ryaw);
				OSCHandler.Instance.SendMessageToClient ("Reaper", addressPitch, rpitch);

			}

		} else if (!activated) {//The corresponding toggle is inactive
			this.gameObject.GetComponent<SpriteRenderer> ().color = Color.white;
		}
	}


	//Call when linked toggle changed value
	public void Activate (bool value){
		activated = value;

		//Change the value shown in the track number and FX number Input Fields of the activated cursor
		if (activated) {
			trackInput.text = trackNumber.ToString();
			FXInput.text = fxChainPosition.ToString ();

			//Change the toggle for cartesian or spherical calculation
			if (cartesian) {
				xyz.isOn = true;
			} else {
				aziele.isOn = true;
			}
		}
	}


	//Call when track number Input Field changed value
	public void ChangeTrackNumber (string value)
	{
		if (activated) {//check if this is the activated cursor
			int tempNum;
			if (int.TryParse (value, out tempNum)) {//Try to transform the value entered in a integer number
				if (tempNum >= 0) {//Check that the number is positive
					//Check if track number is available
					if (manager.trackNumbers.ContainsValue (tempNum)) {//if track number is already taken
						//Find which cursor has the track number assigned
						int conflictOrder = manager.trackNumbers.FirstOrDefault (x => x.Value == tempNum).Key;
						//Activate that cursor
						GameObject.Find ("Toggles").transform.GetChild (conflictOrder).GetComponent<Toggle> ().isOn = true;
					} else {//if track is not taken
						//Change this cursor's track number
						trackNumber = tempNum;
						//Update the track number dictionary
						manager.trackNumbers [order] = tempNum;
						//Update the destination OSC Addresses
						UpdateAdresses ();
					}
				} else {//if value is negative reset to current track number
					trackInput.text = trackNumber.ToString ();
				}
			} else {//if parse wasn't succesful reset to current track number
				trackInput.text = trackNumber.ToString ();
			}
		}
	}


	//Call when FX position number Input Field changed value
	public void ChangeFXNumber (string value)
	{
		if (activated) {//check if this is the activated cursor
			int tempNum;
			if (int.TryParse (value, out tempNum)) {//Try to transform the value entered in a integer number
				if (tempNum >= 0) {//Check that the number is positive
					//Change FX position of the cursor
					fxChainPosition = tempNum;
					//Update the destination OSC Addresses
					UpdateAdresses ();
				} else {//if value is negative reset to current FX position number
					FXInput.text = fxChainPosition.ToString ();
				}
			} else {//if parse wasn't succesful reset to current FX position number
				FXInput.text = fxChainPosition.ToString ();
			}
		}
	}


	//Spherical to Cartesian calculation
	private static Vector3 Sph2Cart (float azimuth, float elevation, float radius){
		float x = (radius * Mathf.Cos (((0.75f - azimuth) * 360) * Mathf.Deg2Rad) * Mathf.Sin (((1 - elevation) * 180) * Mathf.Deg2Rad))/2 + 0.5f;
		float y = (radius * Mathf.Sin (((0.75f - azimuth) * 360) * Mathf.Deg2Rad) * Mathf.Sin (((1 - elevation) * 180) * Mathf.Deg2Rad))/2 + 0.5f;
		float z = (radius * Mathf.Cos (((1 - elevation) * 180) * Mathf.Deg2Rad)) / 2 + 0.5f;

		return new Vector3 (x, y, z);
	}

	//Call when cartesian or spherical toggle is changed
	public void UseCartesian (bool value){
		if (activated) {
			this.cartesian = value;
		}
	}

	//Create address string to send to Reaper depending on the track number and fx position number of this cursor
	private void UpdateAdresses(){
		addressYaw = string.Format ("/track/{0}/fx/{1}/fxparam/{2}/value", trackNumber.ToString (), fxChainPosition.ToString (),manager.aNum.ToString());
		addressPitch = string.Format ("/track/{0}/fx/{1}/fxparam/{2}/value", trackNumber.ToString (), fxChainPosition.ToString (),manager.eNum.ToString());
		addressX = string.Format ("/track/{0}/fx/{1}/fxparam/{2}/value", trackNumber.ToString (), fxChainPosition.ToString (),manager.xNum.ToString());
		addressY = string.Format ("/track/{0}/fx/{1}/fxparam/{2}/value", trackNumber.ToString (), fxChainPosition.ToString (),manager.yNum.ToString());
		addressZ = string.Format ("/track/{0}/fx/{1}/fxparam/{2}/value", trackNumber.ToString (), fxChainPosition.ToString (),manager.zNum.ToString());
	}



	/* TO DO -- Receive data back from Reaper

	void CheckReaperData (OSCServer server, OSCPacket packet){
		foreach (OSCMessage message in packet.Data) {
			Debug.Log (message.Address + message.Data [0].ToString ());
			if (message.Address == addressYaw) {
				reapcoord.x = ((float.Parse (message.Data [0].ToString ()) - 0.5f) / (manager.maxYaw / 360f) + 0.5f) * Screen.width;
			} else if (message.Address == addressPitch) {
				reapcoord.y = ((float.Parse (message.Data [0].ToString ()) - 0.5f) / (manager.maxPitch / 180f) + 0.5f) * Screen.height;
			}
		}
	}
	*/
}

