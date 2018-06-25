//Stalker Drone AI Script

//Configuration
double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);

//Distances
double distanceDroneToPlayer = 0;
double distanceDroneToOrigin = 0;
double distancePlayerToOrigin = 0;

//Bool Checks
bool droneIsNPC = false;
bool inNaturalGravity = false;

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
IMyRemoteControl remoteControl;

Random rnd = new Random();

int messageFarIndex = 0;
int messageNearIndex = 0;
int messageVoxelIndex = 0;
int messageAttackIndex = 0;

int messageTimer = 0;
int messageTimerTrigger = 75;

string lastMessageSent = "Default";

void DroneBehaviour(string argument){
	
	TryIceRefill();
	
	var messagesFar = new List<string>();
	var messagesNear = new List<string>();
	var messagesVoxel = new List<string>();
	var messagesAttack = new List<string>();
	
	messagesFar.Add("I know you're out there, {Player}.");
	messagesFar.Add("What do you think will happen when I find you?");
	messagesFar.Add("Enjoy what little time you have left.");
	
	messagesNear.Add("It won't be long now.");
	messagesNear.Add("Make this easier for both of us and just give up.");
	messagesNear.Add("Nobody is going to save you, {Player}. This is where your story ends.");
	
	messagesVoxel.Add("I know you're around here somewhere.");
	messagesVoxel.Add("Come out, come out, wherever you are.");
	messagesVoxel.Add("You can't hide forever.");
	
	messagesAttack.Add("You're mine now!");
	messagesAttack.Add("It's time to end this little game!");
	messagesAttack.Add("You're not getting away from me now!");
	
	Echo("Got Here");
	
	if(distanceDroneToPlayer > 6000 && distanceDroneToPlayer < 15000 && remoteControl.CustomData != "Attack"){
		
		if(remoteControl.CustomData != "Far"){
			
			messageTimer = 0;
			
		}
		
		remoteControl.CustomData = "Far";
		SetDestination(closestPlayer, true, 15);
		
	}
	
	if(distanceDroneToPlayer < 6000 && distanceDroneToPlayer > 1000 && remoteControl.CustomData != "Attack"){
		
		if(remoteControl.CustomData != "Near"){
			
			messageTimer = 0;
			
		}
		
		remoteControl.CustomData = "Near";
		SetDestination(closestPlayer, true, 15);
		
	}
	
	if(distanceDroneToPlayer < 1000 && remoteControl.CustomData != "Attack"){
		
		if(remoteControl.CustomData != "Voxel"){
			
			messageTimer = 0;
			
		}
		
		remoteControl.CustomData = "Voxel";
		remoteControl.SetAutoPilotEnabled(false);
		
	}
	
	messageTimer++;
	
	if(remoteControl.CustomData == "Far"){
		
		if(messageTimer >= messageTimerTrigger){
			
			messageTimer = 0;
			
			if(messageFarIndex < messagesFar.Count){
				
				TryChat(messagesFar[messageFarIndex]);
				messageFarIndex++;
				
			}
			
		}
	
	}
	
	if(remoteControl.CustomData == "Near"){
	
		if(messageTimer >= messageTimerTrigger){
			
			messageTimer = 0;
			
			if(messageNearIndex < messagesNear.Count){
				
				TryChat(messagesNear[messageNearIndex]);
				messageNearIndex++;
				
			}
			
		}
	
	}
	
	if(remoteControl.CustomData == "Voxel"){
	
		if(messageTimer >= messageTimerTrigger){
			
			messageTimer = 0;
			
			if(messageVoxelIndex < messagesVoxel.Count){
				
				TryChat(messagesVoxel[messageVoxelIndex]);
				messageVoxelIndex++;
				
			}
			
		}
		
		if(ThreatDetection() == true){
			
			remoteControl.CustomData = "Attack";
			if(messageAttackIndex < messagesAttack.Count){
				
				messageTimer = 0;
				TryChat(messagesAttack[messageAttackIndex]);
				messageAttackIndex++;
				
			}
			
		}
	
	}
	
	if(remoteControl.CustomData == "Attack"){
		
		if(messageTimer >= messageTimerTrigger){
			
			messageTimer = 0;
			
			if(messageAttackIndex < messagesAttack.Count){
				
				TryChat(messagesAttack[messageAttackIndex]);
				messageAttackIndex++;
				
			}
			
		}
		
		bool missingCamera = false;
		var forwardScan = CameraRayCast(remoteControl.WorldMatrix.Forward, 800, out missingCamera);
		var downScan = CameraRayCast(remoteControl.WorldMatrix.Down, 800, out missingCamera);
		
		if(forwardScan.IsEmpty() == false){
			
			if(forwardScan.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
				
				WeaponActivation("Shoot_On", remoteControl.WorldMatrix.Forward);
				
			}else{
				
				WeaponActivation("Shoot_Off", remoteControl.WorldMatrix.Forward);
				
			}
			
		}else{
			
			WeaponActivation("Shoot_Off", remoteControl.WorldMatrix.Forward);
			
		}
		
		if(downScan.IsEmpty() == false){
			
			if(downScan.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
				
				WeaponActivation("Shoot_On", remoteControl.WorldMatrix.Down);
				
			}else{
				
				WeaponActivation("Shoot_Off", remoteControl.WorldMatrix.Down);
				
			}
			
		}else{
			
			WeaponActivation("Shoot_Off", remoteControl.WorldMatrix.Down);
			
		}
		
		if(inNaturalGravity == true){
			
			var upDir = Vector3D.Normalize(closestPlayer - planetLocation);
			var targetCoords = upDir * 300 + closestPlayer;
			SetDestination(targetCoords, false, 100);
			
		}else{
			
			if(distanceDroneToPlayer > 300){
				
				SetDestination(closestPlayer, false, 100);
				
			}else{
				
				remoteControl.SetAutoPilotEnabled(false);
				
			}
			
		}
			
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
	
	ResetBlockList();
	
	//Execute Custom Drone Behaviour
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

void WeaponActivation(string weaponMode, Vector3D weaponDirection){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyUserControllableGun;
				
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(weapon.WorldMatrix.Forward != weaponDirection){
			
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

Vector3D randomRealignPosition(){
	
	MatrixD rcMatrix = MatrixD.CreateWorld(remoteControl.GetPosition(), remoteControl.WorldMatrix.Forward, remoteControl.WorldMatrix.Up);
	Vector3D coords = new Vector3D(0,0,0);
	coords.X = (double)rnd.Next(-500, 500);
	coords.Y = 900;
	coords.Z = (double)rnd.Next(-500, 500);
	return coords;
	
}

bool ThreatDetection(){
	
	foreach(var block in blockList){
		
		var cameraBlock = block as IMyCameraBlock;
		var turretBlock = block as IMyLargeTurretBase;
		
		if(block.IsFunctional == false){
			
			continue;
			
		}
		
		if(turretBlock != null){
			
			if(turretBlock.HasTarget == true){
				
				return true;
				
			}
			
			
		}
		
		if(cameraBlock != null){
			
			cameraBlock.EnableRaycast = true;
			if(cameraBlock.CanScan(MeasureDistance(cameraBlock.GetPosition(), closestPlayer)) == true){

				var scanResults = cameraBlock.Raycast(closestPlayer);
				
				if(scanResults.IsEmpty() == false){
					
					if(scanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies || scanResults.Type == MyDetectedEntityType.CharacterHuman){
						
						return true;
						
					}
					
				}
								
			}
			
		}
		
	}
	
	return false;
	
}

double SpeedReducer(double droneSpeed){
	
	if(droneSpeed < 1000){
		
		return Math.Round(droneSpeed / 10, 2);
		
	}else{
		
		return 100;
		
	}
	
}

void TryChat(string message){
	
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
