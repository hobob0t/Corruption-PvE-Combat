//Hound Drone AI Script

//Configuration
double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D hunterLocation = new Vector3D(0,0,0);

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

enum DroneMode{
	Seek,
	Return,
	Despawn
};

DroneMode currentMode = DroneMode.Seek;
int despawnCounter = 0;

string lastMessageSent = "Default";

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update100;
	
}


void DroneBehaviour(string argument){

	TryIceRefill();
	
	if(hunterLocation == new Vector3D(0,0,0)){
		
		hunterLocation = dronePosition;
		
	}
	
	if(despawnCounter > 30){
		
		currentMode = DroneMode.Despawn;
		
	}
	
	if(argument.Contains("HoundReturn")){
		
		var dataSplit = argument.Split('\n');
		
		if(dataSplit.Length == 2){
			
			if(Vector3D.TryParse(dataSplit[1], out hunterLocation) == true){
				
				despawnCounter = 0;
				
			}
			
		}
		
		return;
		
	}
	
	if(argument.Contains("HoundDespawn")){
		
		currentMode = DroneMode.Despawn;
		return;
		
	}
	
	if(currentMode == DroneMode.Seek){
		
		despawnCounter++;
		var targetCoords = new Vector3D(0,0,0);
		
		if(inNaturalGravity == false){
			
			targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 900);
			
		}else{
			
			targetCoords = CreateDirectionAndTarget(planetLocation, closestPlayer, closestPlayer, 800);
			
		}
		
		SetDestination(targetCoords, false, 100);
		
		if(ThreatDetection() == true){
			
			TryChat("Bark, Bark, <Grrrrrrrrr>, BARK! BARK! BARK!");
			currentMode = DroneMode.Return;
			
		}
		
	}
	
	if(currentMode == DroneMode.Return){
		
		despawnCounter++;
		SetDestination(hunterLocation, false, 75);
		foreach(var block in blockList){
			
			var antenna = block as IMyRadioAntenna;
			
			if(antenna != null && block.IsFunctional == true){
				
				if(antenna.Enabled == true && antenna.EnableBroadcasting == true && MeasureDistance(hunterLocation, dronePosition) < 700){
					
					var result = antenna.TransmitMessage("HoundDrone|PlayerNearby\n" + dronePosition.ToString());
					
				}
				
			}
			
		}
		
	}
	
	if(currentMode == DroneMode.Despawn){
		
		if(distanceDroneToPlayer > 1200){
			
			TryDespawn(remoteControl, false);
			
		}
		
		SetDestination(hunterLocation, true, 100);
		
	}
		
}

void Main(string argument){
	
	if(argument.Contains("HoundDrone|PlayerNearby")){
		
		return;
		
	}
	
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

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
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
						
						if(MeasureDistance(scanResults.Position, dronePosition) < 1000){
							
							return true;
							
						}
						
					}
					
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
