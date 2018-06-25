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

bool encounteredPlayer = false;
bool leavePlayer = false;
int failedHackingAttempts = 0;
int hackingTimer = 0;
int hackingTimerTrigger = 35;
string [] hackingTypeList = {"HackBlock", "DisableProduction", "DisableDefense", "DisableAutomation", "HackLights", "HudSpam", "CorruptNames", "GyroOverride", "ThrustOverride"};
bool shieldModCheck = false;
List<long> possibleTargets = new List<long>();

string lastMessageSent = "Default";
Random rnd = new Random();

void DroneBehaviour(string argument){
	
	TryIceRefill();
	
	var chatDictionary = new Dictionary<string, List<string>>();
	
	var greetingChat = new List<string>();
	greetingChat.Add("Let's see how your system security holds up against my skills.");
	greetingChat.Add("Looks like I've found another easy target. Initiating system infiltration.");
	
	var failedChat = new List<string>();
	failedChat.Add("Hmm, that's odd.. I can't find an access point.");
	failedChat.Add("Maybe I'm dealing with luddites who don't have computers.");
	failedChat.Add("No signals on this band.. Trying the next one.");
	
	var leaveChat = new List<string>();
	leaveChat.Add("There doesn't seem to be any valid targets here. I'll search somewhere else.");
	leaveChat.Add("Well this was a waste of time...");
	leaveChat.Add("Whoever put this intel together was a fool! There's nothing I can do here..");
	
	var hackBlockChat = new List<string>();
	hackBlockChat.Add("Code injection complete. We have control of some systems.");
	hackBlockChat.Add("Another system falls under our control.");
	hackBlockChat.Add("You should have changed your passwords, now your systems are ours!");
	chatDictionary.Add("HackBlock", hackBlockChat);
	
	var disableProdChat = new List<string>();
	disableProdChat.Add("Let's disable those noisy machines for you.");
	disableProdChat.Add("You make your machines work too hard. They should have a break.");
	disableProdChat.Add("Productivity is overrated.");
	chatDictionary.Add("DisableProduction", disableProdChat);
	
	var disableTurretsChat = new List<string>();
	disableTurretsChat.Add("Those guns look scary. Maybe they should be turned off.");
	disableTurretsChat.Add("Hard to give hugs with all those turrets trying to hit us.");
	disableTurretsChat.Add("You don't need those turrets enabled. Don't get up, I'll take care of it.");
	chatDictionary.Add("DisableDefense", disableTurretsChat);
	
	var disableAutomationChat = new List<string>();
	disableAutomationChat.Add("Life would be less complicated if you turned off your computers for a while.");
	disableAutomationChat.Add("Too much time on the computers will give you a headache.");
	disableAutomationChat.Add("Those smart devices aren't so smart when turned off.");
	chatDictionary.Add("DisableAutomation", disableAutomationChat);
	
	var hackLightsChat = new List<string>();
	hackLightsChat.Add("It's party time!");
	hackLightsChat.Add("Your choice in lighting was poor. I've fixed it for you.");
	hackLightsChat.Add("What can I say? I'm a fan of RGB.");
	chatDictionary.Add("HackLights", hackLightsChat);
	
	var hudSpamChat = new List<string>();
	hudSpamChat.Add("Look at all those signals!");
	hudSpamChat.Add("How can you see with all that visual noise?");
	hudSpamChat.Add("This is better than owning a label gun.");
	chatDictionary.Add("HudSpam", hudSpamChat);
	
	var overloadShieldsChat = new List<string>();
	overloadShieldsChat.Add("Let's level the playing field a bit.");
	overloadShieldsChat.Add("You won't be needing shields for this encounter.");
	overloadShieldsChat.Add("How does it feel to be so exposed now?");
	chatDictionary.Add("OverloadShields", overloadShieldsChat);
	
	var corruptNamesChat = new List<string>();
	corruptNamesChat.Add("There's no dictionary for gibberish.");
	corruptNamesChat.Add("It's gonna be hard to tell which blocks are what.");
	corruptNamesChat.Add("Here's the result of my keyboard mashing.");
	chatDictionary.Add("CorruptNames", corruptNamesChat);
	
	var gyroOverrideChat = new List<string>();
	gyroOverrideChat.Add("I'll spin you right round.");
	gyroOverrideChat.Add("Hope you brought a barf bag.");
	gyroOverrideChat.Add("Weeeeeeeeee!");
	chatDictionary.Add("GyroOverride", gyroOverrideChat);
	
	var thrustOverrideChat = new List<string>();
	thrustOverrideChat.Add("Betrayed by speed!");
	thrustOverrideChat.Add("You might be drifting a little.");
	thrustOverrideChat.Add("I don't think your thrusters are just idling now.");
	chatDictionary.Add("ThrustOverride", thrustOverrideChat);
	
	if(inNaturalGravity == true && currentMode.Contains("Gravity") == false){
		
		currentMode = "Gravity-Seeking";
		
	}
	
	if(inNaturalGravity == false && currentMode.Contains("Space") == false){
		
		currentMode = "Space-Seeking";
		
	}
			
	if(encounteredPlayer == false && distanceDroneToPlayer > 3500 && leavePlayer == false){
		
		if(currentMode == "Space-Seeking"){
			
			SetDestination(closestPlayer, false, 100);
			
		}else{
			
			var playerUpDir = Vector3D.Normalize(closestPlayer - planetLocation);
			var targetCoords = playerUpDir * 1000 + closestPlayer;
			SetDestination(targetCoords, false, 100);
			
		}
		
		return;
		
	}
	
	if(encounteredPlayer == false && distanceDroneToPlayer < 3500 && leavePlayer == false){
		
		remoteControl.SetAutoPilotEnabled(false);
		encounteredPlayer = true;
		TryChat(greetingChat[rnd.Next(0, greetingChat.Count)]);
		HackerAnnounce();
		possibleTargets.Clear();
		possibleTargets = Me.GetValue<List<long>>("CorruptionHackingTargets");
		
	}
	
	if(encounteredPlayer == true && leavePlayer == false){
		
		hackingTimer++;
		
		if(hackingTimer >= hackingTimerTrigger){
			
			bool successfulHack = false;
			hackingTimer = 0;
			
			var hackingType = "";
			
			if(shieldModCheck == false){
				
				shieldModCheck = true;
				
				if(TryShieldModCheck() == true){
					
					hackingType = "OverloadShields";
					
				}else{
					
					hackingType = hackingTypeList[rnd.Next(0, hackingTypeList.Length)];
					
				}
				
			}else{
				
				hackingType = hackingTypeList[rnd.Next(0, hackingTypeList.Length)];
				
			}
						
			foreach(var target in possibleTargets){
				
				Me.CustomData = target.ToString() + "\n" + hackingType;
				
				Echo(hackingType);
				if(Me.GetValue<bool>("CorruptionHacking") == true){
					
					successfulHack = true;
					
				}
				
			}
			
			if(successfulHack == true){
				
				failedHackingAttempts = 0;
				var tempChatList = new List<string>();
				if(chatDictionary.ContainsKey(hackingType)){
					
					tempChatList = chatDictionary[hackingType];
					TryChat(tempChatList[rnd.Next(0, tempChatList.Count)]);
					
				}
				
			}else{
				
				failedHackingAttempts++;
				if(failedHackingAttempts <= 2){
					
					TryChat(failedChat[rnd.Next(0, failedChat.Count)]);
					
				}else{
					
					TryChat(leaveChat[rnd.Next(0, leaveChat.Count)]);
					leavePlayer = true;
					
				}
				
				
				
			}
			
		}
		
	}
	
	if(leavePlayer == true){
		
		if(currentMode == "Space-Seeking"){
			
			var awayDirection = Vector3D.Normalize(dronePosition - closestPlayer);
			var targetCoords = awayDirection * 8000 + closestPlayer;
			SetDestination(targetCoords, true, 100);
			
		}else{
			
			var playerUpDir = Vector3D.Normalize(closestPlayer - planetLocation);
			var targetCoords = playerUpDir * 8000 + closestPlayer;
			SetDestination(targetCoords, true, 100);
			
		}
				
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
	
	//Execute Custom Drone Behaviour
	DroneBehaviour(argument);
	
}

double MeasureDistance(Vector3D point_a, Vector3D point_b){
	
	double result = Math.Round( Vector3D.Distance( point_a, point_b ), 2 );
	return result;
	
}

void HackerAnnounce(){
	
	foreach(var block in blockList){
		
		if(block.IsFunctional == false || block.IsWorking == false){
			
			continue;
			
		}
		
		var antenna = block as IMyRadioAntenna;
		
		if(antenna == null){
			
			continue;
			
		}
		
		if(antenna.Enabled == true || antenna.EnableBroadcasting == true){
			
			var result = antenna.TransmitMessage("CorruptionHackerDroneAnnounce", MyTransmitTarget.Everyone);
			
		}
		
	}
	
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

bool TryShieldModCheck(){
	
	bool result = false;
	
	try{
		
		result = Me.GetValue<bool>("CorruptionShieldMod");
		
	}catch(Exception exc){
		
		
		
	}
	
	return result;
	
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
