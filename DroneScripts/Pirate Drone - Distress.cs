//Corruption Drone AI Template - Distress Call Ship

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

int chatTimer = 0;
int chatIndex = 0;

string lastMessageSent = "Default";
Random rnd = new Random();

void DroneBehaviour(string argument){

	var greetingChat = new List<string>();
	greetingChat.Add("Hello? Is anybody out there?");
	greetingChat.Add("We need assistance.");
	greetingChat.Add("Our transport was attacked by the drones. We're barely operational.");
	greetingChat.Add("We have information about them. Information that could stop them for good.");
	greetingChat.Add("Please, if you are receiving this, you may be our only hope for survival.");
	
	if(ThreatDetection() == true && remoteControl.CustomData != "Attack"){
		
		//TODO - Try Spawning
		Me.CustomData = "(CPC)DECEPTION_DRONE_ANTENNA\n";
		Me.CustomData += Me.GetPosition().ToString() + "\n";
		Me.CustomData += new Vector3D(0,0,0).ToString() + "\n";
		Me.CustomData += Me.CubeGrid.WorldMatrix.Forward.ToString() + "\n";
		Me.CustomData += "False";
		var spawningResult = TrySpawning();
		
		var antennaA = GridTerminalSystem.GetBlockWithName("Damaged Research Vessel") as IMyRadioAntenna;
		var antennaB = GridTerminalSystem.GetBlockWithName("Deception Drone") as IMyRadioAntenna;
		var gravity = GridTerminalSystem.GetBlockWithName("Spherical Gravity Generator") as IMyGravityGeneratorBase;
		
		if(antennaA != null){
			
			antennaA.Enabled = false;
			
		}
		
		if(antennaB != null){
			
			antennaB.Enabled = true;
			antennaB.EnableBroadcasting = true;
			
		}
		
		if(gravity != null){
			
			gravity.GravityAcceleration = gravity.GravityAcceleration * -1;
			
		}
		
		remoteControl.CustomData = "Attack";
		Me.CubeGrid.CustomName = "(NPC-CPC) Deception Drone";
		TryChat("Today you will learn that we do not leave survivors. All units, attack!");
		
	}
	
	if(remoteControl.CustomData != "Attack"){
		
		if(chatIndex < greetingChat.Count && chatTimer >= 20){
			
			chatTimer = 0;
			TryChat(greetingChat[chatIndex]);
			chatIndex++;
			
		}
		
		if(chatIndex < greetingChat.Count && chatTimer < 20){
			
			chatTimer++;
			
		}
		
	}else{
		
		if(distanceDroneToPlayer > 150){
			
			SetDestination(closestPlayer, false, 100);
			
		}else{
			
			remoteControl.SetAutoPilotEnabled(false);
			
		}
		
		bool noCamera = false;
		var scannedData = CameraRayCast(remoteControl.WorldMatrix.Forward, 850, out noCamera);	
		
		if(scannedData.IsEmpty() != false){
			
			if(scannedData.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
						
				WeaponActivation("Shoot_On");
					
			}else{
				
				WeaponActivation("Shoot_Off");
				
			}
			
		}else{
				
			WeaponActivation("Shoot_Off");
				
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
	
		//ModAPI Check Failed
	
	}
	
	//Reset Block List and Check For Remote Control
	remoteControl = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
	
	inNaturalGravity = false;
	if(remoteControl != null){
		
		if(remoteControl.IsFunctional == true){
			
			if(remoteControl.TryGetPlanetPosition(out planetLocation) == true){
				
				inNaturalGravity = true;
				
			}
			
		}else{
			
			Echo("Remote Control Broken.");
			return;
			
		}

	}else{
		
		Echo("Remote Control Not Found.");
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
	remoteControl.FlightMode = FlightMode.OneWay;
	remoteControl.AddWaypoint(coords, "Destination");
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

bool ThreatDetection(){
	
	if(distanceDroneToPlayer < 1000){
		
		return true;
		
	}
	
	var scannedData = new MyDetectedEntityInfo();
	foreach(var block in blockList){
		
		var cameraBlock = block as IMyCameraBlock;
		var turret = block as IMyLargeTurretBase;
		
		if(block.IsFunctional == false){
			
			continue;
			
		}
				
		if(cameraBlock != null){
			
			cameraBlock.EnableRaycast = true;
			if(cameraBlock.CanScan(MeasureDistance(cameraBlock.GetPosition(), closestPlayer)) == true){

				var scanResults = cameraBlock.Raycast(closestPlayer);
				
				if(scanResults.IsEmpty() == false){
					
					if(scanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies && MeasureDistance(scannedData.Position, dronePosition) < 1000){
						
						scannedData = scanResults;
						return true;
						
					}
					
				}
								
			}
			
			if(cameraBlock.CanScan(1000) == true){
				
				var scanResults = cameraBlock.Raycast(1000, 0, 0);
				
				if(scanResults.IsEmpty() == false){
					
					if(scanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
						
						scannedData = scanResults;
						return true;
						
					}
					
				}
				
			}
			
		}
		
		if(turret != null){
			
			var scanResults = turret.GetTargetedEntity();
				
			if(scanResults.IsEmpty() == false){
				
				if(scanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
					
					scannedData = scanResults;
					return true;
					
				}
				
			}
			
		}
				
	}

	return false;
	
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

bool TrySpawning(){
	
	try{
		
		var result = Me.GetValue<bool>("CorruptionSpawning");
		return result;
		
	}catch(Exception exc){
		
		return false;
		
	}
	
	return false;
	
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
