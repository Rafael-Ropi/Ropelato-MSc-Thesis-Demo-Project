# Ropelato-MSc-Thesis-Demo-Project
 Master Thesis Repository of Rafael Ropelato for Thesis "Generating Feasible Spawn Locations for Autonomous Robot Simulations in Complex Environments"


## Launch Project
- Download and Install Unity Hub (\href{https://unity3d.com/get-unity/download}{https://unity3d.com/get-unity/download})
- Install Unity version \textbf{2020.3.24f1}
- Open Unity project folder in Unity Hub


## Launch Search Algorithms
- Once the Project was loaded, it should open the training environment. If not, select the scene in the Project Window under Assets/Scenes/Training Environment.
- The Project Hierarchy shows all GameObjects. This includes th 2D\_Search, 3D\_Search and RandomSearch object.
- Select any of the search algorithms.
- The parameters for the search algorithm can be set in the Object Inspector on the right-hand side.
- Set up the parameters and launch the algorithm by setting the RUN TEST boolean (check box) to $True$.

### Algorithm Paramerers:
 - robotModel 	(attach robot game object)

Settings - Spawn Generator
 - limitLow 		(Search perimeter lower limit)
 - limitHigh		(Search perimeter upper limit)
 - stepSizeXZ 		(Step size in XZ plane)
 - stepSizeY 		(Step size in Y direction)
 - checkHeight 		(Max spawn height above Terrain)
 - maxWaterHeight 	(Max height of any water plane)
 - ignoredTags 		(Tags that are not obstacles)
 - waterTag		(Water Tag)
 - terrainTag		(Terrain Tag)

Select - Gizmos Drawn
 - drawSearchArea	(Draw search perimeter (red))
 - drawInvalid		(Draw Invalid spawns found (red))
 - drawValid		(Draw Valid spawns found (green))
 - drawAir		(Draw Aerial spawns found (blue) - CAUTION, can crash Unity if too many)

Settings - Gizmos
 - gizmoSize		(Spawn marker size)
 - gizmoAlpha		(Spawn marker transparency)
 - gizmoSphere		(Draw spheres instead of boxes (increases lag))
 - gizmoWire		(Reduce lag)

Run Single Test Commands
 - runTest	 	(Execute single test run)
 - destroySpawnLists	(Destroy existing results)

Write Results
 - testNr		(Test number, used for save folder name)
 - environmentName	(Name of environment (to create folder))
 - writeToFile		(Start writing to file)

Run Multiple Searches
 - multiRunTestNr	(Test number, used for save folder name)
 - multiRunEnvironmentName;	(Name of environment (to create folder))
 - N = 1;      		(Nr. of test runs)
 - minStepSize		(Linear step size increment lower limit)
 - maxStepSize		(Linear step size increment upper limit)
 - runMultipleTests	(Execute multiple test runs and store to file)


## Custom Environment
- All objects that can cause a collision need to have a collider attached
- All objects which should server as spawnable terrain need to have the tag "Terrain". Can be set in the top-left of the Object Inspector.
- All water planes need to have the tag "Water".


## Custom Robot
Import object.
- Add script in object inspector. "Add Component" $\rightarrow$ search "Feasibility Check (Script)".
- Append a "Rigid Body" and a "Collider" object in the object inspector.
  - If mesh collider, make it "Convex".
  - Make collider "Is Trigger".
  - Make rigid body "Is Kinematic".
- Attach robot object to the search algorithms.
- (OPTIONAL) Attach "Colored Collision Detector (Script)" to robot object. This colors the robot green or red depending on collision status in scene view.