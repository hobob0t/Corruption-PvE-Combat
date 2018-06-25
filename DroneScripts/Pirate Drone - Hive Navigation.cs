//Corruption Drone AI Template - Hive Ship Navigation

//Configuration
double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D deployingLocation = new Vector3D(0,0,0);

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

string currentMode = "Seeking";
Vector3D playerPlanetLocation = new Vector3D(0,0,0);

string lastMessageSent = "Default";
Random rnd = new Random();

bool greetingMsgSent = false;
bool leavingMsgSent = false;
bool gotTooCloseMsgSent = false;

int spawnTimer = 235;
int spawnTimerTrigger = 240;

bool spawnPhaseActive = false;
int spawnPhaseTimer = 0;
int spawnPhaseTimerTrigger = 3;
int spawnedDrones = 0;
int spawnedDronesMax = 3;
string selectedSpawnGroup = "";

int totalSpawnPhases = 0;
int maxSpawnPhases = 5;

int scriptRun = 0;

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}

void DroneBehaviour(string argument){
	
	TryIceRefill();

	if(currentMode == "Seeking"){
		
		if(distanceDroneToPlayer < 6000 && greetingMsgSent == false){
			
			greetingMsgSent = true;
			TryChat("You are not welcome here. Leave, or face extermination.");
			
		}
		
		if(distanceDroneToPlayer < 2300){
			
			currentMode = "Deploying";
			deployingLocation = dronePosition;
			remoteControl.SetAutoPilotEnabled(false);
			return;
			
		}else{
			
			if(inNaturalGravity == false){
				
				var playerDir = Vector3D.Normalize(dronePosition - closestPlayer);
				var targetCoords = playerDir * 2100 + closestPlayer;
				SetDestination(closestPlayer, true, 50);
				
			}else{
				
				if(playerPlanetLocation == new Vector3D(0,0,0) || MeasureDistance(closestPlayer, playerPlanetLocation) > 3000){
					
					var upDir = Vector3D.Normalize(closestPlayer - planetLocation);
					var perpDir = Vector3D.CalculatePerpendicularVector(upDir);
					var matrix = MatrixD.CreateWorld(closestPlayer, perpDir, upDir);
					var offset = new Vector3D(0,1900,0);
					offset.X = (double)rnd.Next(-500, 500);
					offset.Z = (double)rnd.Next(-500, 500);
					playerPlanetLocation = Vector3D.Transform(offset, matrix);
					
				}
				
				SetDestination(playerPlanetLocation, true, 30);
				
			}
			
		}
		
	}
	
	if(currentMode == "Deploying"){
		
		//Try Spawning Drones
		
		if(spawnPhaseActive == false){
			
			if(totalSpawnPhases >= maxSpawnPhases){
				
				TryChat("You've proven to be more troublesome than we've anticipated. This isn't over, we will be back.");
				currentMode = "Retreat";
				return;
				
			}
			
			spawnTimer++;
			
			if(spawnTimer >= spawnTimerTrigger){
				
				spawnTimer = 0;
				totalSpawnPhases++;
				
				if(WelderCheck() == false){
					
					TryChat("Cannot deploy drones. The manufacturing bay has been too heavily damaged.");
					return;
					
				}
				
				string [] spawnGroupGravityList = {"(CPC)HORSEFLY_DRONE_1", "(CPC)STRIKE_DRONE_1", "(CPC)KAMIKAZE_DRONE_1"};
				string [] spawnGroupSpaceList = {"(CPC)HORSEFLY_DRONE_1", "(CPC)STRIKE_DRONE_1", "(CPC)KAMIKAZE_DRONE_1", "(CPC)FIGHTER_DRONE_1", "(CPC)SALVAGE_DRONE_1", "(CPC)SNIPER_DRONE_1", "(CPC)TUNNEL_DRONE_1"};
				var spawnGroupChatMsg = new Dictionary<string, string>();
				spawnGroupChatMsg.Add("(CPC)HORSEFLY_DRONE_1", "Deploying Horsefly Drones");
				spawnGroupChatMsg.Add("(CPC)STRIKE_DRONE_1", "Deploying Strike Drones");
				spawnGroupChatMsg.Add("(CPC)KAMIKAZE_DRONE_1", "Deploying Kamikaze Drones");
				spawnGroupChatMsg.Add("(CPC)FIGHTER_DRONE_1", "Deploying Fighter Drones");
				spawnGroupChatMsg.Add("(CPC)SALVAGE_DRONE_1", "Deploying Salvage Drones");
				spawnGroupChatMsg.Add("(CPC)SNIPER_DRONE_1", "Deploying Sniper Drones");
				spawnGroupChatMsg.Add("(CPC)TUNNEL_DRONE_1", "Deploying Tunnel Drones");
				string [] spawnGroups = {"NONE"};
				
				if(inNaturalGravity == false){
					
					spawnGroups = spawnGroupSpaceList;
					
				}else{
					
					spawnGroups = spawnGroupGravityList;
					
				}
				
				selectedSpawnGroup = spawnGroups[rnd.Next(0, spawnGroups.Length)];
				TryChat(spawnGroupChatMsg[selectedSpawnGroup]);
				spawnPhaseActive = true;
			
			}
			
		}
		
		if(spawnPhaseActive == true){
			
			if(spawnedDrones >= spawnedDronesMax){
				
				spawnedDrones = 0;
				spawnPhaseActive = false;
				return;
				
			}
			
			spawnPhaseTimer++;
			
			if(spawnPhaseTimer >= spawnPhaseTimerTrigger){
				
				spawnPhaseTimer = 0;
				var spawnMatrix = MatrixD.CreateWorld(Me.GetPosition(), Me.WorldMatrix.Forward, Me.WorldMatrix.Up);
				var offset = new Vector3D(0, -2.2, 105);
				var spawnarea = Vector3D.Transform(offset, spawnMatrix);
				var velocity = Me.WorldMatrix.Forward * -85;
				var totalVelocity = velocity + remoteControl.GetShipVelocities().LinearVelocity;
				Me.CustomData = selectedSpawnGroup + "\n";
				Me.CustomData += spawnarea.ToString() + "\n";
				Me.CustomData += totalVelocity.ToString() + "\n";
				Me.CustomData += Me.CubeGrid.WorldMatrix.Forward.ToString() + "\n";
				Me.CustomData += "False";
				var spawningResult = TrySpawning();
				Echo(spawningResult.ToString());
				spawnedDrones++;
				
			}
			
		}
		
		//Check For Player and Engage if Nearby
		MyDetectedEntityInfo targetData = new MyDetectedEntityInfo();
		var threatDetected = ThreatDetection(out targetData);
		
		if(threatDetected == true){
			
			if(distanceDroneToPlayer < 1100){
				
				SetDestination(closestPlayer, false, 30);
				
			}else{
				
				if(targetData.IsEmpty() == false){
					
					SetDestination(targetData.Position, false, 30);
					
				}
				
			}
			
		}else{
			
			if(MeasureDistance(deployingLocation, dronePosition) > 500){
				
				SetDestination(deployingLocation, false, 30);
				
			}else{
				
				remoteControl.SetAutoPilotEnabled(false);
				
			}
			
			
		}
		
	}
	
	if(currentMode == "Retreat"){
		
		if(distanceDroneToPlayer > 4000){
			
			TryDespawn(remoteControl, false);
			
		}
		
		if(inNaturalGravity == false){
			
			var targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 8000);
			SetDestination(targetCoords, true, 50);
			
		}else{
			
			var targetCoords = CreateDirectionAndTarget(planetLocation, dronePosition, dronePosition, 8000);
			SetDestination(targetCoords, true, 50);
			
		}
		
	}
	
	if(distanceDroneToPlayer < 200){
		
		remoteControl.SetAutoPilotEnabled(false);
		
	}
	
}

void Main(string argument){
	
	scriptRun += 10;
	
	if(scriptRun < 60){
		
		return;
		
	}
	
	scriptRun = 0;
	
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

bool ThreatDetection(out MyDetectedEntityInfo scannedData){
	
	scannedData = new MyDetectedEntityInfo();
	
	if(distanceDroneToPlayer < 1100){
		
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
		
		Echo("Bad Fail");
		return false;
		
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

bool WelderCheck(){
	
	foreach(var block in blockList){
		
		if(block.CustomName == "Welder (Drone)" && block.IsFunctional == true){
			
			return true;
			
		}
		
	}
	
	return false;
	
}

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

string VectorToGPS(Vector3D coords, string gpsName){
			
	string result = "GPS:"+gpsName+":"+coords.X.ToString()+":"+coords.Y.ToString()+":"+coords.Z.ToString()+":";
	return result;
	
}