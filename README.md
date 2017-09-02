# Houston
This is a game in-progress (not yet finished) I am working on in Unity.  The idea is that one player is in the cockpit of a rocket with minimal instrumentation, and must be given commands from another player who only sees the inside of their command center.  This is based loosely on the game *Keep Talking and Nobody Explodes*, where the goal is a mix of humor and a sense of accomplishment and teamwork when the players are successful.  

I am not an artist of any sort, so all models and skins should be considered WIP - basically all of them are placeholders.
This is my first major project involving C#, though the language structure is very similar to Java, my strongest language.  I once also attempted to write a VR chess game using C#, which was mildly successful, but I did not finish and moved on to other things before writing much of anything.
While I intended the game to be more silly, I got caught up in physics simulation and accidentally made an accurate simulation of a Saturn V rocket, including air resistance based on atmospheric pressure.  I am still unsure if I will continue with this model or create a less simulated model
Most of my ideas for multiplayer interaction are not yet implemented, and neither is the multiplayer itself or any of the Control Center.  This is a very early project but already utilizes some complex concepts.

## Features
  * Realistic physics simulation for a Saturn V Rocket
  * Includes air resistance, weight loss due to fuel, gravity based upon distance to the planet
  * Randomized player-blind buttons - the operator is meant to relay the purpose of each button
  * Minor 'punishments' for pressing these buttons without direction, such as a 'jettison' button
  * Launch button will toggle rockets on or off
  * Full player control of rocket facing and rotation using QEWASD
  * Custom edited sky filter to display stars upon gaining atmospheric height
  * Ability to roam the cabin by using 'F' to exit piloting mode.  Click the stick to pilot once more
  * Gravity applied on a roaming player based upon G forces on the craft and planetary gravity
  * 'Fake' ship - The player is inside a static model that displays the view of the moving model to reduce complications
  * Minor tree and terrain generation near launch site
  * Full-scale Earth and atmospheric model

## Known Bugs
  * Looking straight up or down can cause view problems
  * Rocket sounds do not end when thrusters cut
  * Resizing the game window causes the reticle to become offset from the center
  * Some textures or materials do not export properly when building with Unity
  * Unity WebPlayer sometimes offsets the game content slightly from the frame

### Play Live Demo
##### Be warned this is very WIP as mentioned on the site
[http://Dimencia.com](http://Dimencia.com/Houston)