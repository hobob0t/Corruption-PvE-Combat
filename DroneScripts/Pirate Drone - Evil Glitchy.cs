//Glitchy Drone Mk.II AI Script

double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D lastSpacePosition = new Vector3D(0,0,0);

//Distances
double distanceDroneToPlayer = 0;
double distanceDroneToOrigin = 0;
double distancePlayerToOrigin = 0;

//Bool Checks
bool droneIsNPC = false;
bool inNaturalGravity = false;
bool touchedGravity = false;
bool aggressionBegins = false;
bool transforming = false;
bool playerLeft = false;

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
IMyRemoteControl remoteControl;
IMyRadioAntenna antenna;
IMyTextPanel facePanel;
IMyTextPanel rearPanel;

string lastMessageSent = "Default";

string currentMode = "Space-Seeking";
Vector3D targetDirection = new Vector3D(0,0,0);
Vector3D localDirection = new Vector3D(0,0,0);
int timeOut = 0;
int tickCounter = 0;
Random rnd = new Random();

int currentBlockCount = 0;
int previousBlockCount = 0;

int currentFunctional = 0;
int previousFunctional = 0;

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}

void DroneBehaviour(string argument){
	
	WeaponActivation("Shoot_Off");
	
	if(antenna == null){
		
		foreach(var block in blockList){
		
			var tempAntenna = block as IMyRadioAntenna;
			
			if(tempAntenna != null){
				
				antenna = tempAntenna;
				break;
				
			}
			
		}
		
	}
	
	if(facePanel == null || rearPanel == null){
		
		foreach(var block in blockList){
		
			var tempFace = block as IMyTextPanel;
			
			if(tempFace != null && block.CustomName == "LCD Face"){
				
				facePanel = tempFace;
				
			}
			
			if(tempFace != null && block.CustomName == "LCD Rear"){
				
				rearPanel = tempFace;
				
			}
			
		}
		
	}
	
	if(aggressionBegins == false){
		
		//Check Rear Prox
		if(rearPanel != null){
			
			if(MeasureDistance(closestPlayer, rearPanel.GetPosition()) <= 2){
				
				TryChat("You freak! What do you think you're doing!?");
				aggressionBegins = true;
				timeOut = 0;
				currentMode = "Transform";
				
			}
			
		}
		
		//Check HackingDetection
		if(HackingDetection() == true){
			
			TryChat("You're a monster!");
			aggressionBegins = true;
			timeOut = 0;
			currentMode = "Transform";
			
		}
		
		//Check Existing Weapons
		if(HasWeapon() == true){
			
			aggressionBegins = true;
			timeOut = 0;
			currentMode = "Transform";
			
		}
		
		//Button Press
		if(argument.Contains("Run")){
			
			TryChat("I know what happened to my sisters... You're gonna pay!");
			timeOut = 0;
			aggressionBegins = true;
			currentMode = "Transform";
			
		}
		
		var buttonPanel = GridTerminalSystem.GetBlockWithName("Button Panel") as IMyButtonPanel;
		
		if(buttonPanel != null){
			
			if(buttonPanel.AnyoneCanUse == false){
				
				TryChat("I know what happened to my sisters... You're gonna pay!");
				timeOut = 0;
				aggressionBegins = true;
				currentMode = "Transform";
				
			}
			
		}
		
		//Check Missing Blocks
		currentBlockCount = blockList.Count;
		
		if(currentBlockCount < previousBlockCount && previousBlockCount != 0){
			
			TryChat("How could you hurt me like that!?");
			timeOut = 0;
			aggressionBegins = true;
			currentMode = "Transform";
			
		}else{
			
			previousBlockCount = currentBlockCount;
			
		}
		
		//Check Functional Blocks
		currentFunctional = FunctionalBlockCount();
		
		if(currentFunctional < previousFunctional && previousFunctional != 0){
			
			TryChat("How could you hurt me like that!?");
			timeOut = 0;
			aggressionBegins = true;
			currentMode = "Transform";
			
		}else{
			
			previousFunctional = currentFunctional;
			
		}
		
	}
	
	if(currentMode == "Space-Seeking"){
		
		Me.CubeGrid.CustomName = "(NPC-CPC) Glitchy Drone";
		timeOut = 0;
		if(inNaturalGravity == false && touchedGravity == false){
			
			lastSpacePosition = dronePosition;
			var targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 4900);
			SetDestination(targetCoords, true, 50);
			
			if(distanceDroneToPlayer < 5000){
				
				currentMode = "GreetPlayer";
				
			}
			
		}else{
			
			touchedGravity = true;
			SetDestination(lastSpacePosition, true, 100);
			
			if(MeasureDistance(lastSpacePosition, dronePosition) < 50){
				
				currentMode = "GreetPlayer";
				
			}
			
		}
		
	}
	
	
	if(currentMode == "GreetPlayer"){
		
		remoteControl.SetAutoPilotEnabled(false);
		FacePanelManager(facePanel, "CPC-DroneFaceNormal");
		
		if(distanceDroneToPlayer < 60){
			
			currentMode = "PlayerArrive";
			timeOut = 0;
			return;
			
		}
		
		timeOut++;
		
		if(timeOut <= 240){
			
			TryChat("Hi! I'm here to help! Please don't attack!");
			
		}
		
		if(timeOut > 240 && timeOut <= 300){
			
			TryChat("Is anybody out there??");
			
		}
		
		if(timeOut > 300){
			
			TryChat("Nobody ever wants my help...");
			currentMode = "Retreat";
			
		}
		
		
	}
	
	if(currentMode == "PlayerArrive"){
		
		remoteControl.SetAutoPilotEnabled(false);
				
		if(distanceDroneToPlayer > 60 && playerLeft == false){
			
			playerLeft = true;
			TryChat("Wait! Comeback!");
			
		}
		
		if(playerLeft == true && distanceDroneToPlayer < 60){
			
			playerLeft = false;
			
		}
		
		timeOut++;
		
		if(timeOut <= 60){
			
			TryChat("I'll do what I can to help! Just press the button!");
			
		}
		
		if(timeOut > 60 && timeOut <= 120){
			
			TryChat("The button is below my face panel. Go ahead and press it!");
			
		}
		
		if(timeOut > 120 && timeOut <= 180){
			
			TryChat("I'll do my best if you give me a chance!");
			
		}
		
		if(timeOut > 180 && timeOut <= 240){
			
			FacePanelManager(facePanel, "CPC-DroneFaceBored");
			TryChat("Maybe you don't want my help after all..");
			
		}
		
		if(timeOut > 240 && timeOut <= 300){
			
			TryChat("I'm going to leave soon...");
			
		}
		
		if(timeOut > 300){
			
			TryChat("I get it, you don't want my help. I'll go away...");
			currentMode = "Retreat";
			
		}
		
	}
	
	if(currentMode == "Retreat"){
		
		var targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 8000);
		SetDestination(targetCoords, true, 100);
		
		if(distanceDroneToPlayer > 4000){
			
			TryDespawn(remoteControl, false);
			
		}
		
	}
	
	if(currentMode == "Transform"){
		
		timeOut++;
		FacePanelManager(facePanel, "CPC-DroneFaceBerserk");
		remoteControl.Direction = Base6Directions.Direction.Backward;
		var targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 30);
		SetDestination(targetCoords, false, 100);
		transforming = true;
		
		foreach(var block in blockList){
			
			var connector = block as IMyShipConnector;
			
			if(connector != null){
				
				connector.Connect();
				
			}
			
		}
		
		if(timeOut >= 10){
			
			currentMode = "Attack";
			
			if(antenna != null){
				
				antenna.CustomName = "Glitchy Drone (Berserk)";
				
			}
			
		}
		
		
	}
	
	if(currentMode == "Attack"){
		
		transforming = false;
		
		remoteControl.Direction = Base6Directions.Direction.Forward;
		
		if(distanceDroneToPlayer > 40){
			
			var targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 30);
			SetDestination(targetCoords, false, 100);
			
		}
		bool noCamera = false;
		var scannedData = CameraRayCast(remoteControl.WorldMatrix.Forward, 800, out noCamera);
		
		if(scannedData.IsEmpty() == false){
			
			if(scannedData.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
				
				WeaponActivation("Shoot_On");
				return;
				
			}
			
		}
		
		WeaponActivation("Shoot_Off");
		
	}
	
}

