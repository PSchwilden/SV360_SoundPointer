# SV360_SoundPointer

OSC Driven standalone panner to use with Reaper.

Created with Unity 5.4.3 and jorgegarcia's UnityOSC scripts (https://github.com/jorgegarcia/UnityOSC).

It is obvious there is much room to improve so feel free to contribute to this project by giving feedback or advice.

# How to use:

Launch the sound pointer and launch the Reaper application. 

Add a new Control Surface in Reaper preferences, make it OSC and select your listen port.

In the sound pointer, set up the initial parameters, create a new panner by clicking the "+" button, and you can then choose which track and which FX it should be controlling. Do not forget to also choose if the values sent to Reaper are in azimuth-elevation or xyz values.

For the moment, to make it work with the Reaper video we use PeekThrough (http://www.lukepaynesoftware.com/projects/peek-through/) and superimpose the sound pointer to Reaper's video window.

# TODO

 - Settings panel with send port control and fx parameter number control DONE :)
 - Receive data back from Reaper
 - Play video synchronised to Reaper transport
