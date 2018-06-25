//Strike Drone Mk.II AI Script

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
Vector3D targetDirection = new Vector3D(0,0,0);
Vector3D localDirection = new Vector3D(0,0,0);
int timeOut = 0;
Random rnd = new Random();

void DroneBehaviour(string argument){
	
	Echo(currentMode);
	Echo(timeOut.ToString());
	bool cameraDamaged = false;
	TryIceRefill();
		
	if(inNaturalGravity == false && currentMode.Contains("Space") == false){
		
		currentMode = "Space-Seeking";
		timeOut = 0;
		
	}
	
	if(inNaturalGravity == true && currentMode.Contains("Gravity") == false){
		
		currentMode = "Gravity-Seeking";
		timeOut = 0;
		
	}
	
	///////////////////////////////////////
	//SPACE BEHAVIOUR
	///////////////////////////////////////
	
	if(timeOut > 0){
			
		timeOut--;
			
	}
	
	if(currentMode == "Space-Seeking"){
		
		if(timeOut <= 0){
			
			targetDirection = RandomDirection();
			timeOut = 30;
			
		}
		
		
		var targetCoords = targetDirection * 450 + closestPlayer;
		SetDestination(targetCoords, true, 100);
		
		if(MeasureDistance(dronePosition, targetCoords) < 50){
			
			targetDirection = RandomDirection();
			targetCoords = targetDirection * 450 + closestPlayer;
			SetDestination(targetCoords, true, 100);
			timeOut = 30;
			
		}

	}
	
	///////////////////////////////////////
	//GRAVITY BEHAVIOUR
	///////////////////////////////////////
	
	if(currentMode == "Gravity-Seeking"){
		
		double elevation = 0;
		remoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
		if(remoteControl.IsAutoPilotEnabled == true){
			
			if(MeasureDistance(dronePosition, remoteControl.CurrentWaypoint.Coords) < 50){

				timeOut = 0;
			
			}
			
		}
		
		
		if(elevation < 150){
			
			var upDir = Vector3D.Normalize(dronePosition - planetLocation);
			var targetCoords = upDir * 200 + dronePosition;
			SetDestination(targetCoords, true, 100);
			timeOut = 0;
			return;
			
		}
		
		if(timeOut <= 0){
			
			localDirection.X = (double)rnd.Next(-500, 500);
			localDirection.Y = (double)rnd.Next(300, 550);
			localDirection.Z = (double)rnd.Next(-500, 500);
			MatrixD rcMatrix = MatrixD.CreateWorld(closestPlayer, remoteControl.WorldMatrix.Forward, remoteControl.WorldMatrix.Up);
			var targetCoords = Vector3D.Transform(localDirection, rcMatrix);
			SetDestination(targetCoords, true, 100);
			timeOut = 30;
			
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

double SpeedReducer(double droneSpeed){
	
	if(droneSpeed < 1000){
		
		return Math.Round(droneSpeed / 10, 2);
		
	}else{
		
		return 100;
		
	}
	
}