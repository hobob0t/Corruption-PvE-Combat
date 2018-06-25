//Base Pirate Template

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D asteroidLocation = new Vector3D(0,0,0);

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

//Other Values
int tunnelAdjustTimer = 0;
Random rnd = new Random();
int soundTimer = 0;
int soundTimerTrigger = 0;
string [] soundChoice = {"Drone - Small", "Drone - Medium", "Drone - Large", "Drone - Huge"};

void DroneBehaviour(string argument){
	
	//Sound
	if(soundTimerTrigger == 0){
		
		soundTimerTrigger = rnd.Next(30, 60);
		
	}
	
	soundTimer++;
	
	if(soundTimer >= soundTimerTrigger){
		
		soundTimer = 0;
		soundTimerTrigger = rnd.Next(30, 60);
		PlaySound(soundChoice[rnd.Next(0, 4)]);
		
	}
	
	MyDetectedEntityInfo forwardScanResults = CameraRayCast(remoteControl.WorldMatrix.Forward, 2000);
		
	if(forwardScanResults.IsEmpty() == true){
		
		SetDestination(closestPlayer, false, 100, "Forward");
		WeaponActivation("Shoot_Off");
		return;
		
	}
	
	if(forwardScanResults.Type == MyDetectedEntityType.Asteroid){
		
		asteroidLocation = (Vector3D)forwardScanResults.HitPosition;
		WeaponActivation("Shoot_Off");
		Vector3D asteroidSurfaceCoords = (Vector3D)forwardScanResults.HitPosition;
		double droneAsteroidDistance = MeasureDistance(dronePosition, asteroidSurfaceCoords);
		OverrideForwardThrust(false, remoteControl.WorldMatrix.Forward, 0.5f);
		
		if(droneAsteroidDistance < 1000 && droneAsteroidDistance > 301){
			
			SetDestination(closestPlayer, false, 40, "Forward");
			
			
		}
		
		if(droneAsteroidDistance < 300 && droneAsteroidDistance > 151){

			SetDestination(closestPlayer, false, 20, "Forward");
			
		}
		
		if(droneAsteroidDistance < 150 && droneAsteroidDistance > 31){
			
			SetDestination(closestPlayer, false, 10, "Forward");
			
		}
		
		if(droneAsteroidDistance < 30){
			
			remoteControl.SetAutoPilotEnabled(false);
			remoteControl.DampenersOverride = true;
			
			if(tunnelAdjustTimer < 5){
				
				tunnelAdjustTimer++;
				
			}else{
				
				tunnelAdjustTimer = 0;
				SetDestination(closestPlayer, false, 1, "Forward");
				
			}
			
			if(remoteControl.GetShipSpeed() < 3){
				
				OverrideForwardThrust(true, remoteControl.WorldMatrix.Forward, 0.5f);

			}else{
				
				OverrideForwardThrust(false, remoteControl.WorldMatrix.Forward, 0.5f);
				
			}
			
		}
		
	}
	
	if(MeasureDistance(closestPlayer, dronePosition) < 7){
		
		SelfDestruct("Countdown");
		SelfDestruct("Detonate");
			
	}
	
	if(forwardScanResults.Type == MyDetectedEntityType.SmallGrid || forwardScanResults.Type == MyDetectedEntityType.LargeGrid || forwardScanResults.Type == MyDetectedEntityType.CharacterHuman){
				
		Vector3D targetCoords = (Vector3D)forwardScanResults.HitPosition;
		SetDestination(targetCoords, false, 100, "Forward");
		
		if(MeasureDistance(targetCoords, dronePosition) < 800 && forwardScanResults.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
			
			WeaponActivation("Shoot_On");
			
		}else{
			
			WeaponActivation("Shoot_Off");
			
		}
		
		if(forwardScanResults.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies){
			
			return;
			
		}
		
		if(MeasureDistance(targetCoords, dronePosition) < 6){
			
			SelfDestruct("Countdown");
			SelfDestruct("Detonate");
			
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
	remoteControl = null;
	ResetBlockList();
	
	foreach(var block in blockList){
		
		var firstRemote = block as IMyRemoteControl;
		
		if(firstRemote == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		remoteControl = firstRemote;
		break;
		
	}
	
	if(remoteControl == null){
		
		Echo("Remote Control Missing");
		return;
		
	}
	
	//Ensure Remote Control Is NPC Owned & Get Player Location
	droneIsNPC = remoteControl.GetNearestPlayer(out closestPlayer);
	OriginSetup();
	
	if(droneIsNPC == false){
		
		Echo("Drone is not controlled by an NPC");
		return;
		
	}
		
	//Get Other Location Data
	dronePosition = remoteControl.GetPosition();
	inNaturalGravity = remoteControl.TryGetPlanetPosition(out planetLocation);

	//Calculate Distances
	distanceDroneToPlayer = MeasureDistance(dronePosition, closestPlayer);
	distanceDroneToOrigin = MeasureDistance(dronePosition, originPosition);
	distancePlayerToOrigin = MeasureDistance(closestPlayer, originPosition);
	
	if(distanceDroneToPlayer > 20000){
		
		TryDespawn(remoteControl, false);
		
	}
	
	TryDespawn(remoteControl, true);

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

MyDetectedEntityInfo CameraRayCast(Vector3D direction, float scanDistance){
	
	MyDetectedEntityInfo scanResults = new MyDetectedEntityInfo();
	
	foreach(var block in blockList){
		
		var camera = block as IMyCameraBlock;
		
		if(camera == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		//TODO: Figure out if camera direction is Forward or Up
		if(camera.WorldMatrix.Forward != direction){
			
			Echo("Scan Fail");
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

void SetDestination(Vector3D coords, bool collisionModeEnable, float speedLimit, string forwardDirection){
	
	remoteControl.ClearWaypoints();
	remoteControl.AddWaypoint(coords, "Destination");
	remoteControl.SetAutoPilotEnabled(true);
	remoteControl.SetValueBool("CollisionAvoidance", collisionModeEnable);
	remoteControl.SetValueFloat("SpeedLimit", speedLimit);
	remoteControl.ApplyAction(forwardDirection);
	
}

void ToggleBlock(string blockName, bool toggleMode, bool singleBlock){
	
	foreach(var block in blockList){
		
		var functionalBlock = block as IMyFunctionalBlock;
		
		if(functionalBlock == null || block.IsFunctional == false || block.CustomName.Contains(blockName) == false){
			
			continue;
			
		}
		
		functionalBlock.Enabled = toggleMode;
		
		if(singleBlock == true){
			
			return;
			
		}
		
	}
	
}

void OverrideForwardThrust(bool enableOverride, Vector3D direction, float thrustModifier){
	
	foreach(var block in blockList){
		
		var thruster = block as IMyThrust;
		
		if(thruster == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(enableOverride == true){
			
			if(thruster.WorldMatrix.Forward == direction * -1){
			
				thruster.SetValueFloat("Override", 100 * thrustModifier);
			
			}
			
		}else{
			
			thruster.SetValueFloat("Override", 0);
			
		}
		
	}
	
}

void PlaySound(string soundName){
	
	foreach(var block in blockList){
		
		var sound = block as IMySoundBlock;
		
		if(sound == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		sound.SelectedSound = soundName;
		sound.Play();
		
	}	
	
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

void SelfDestruct(string weaponMode){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyWarhead;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(weaponMode == "Countdown"){
			
			weapon.ApplyAction("StartCountdown");
			
		}
		
		if(weaponMode == "Detonate"){
			
			weapon.SetValueBool("Safety", true);
			weapon.ApplyAction("Detonate");
			
		}
		
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