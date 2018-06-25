//Hunter Drone Mk.II AI Script

//Configuration
string remoteControlSpaceName = "Remote Control (Space)";
string remoteControlGravityName = "Remote Control (Gravity)";
double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D despawnLocation = new Vector3D(0,0,0);

//Distances
double distanceDroneToPlayer = 0;
double distanceDroneToOrigin = 0;
double distancePlayerToOrigin = 0;

//Bool Checks
bool droneIsNPC = false;
bool inNaturalGravity = false;
bool releasedHounds = false;
bool sentDespawnCommand = false;

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
IMyRemoteControl remoteControlSpace;
IMyRemoteControl remoteControlGravity;
IMyRemoteControl remoteControl;

enum DroneMode{
	Broadcast,
	Attack
};

DroneMode currentMode = DroneMode.Broadcast;
int tickCounter = 0;
string lastMessageSent = "Default";
Random rnd = new Random();

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}

void DroneBehaviour(string argument){
	
	TryIceRefill();
	
	if(currentMode == DroneMode.Broadcast){
		
		if(inNaturalGravity == true){
			
			SetDestination(despawnLocation, false, 20);
			
		}
		
		if(ThreatDetection() == true){
			
			TryChat("You've come to accept your fate? So be it!");
			currentMode = DroneMode.Attack;
			this.Storage = "Attack";
			return;
			
		}
		
		foreach(var block in blockList){
			
			var antenna = block as IMyRadioAntenna;
			
			if(antenna != null && block.IsFunctional == true){
				
				if(antenna.Enabled == true && antenna.EnableBroadcasting == true){
					
					var houndLocTransmit = remoteControl.WorldMatrix.Backward * 150 + dronePosition;
					var result = antenna.TransmitMessage("HoundReturn\n" + houndLocTransmit.ToString());
					
					if(releasedHounds == false){
						
						var spawnareaA = remoteControl.WorldMatrix.Up * 300 + remoteControl.GetPosition();
						
						Me.CustomData = "(CPC)HOUND_DRONE_ANTENNA" + "\n";
						Me.CustomData += spawnareaA.ToString() + "\n";
						Me.CustomData += remoteControl.GetShipVelocities().LinearVelocity.ToString() + "\n";
						Me.CustomData += remoteControl.WorldMatrix.Forward.ToString() + "\n";
						Me.CustomData += "True";
						var spawningResultA = TrySpawning();
						
						if(spawningResultA == true){
							
							releasedHounds = true;
							TryChat("The prey is near-by. Release the Hound!");
							
						}
						
						break;
						
					}
					
					break;
					
				}
				
			}
			
		}
		
		if(argument.Contains("HoundDrone|PlayerNearby") == true){
			
			var dataSplit = argument.Split('\n');
			var houndLocation = new Vector3D(0,0,0);
		
			if(dataSplit.Length == 2){
				
				if(Vector3D.TryParse(dataSplit[1], out houndLocation) == true){
					
					if(MeasureDistance(dronePosition, houndLocation) < 1000){
						
						TryChat("You've been spotted, {Player}. Now, I'm coming for you!");
						currentMode = DroneMode.Attack;
						this.Storage = "Attack";
						
					}
					
				}
				
			}
						
		}
		
		return;
		
	}
	
	if(currentMode == DroneMode.Attack){
		
		var scannedTarget = new MyDetectedEntityInfo();
		var targetCoords = new Vector3D(0,0,0);
		
		if(sentDespawnCommand == false){
			
			sentDespawnCommand = true;
			
			foreach(var block in blockList){
			
				var antenna = block as IMyRadioAntenna;
				
				if(antenna != null && block.IsFunctional == true){
					
					if(antenna.Enabled == true && antenna.EnableBroadcasting == true){
						
						var result = antenna.TransmitMessage("HoundDespawn");
					
					}
					
				}
				
			}
			
		}
		
		if(inNaturalGravity == false){
			
			scannedTarget = CameraRayCast(remoteControl.WorldMatrix.Forward, 800);
			targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 250);
			
			
		}else{
			
			scannedTarget = CameraRayCast(remoteControl.WorldMatrix.Down, 800);
			targetCoords = CreateDirectionAndTarget(planetLocation, closestPlayer, closestPlayer, 250);
			
		}
		
		if(distanceDroneToPlayer > 250){
			
			SetDestination(targetCoords, false, 100);
			
		}else{
			
			remoteControl.SetAutoPilotEnabled(false);
			
		}
		
		if(scannedTarget.IsEmpty() == false){
			
			if(scannedTarget.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
				
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
	
		//ModAPI Despawn Failed
	
	}
	
	tickCounter += 10;
	
	if(tickCounter < 60){
		
		return;
		
	}
	
	tickCounter = 0;
	
	//Reset Block List and Check For Remote Control
	remoteControlSpace = GridTerminalSystem.GetBlockWithName(remoteControlSpaceName) as IMyRemoteControl;
	remoteControlGravity = GridTerminalSystem.GetBlockWithName(remoteControlGravityName) as IMyRemoteControl;
	
	if(remoteControlSpace == null && remoteControlGravity == null){
		
		Echo("No remote control found. Aborting Script");
		return;
		
	}
	
	if(remoteControlSpace.IsFunctional == false && remoteControlGravity.IsFunctional == false){
		
		Echo("No remote control found. Aborting Script");
		return;
		
	}
	
	ResetBlockList();
	
	
	inNaturalGravity = false;
	if(remoteControlGravity != null){
		
		if(remoteControlGravity.IsFunctional == true){
			
			if(remoteControlGravity.TryGetPlanetPosition(out planetLocation) == true){
				
				remoteControl = remoteControlGravity;
				remoteControlSpace?.SetAutoPilotEnabled(false);
				inNaturalGravity = true;
				
			}
			
		}

	}
	
	if(inNaturalGravity == false){
		
		remoteControl = remoteControlSpace;
		remoteControlGravity?.SetAutoPilotEnabled(false);
		
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
	
	if(this.Storage == null || this.Storage == ""){
		
		var despawnTarget = remoteControl.WorldMatrix.Forward * 12000 + remoteControl.GetPosition();
		despawnLocation = despawnTarget;
		this.Storage = despawnTarget.ToString();
		
	}else{
		
		if(Vector3D.TryParse(this.Storage, out despawnLocation) == false && this.Storage != "Attack"){
			
			var despawnTarget = remoteControl.WorldMatrix.Forward * 12000 + remoteControl.GetPosition();
			despawnLocation = despawnTarget;
			this.Storage = despawnTarget.ToString();
			
		}
		
	}
	
	if(this.Storage == "Attack"){
		
		currentMode = DroneMode.Attack;
		
	}
	
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

MyDetectedEntityInfo CameraRayCast(Vector3D direction, float scanDistance){
	
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
		
		camera.EnableRaycast = true;
				
		if(camera.CanScan(scanDistance) == false){
			
			continue;
			
		}
		
		scanResults = camera.Raycast(scanDistance, 0, 0);
		return scanResults;
		
	}
	
	return scanResults;
	
}


bool ThreatDetection(){
		
	if(distanceDroneToPlayer < 1000){
		
		return true;
		
	}
	
	foreach(var block in blockList){

		var turret = block as IMyLargeTurretBase;
		
		if(block.IsFunctional == false){
			
			continue;
			
		}
		
		if(turret != null){
			
			var scanResults = turret.GetTargetedEntity();
				
			if(scanResults.IsEmpty() == false){
				
				if(scanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
					
					return true;
					
				}
				
			}
			
		}
				
	}

	return false;
	
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


Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

bool TrySpawning(){
	
	try{
		
		var result = Me.GetValue<bool>("CorruptionSpawning");
		return result;
		
	}catch(Exception exc){
		
		Echo("Bad Fail");
		return false;
		
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