void Main(string argument){
	
	try{
		
		if(Me.GetValue<bool>("CorruptionOwnerCheck") == false){
	
			Echo("Non-NPC");
			return;
		
		}
		
	}catch(Exception exc){
	
		//ModAPI Despawn Failed
	
	}
	
	if(transforming == true){
		
		TryAutoRepair();
		
	}
	
	tickCounter += 10;
	
	if(tickCounter < 60){
		
		return;
		
	}
	
	tickCounter = 0;
		
	//Reset Block List and Check For Remote Control
	remoteControl = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
			
	inNaturalGravity = false;
	if(remoteControl != null){
		
		if(remoteControl.IsFunctional == true){
			
			if(remoteControl.TryGetPlanetPosition(out planetLocation) == true){
				
				inNaturalGravity = true;
				
			}
			
		}

	}else{
		
		return;
		
	}
	
	//Ensure Remote Control Is NPC Owned & Get Player Location
	droneIsNPC = remoteControl.GetNearestPlayer(out closestPlayer);

	if(droneIsNPC == false){
		
		Echo("Drone is not controlled by an NPC");
		return;
		
	}
	
	OriginSetup();
		
	//Get Other Location Data
		
	dronePosition = remoteControl.GetPosition();

	//Calculate Distances
	distanceDroneToPlayer = MeasureDistance(dronePosition, closestPlayer);
	distanceDroneToOrigin = MeasureDistance(dronePosition, originPosition);
	distancePlayerToOrigin = MeasureDistance(closestPlayer, originPosition);
	
	if(distanceDroneToPlayer > noPlayerDespawnDist){
		
		TryDespawn(remoteControl, false);
		
	}
	
	//Execute Custom Drone Behaviour
	ResetBlockList();
	DroneBehaviour(argument);
	
}

