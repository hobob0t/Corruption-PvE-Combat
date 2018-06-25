//Corruption Drone AI Template

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

string currentMode = "Space-Seeking";

int teleportsAvailable = 5;
int rechargeTimer = 0;
int rechargeTimerTrigger = 30;

double teleportDistanceMin = 100;
double teleportDistanceMax = 200;

int prevFunctionalBlocks = 0;
int currentFunctionalBlocks = 0;
int damageTimer = 0;

int totalAttackRuns = 0;
int totalAttackRunsLimit = 3;
bool despawnMessage = false;

bool tauntOnRun = false;

string lastMessageSent = "Default";
Random rnd = new Random();

void DroneBehaviour(string argument){
	
	TryIceRefill();
	
	if(damageTimer > 0){
		
		damageTimer--;
		
	}
	
	var tauntChat = new List<string>();
	tauntChat.Add("No way you can keep up with me!");
	tauntChat.Add("Try walking away from this!");
	tauntChat.Add("Initiating micro-jump attack sequence.");
	
	var damagedChat = new List<string>();
	damagedChat.Add("That was a lucky hit. It won't happen again!");
	damagedChat.Add("You really think you can defeat me?");
	damagedChat.Add("You scratched my paint! You'll pay for that!");
	
	if(prevFunctionalBlocks == 0){
		
		prevFunctionalBlocks = CountFunctionalBlocks();
		currentFunctionalBlocks = prevFunctionalBlocks;
		
	}
	
	currentFunctionalBlocks = CountFunctionalBlocks();
	
	if(prevFunctionalBlocks > currentFunctionalBlocks && damageTimer <= 0){
		
		damageTimer = 10;
		TryChat(damagedChat[rnd.Next(0, damagedChat.Count)]);
		prevFunctionalBlocks = currentFunctionalBlocks;
		
	}
	
	if(inNaturalGravity == true && currentMode.Contains("Gravity") == false){
		
		currentMode = "Gravity-Seeking";
		
	}
	
	if(inNaturalGravity == false && currentMode.Contains("Space") == false){
		
		currentMode = "Space-Seeking";
		
	}
	
	if(currentMode == "Space-Seeking"){
		
		if(totalAttackRuns >= totalAttackRunsLimit){
			
			currentMode = "Space-Retreat";
			
		}
		
		if(teleportsAvailable > 0){
			
			if(distanceDroneToPlayer > 1000){
				
				WeaponActivation("Shoot_Off");
				SetDestination(closestPlayer, false, 100);
				
			}else{
				
				//Try Detections Of Player
				MyDetectedEntityInfo scannedData = new MyDetectedEntityInfo();
				
				if(ThreatDetection(out scannedData) == true){
					
					//Setup Positions
					var randomDir = RandomDirection();
					var randomDirInv = randomDir * -1;
					double randomDist = RandomNumberBetween(teleportDistanceMin, teleportDistanceMax);
					var positionA = randomDir * randomDist + (Vector3D)scannedData.HitPosition;
					var positionB = randomDirInv * randomDist + (Vector3D)scannedData.HitPosition;
					var positionChoice = new Vector3D(0,0,0);
					
					if(MeasureDistance(dronePosition, positionA) < MeasureDistance(dronePosition, positionB)){
						
						positionChoice = positionA;
						
					}else{
						
						positionChoice = positionB;
						
					}
					
					Me.CustomData = "";
					Me.CustomData += positionChoice.ToString() + "\n";
					Me.CustomData += scannedData.Position.ToString() + "\n";
					
					if(teleportsAvailable == 1){
						
						var lastTeleportVelocity = Vector3D.Normalize(positionChoice - scannedData.Position);
						lastTeleportVelocity = lastTeleportVelocity * 50;
						Me.CustomData += lastTeleportVelocity.ToString();
						
					}else{
						
						Me.CustomData += scannedData.Velocity.ToString();
						
					}
					
					
					if(TryTeleport() == true){
						
						if(tauntOnRun == false){
							
							tauntOnRun = true;
							TryChat(tauntChat[rnd.Next(0, damagedChat.Count)]);
							
						}
						
						teleportsAvailable--;
						WeaponActivation("Shoot_On");
						remoteControl.SetAutoPilotEnabled(false);
						remoteControl.DampenersOverride = false;
						
						if(teleportsAvailable == 0){
							
							totalAttackRuns++;
							
						}
						
					}else{
						
						SetDestination(closestPlayer, false, 100);
						WeaponActivation("Shoot_Off");
						
					}
					
				}else{
					
					SetDestination(closestPlayer, false, 100);
					WeaponActivation("Shoot_Off");
					
				}
				
			}
			
			
		}else{
			
			//Flee Player
			tauntOnRun = false;
			WeaponActivation("Shoot_Off");
			var oppositeDirection = Vector3D.Normalize(dronePosition - closestPlayer);
			var targetCoords = oppositeDirection * 1000 + closestPlayer;
			SetDestination(targetCoords, true, 100);
			
			rechargeTimer++;
			if(rechargeTimer >= rechargeTimerTrigger){
				
				rechargeTimer = 0;
				teleportsAvailable = 5;
				
			}
			
		}
		
	}
	
	if(currentMode == "Gravity-Seeking"){
		
		if(totalAttackRuns >= totalAttackRunsLimit){
			
			currentMode = "Gravity-Retreat";
			
		}
		
		if(teleportsAvailable > 0){
			
			if(distanceDroneToPlayer > 1000){
				
				WeaponActivation("Shoot_Off");
				var targetCoords = Vector3D.Normalize(closestPlayer - planetLocation) * 100 + closestPlayer;
				SetDestination(targetCoords, false, 100);
				
			}else{
				
				//Try Detections Of Player
				MyDetectedEntityInfo scannedData = new MyDetectedEntityInfo();
				
				if(ThreatDetection(out scannedData) == true){
					
					//Setup Positions
					var randomDir = RandomDirection();
					var randomDirInv = randomDir * -1;
					double randomDist = RandomNumberBetween(teleportDistanceMin, teleportDistanceMax);
					var positionA = randomDir * randomDist + (Vector3D)scannedData.HitPosition;
					var positionB = randomDirInv * randomDist + (Vector3D)scannedData.HitPosition;
					positionA = Vector3D.Normalize(positionA - planetLocation) * 150 + positionA;
					positionB = Vector3D.Normalize(positionB - planetLocation) * 150 + positionB;
					var positionChoice = new Vector3D(0,0,0);
					
					if(MeasureDistance(planetLocation, positionA) > MeasureDistance(planetLocation, positionB)){
						
						positionChoice = positionA;
						
					}else{
						
						positionChoice = positionB;
						
					}
					
					Me.CustomData = "";
					Me.CustomData += positionChoice.ToString() + "\n";
					Me.CustomData += scannedData.Position.ToString() + "\n";
					
					if(teleportsAvailable == 1){
						
						var lastTeleportVelocity = Vector3D.Normalize(positionChoice - planetLocation);
						lastTeleportVelocity = lastTeleportVelocity * 50;
						Me.CustomData += lastTeleportVelocity.ToString();
						
					}else{
						
						Me.CustomData += scannedData.Velocity.ToString();
						
					}
					
					if(TryTeleport() == true){
						
						if(OverloadRisk() == true){
							
							TryChat("System overloading! I'm losing contr...");
							DisablePower();
							return;
							
						}
						
						if(tauntOnRun == false){
							
							tauntOnRun = true;
							TryChat(tauntChat[rnd.Next(0, damagedChat.Count)]);
							
						}
						
						teleportsAvailable--;
						WeaponActivation("Shoot_On");
						remoteControl.SetAutoPilotEnabled(false);
						remoteControl.DampenersOverride = false;
						
						if(teleportsAvailable == 0){
							
							totalAttackRuns++;
							
						}
						
					}else{
						
						var targetCoords = Vector3D.Normalize(closestPlayer - planetLocation) * 100 + closestPlayer;
						SetDestination(targetCoords, false, 100);
						WeaponActivation("Shoot_Off");
						
					}
					
				}else{
					
					var targetCoords = Vector3D.Normalize(closestPlayer - planetLocation) * 100 + closestPlayer;
					SetDestination(targetCoords, false, 100);
					WeaponActivation("Shoot_Off");
					
				}
				
			}
			
			
		}else{
			
			//Flee Player
			tauntOnRun = false;
			WeaponActivation("Shoot_Off");
			var oppositeDirection = Vector3D.Normalize(closestPlayer - planetLocation);
			var targetCoords = oppositeDirection * 1000 + closestPlayer;
			SetDestination(targetCoords, true, 100);
			
			rechargeTimer++;
			if(rechargeTimer >= rechargeTimerTrigger){
				
				rechargeTimer = 0;
				teleportsAvailable = 5;
				
			}
			
		}
		
	}
	
	var leaveChatList = new List<string>();
	leaveChatList.Add("Well, I'm just about out of ammo. Consider this your lucky day.");
	leaveChatList.Add("This encounter was rather bland. I think I'll find someone who can actually put up a fight.");
	leaveChatList.Add("I'll let you live today. But that doesn't mean you won't see me again.");
	
	if(currentMode == "Space-Retreat"){
		
		if(despawnMessage == false){
			
			TryChat(leaveChatList[rnd.Next(0, leaveChatList.Count)]);
			despawnMessage = true;
			
		}
		
		var awayDir = Vector3D.Normalize(dronePosition - closestPlayer);
		var targetCoords = awayDir * 8000 + dronePosition;
		SetDestination(targetCoords, true, 100);
		
		if(distanceDroneToPlayer > 4000){
			
			TryDespawn(remoteControl, false);
			
		}
		
		
	}
	
	if(currentMode == "Gravity-Retreat"){
		
		if(despawnMessage == false){
			
			TryChat(leaveChatList[rnd.Next(0, leaveChatList.Count)]);
			despawnMessage = true;
			
		}
		
		var awayDir = Vector3D.Normalize(closestPlayer - planetLocation);
		var targetCoords = awayDir * 8000 + closestPlayer;
		SetDestination(targetCoords, true, 100);
		
		if(distanceDroneToPlayer > 4000){
			
			TryDespawn(remoteControl, false);
			
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
	
	if(remoteControl.CustomData == ""){
		
		remoteControl.CustomData = CountFunctionalBlocks().ToString();
		
	}
	
	//Execute Custom Drone Behaviour
	DroneBehaviour(argument);
	
}

bool OverloadRisk(){
	
	int totalFunctional = 0;
	if(Int32.TryParse(remoteControl.CustomData, out totalFunctional) == false){
		
		return true;
		
	}
	
	int randomNumber = rnd.Next(0, 100);
	float currentTotal = (float)CountFunctionalBlocks();
	float functionalPercent = currentTotal / (float)totalFunctional;
	functionalPercent *= 100;
	
	if(randomNumber >= functionalPercent){
		
		return true;
		
	}
	
	return false;
	
}

void DisablePower(){
	
	foreach(var block in blockList){
		
		var battery = block as IMyBatteryBlock;
		var reactor = block as IMyReactor;
		
		if(battery != null){
			
			battery.Enabled = false;
			
		}
		
		if(reactor != null){
			
			reactor.Enabled = false;
			
		}
		
	}
	
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
	foreach(var block in blockList){
		
		var cameraBlock = block as IMyCameraBlock;
		
		if(block.IsFunctional == false){
			
			continue;
			
		}
				
		if(cameraBlock != null){
			
			
			cameraBlock.EnableRaycast = true;
			if(cameraBlock.CanScan(MeasureDistance(cameraBlock.GetPosition(), closestPlayer)) == true){

				var scanResults = cameraBlock.Raycast(closestPlayer);
				
				if(scanResults.IsEmpty() == false){
					
					if(scanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
						
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

bool TryTeleport(){
	
	bool result = false;
	
	try{
		
		result = Me.GetValue<bool>("CorruptionTeleport");
		
	}catch(Exception exc){
		
		result = false;
		Echo("ModAPI Call Failed");
		
	}
	
	return result;
	
}

int CountFunctionalBlocks(){
	
	int result = 0;
	foreach(var block in blockList){
		
		if(block.IsFunctional == true){
			
			result++;
			
		}
		
	}
	
	return result;
	
}