double MeasureDistance(Vector3D point_a, Vector3D point_b){
	
	double result = Math.Round( Vector3D.Distance( point_a, point_b ), 2 );
	return result;
	
}

void ResetBlockList(){
	
	blockList.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blockList);
	
}

void OriginSetup(){
	
	if(droneIsNPC == false){
		
		originPosition = new Vector3D(0,0,0);
		
	}
	
	if(originPosition == new Vector3D(0,0,0) && droneIsNPC == true){
		
		originPosition = remoteControl.GetPosition();
		
	}
	
}

void SetDestination(Vector3D coords, bool collisionModeEnable, float speedLimit){
	
	remoteControl.ClearWaypoints();
	remoteControl.AddWaypoint(coords, "Destination");
	remoteControl.FlightMode = FlightMode.OneWay;
	remoteControl.SetAutoPilotEnabled(true);
	remoteControl.SetCollisionAvoidance(collisionModeEnable);
	remoteControl.SpeedLimit = speedLimit;
	
}

MyDetectedEntityInfo CameraRayCast(Vector3D direction, float scanDistance, out bool noCamera){
	
	noCamera = true;
	MyDetectedEntityInfo scanResults = new MyDetectedEntityInfo();
	
	foreach(var block in blockList){
		
		var camera = block as IMyCameraBlock;
		
		if(camera == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		//TODO: Figure out if camera direction is Forward or Up
		if(camera.WorldMatrix.Forward != direction){
			
			continue;
			
		}
		
		noCamera = false;
		camera.EnableRaycast = true;
				
		if(camera.CanScan(scanDistance) == false){
			
			continue;
			
		}
		
		scanResults = camera.Raycast(scanDistance, 0, 0);
		return scanResults;
		
	}
	
	return scanResults;
	
}

void WeaponActivation(string weaponMode){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyUserControllableGun;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		weapon.ApplyAction(weaponMode);
		
	}
	
}

void OverrideThrust(bool enableOverride, Vector3D direction, float thrustModifier){
	
	foreach(var block in blockList){
		
		var thruster = block as IMyThrust;
		
		if(thruster == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(enableOverride == true){
			
			if(thruster.WorldMatrix.Forward == direction * -1){
			
				float maxthrust = thruster.MaxThrust;
				thruster.ThrustOverridePercentage = thrustModifier;
			
			}
			
		}else{
			
			thruster.SetValueFloat("Override", 0);
			
		}
		
	}
	
}

void ArmWarheads(bool enabled){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyWarhead;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		weapon.IsArmed = enabled;
		
	}
	
}

void SelfDestruct(string weaponMode){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyWarhead;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
	
		if(weaponMode == "Detonate"){
			
			weapon.IsArmed = true;
			weapon.Detonate();
			
		}
		
	}
	
}

Vector3D RandomDirection(){
	
	Vector3D randomDir = new Vector3D(0,0,0);
	randomDir.X = RandomNumberBetween(-0.999999, 0.999999);
	randomDir.Y = RandomNumberBetween(-0.999999, 0.999999);
	randomDir.Z = RandomNumberBetween(-0.999999, 0.999999);
	randomDir = Vector3D.Normalize(randomDir);
	return randomDir;
	
	
}

double RandomNumberBetween(double minValue, double maxValue){
	
    var next = rnd.NextDouble();
    return minValue + (next * (maxValue - minValue));
	
}

void TryDespawn(IMyRemoteControl remoteControl, bool gravity){
	
	Vector3D planetPosition = new Vector3D(0,0,0);
	if(remoteControl.TryGetPlanetPosition(out planetPosition) == true || gravity == false){
	
		try{
		
			bool despawn = Me.GetValue<bool>("CorruptionDroneDespawn");
		
		}catch(Exception exc){
		
			//ModAPI Despawn Failed
		
		}
	
	}

}

void TryAutoRepair(){
	
	try{
		
		var projector = GridTerminalSystem.GetBlockWithName("Projector") as IMyProjector;
		
		if(projector != null){
			
			projector.Enabled = true;
			
		}
		
		Me.CustomData = "False\n10";
		var projectRepair = Me.GetValue<bool>("CorruptionProjectorBuild");
		Me.CustomData = "10";
		var gridRepair = Me.GetValue<bool>("CorruptionRepairBlocks");
		
	}catch(Exception e){
		
		//Couldn't Repair
		
	}
	
}

void TryIceRefill(){

	try{
		
		foreach(var block in blockList){
			
			var gasGenerator = block as IMyGasGenerator;
			
			if(gasGenerator != null){
				
				var inv = gasGenerator.GetInventory(0);
				
				if(inv.CurrentVolume == 0){
					
					bool refill = Me.GetValue<bool>("CorruptionIceRefill");
					
				}
				
			}
			
		}
		
	}catch(Exception exc){
	
		//ModAPI Refill Failed
	
	}

}

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

bool HackingDetection(){
	
	foreach(var block in blockList){
		
		if(block.IsBeingHacked == true){
			
			return true;
			
		}
		
	}
	
	return false;
	
}

bool HasWeapon(){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyUserControllableGun;
		
		if(weapon != null){
			
			return true;
			
		}
		
	}
	
	return false;
	
}

int FunctionalBlockCount(){
	
	int result = 0;
	
	foreach(var block in blockList){
		
		if(block.IsFunctional == true){
			
			result++;
			
		}
		
	}
	
	return result;
	
}

void TryChat(string message){
	
	if(antenna != null){
		
		antenna.CustomName = message;
		antenna.EnableBroadcasting = true;
		antenna.Radius = 8000;
		antenna.Enabled = true;
		
	}
	
	if(message == lastMessageSent){
		
		return;
		
	}
	
	try{
		
		Me.CustomData = message;
		bool despawn = Me.GetValue<bool>("CorruptionDroneChat");
		lastMessageSent = message;
	
	}catch(Exception exc){
	
		//ModAPI Chat Failed
	
	}

}

void FacePanelManager(IMyTextPanel textPanel, string facePanelName){
	
	if(textPanel == null){
		
		return;
		
	}
	
	string previousImage = textPanel.CurrentlyShownImage;
	
	if(textPanel.CurrentlyShownImage == null || textPanel.CurrentlyShownImage != facePanelName){
		
		textPanel.AddImageToSelection(facePanelName, false);
		
		if(previousImage != null){
			
			textPanel.RemoveImageFromSelection(textPanel.CurrentlyShownImage, true);
			
		}
				
	}
		
}